using ApparkaTrainingFlowOnline.Models;
using ApparkaTrainingFlowOnline.Services;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ApparkaTrainingFlowOnline.Data;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<AppDbContext>();
        var clock = services.GetRequiredService<PeruClock>();
        var passwords = services.GetRequiredService<PasswordService>();

        await EnsureDemoUsersAsync(db, passwords);
        await EnsureOrganizationAsync(db);

        var position = await db.Positions
            .OrderBy(x => x.Id)
            .FirstAsync();

        if (position.Name == "Operador de estacionamiento")
        {
            position.Name = "Anfitrión Red Comercial";
            position.Description = "Atención al cliente, seguridad, plaqueo, control operativo, incidencias y entrega de turno.";
            await db.SaveChangesAsync();
        }

        await SyncLearningMaterialsAsync(db, position.Id);
        await SyncTrainingContentAsync(db);
        await EnsureDemoAssignmentAsync(db, services, clock, position.Id);
    }

    private static async Task EnsureDemoUsersAsync(AppDbContext db, PasswordService passwords)
    {
        if (await db.Users.AnyAsync()) return;

        foreach (var (name, email, role, password) in new[]
        {
            ("Administrador Demo", "admin@demo.local", AppRoles.Administrator, "Admin123*"),
            ("Recursos Humanos", "rrhh@demo.local", AppRoles.Hr, "Rrhh123*"),
            ("Supervisor Demo", "supervisor@demo.local", AppRoles.Supervisor, "Supervisor123*"),
            ("Colaborador Demo", "colaborador@demo.local", AppRoles.Collaborator, "Colaborador123*")
        })
        {
            var user = new AppUser { FullName = name, Email = email, Role = role };
            user.PasswordHash = passwords.Hash(user, password);
            db.Users.Add(user);
        }

        await db.SaveChangesAsync();
    }

    private static async Task EnsureOrganizationAsync(AppDbContext db)
    {
        if (!await db.Locations.AnyAsync())
        {
            db.Locations.AddRange(
                new Location { Name = "Sede Centro", Address = "Lima" },
                new Location { Name = "Sede Norte", Address = "Lima" },
                new Location { Name = "Sede Sur", Address = "Lima" });
        }

        if (!await db.Positions.AnyAsync())
        {
            db.Positions.Add(new Position
            {
                Name = "Anfitrión Red Comercial",
                Description = "Atención al cliente, seguridad, plaqueo, control operativo, incidencias y entrega de turno."
            });
        }

        await db.SaveChangesAsync();
    }

    private static async Task SyncLearningMaterialsAsync(AppDbContext db, int positionId)
    {
        var existing = await db.LearningMaterials
            .Where(x => x.PositionId == positionId)
            .OrderBy(x => x.Id)
            .ToListAsync();

        foreach (var oldMaterial in existing)
            oldMaterial.IsActive = false;

        foreach (var definition in BuildLearningModules())
        {
            var material = existing.FirstOrDefault(x => x.SortOrder == definition.SortOrder);
            if (material is null)
            {
                material = new LearningMaterial { PositionId = positionId, SortOrder = definition.SortOrder };
                db.LearningMaterials.Add(material);
            }

            material.Title = definition.Title;
            material.Summary = definition.Summary;
            material.EstimatedMinutes = definition.EstimatedMinutes;
            material.ContentJson = JsonSerializer.Serialize(definition.Content);
            material.VideoUrl = null;
            material.IsActive = true;
        }

        await db.SaveChangesAsync();
    }

    private static async Task SyncTrainingContentAsync(AppDbContext db)
    {
        var definitions = BuildActivities();
        var templates = await db.ActivityTemplates.ToListAsync();

        foreach (var template in templates.Where(x => definitions.All(d => d.Sequence != x.Sequence)))
            template.IsActive = false;

        foreach (var definition in definitions)
        {
            var template = templates.FirstOrDefault(x => x.Sequence == definition.Sequence);
            if (template is null)
            {
                template = new ActivityTemplate { Sequence = definition.Sequence };
                db.ActivityTemplates.Add(template);
                templates.Add(template);
            }

            template.WeekNumber = definition.WeekNumber;
            template.Title = definition.Title;
            template.Instructions = definition.Instructions;
            template.PracticalChallenge = definition.PracticalChallenge;
            template.IsActive = true;
        }

        await db.SaveChangesAsync();

        var existingQuestions = await db.Questions.ToListAsync();
        foreach (var legacy in existingQuestions.Where(x => string.IsNullOrWhiteSpace(x.ContentKey)))
            legacy.IsActive = false;

        var questionsByKey = existingQuestions
            .Where(x => !string.IsNullOrWhiteSpace(x.ContentKey))
            .ToDictionary(x => x.ContentKey!, StringComparer.OrdinalIgnoreCase);
        var activeKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var definition in definitions)
        {
            var templateId = templates.Single(x => x.Sequence == definition.Sequence).Id;
            foreach (var questionDefinition in definition.Questions)
            {
                activeKeys.Add(questionDefinition.Key);
                UpsertQuestion(db, questionsByKey, questionDefinition, templateId, false);
            }
        }

        foreach (var questionDefinition in BuildFinalQuestions())
        {
            activeKeys.Add(questionDefinition.Key);
            UpsertQuestion(db, questionsByKey, questionDefinition, null, true);
        }

        foreach (var oldQuestion in questionsByKey.Values.Where(x => !activeKeys.Contains(x.ContentKey!)))
            oldQuestion.IsActive = false;

        await db.SaveChangesAsync();
        await RepairLegacyInProgressActivitiesAsync(db);
    }

    private static async Task RepairLegacyInProgressActivitiesAsync(AppDbContext db)
    {
        var legacyActivities = await db.ActivityEvidences
            .Include(x => x.QuestionSelections)
            .Include(x => x.Template).ThenInclude(x => x.Questions)
            .Where(x => x.Status == EvidenceStatus.InProgress && !x.QuestionSelections.Any())
            .AsSplitQuery()
            .ToListAsync();

        foreach (var evidence in legacyActivities)
        {
            var questions = evidence.Template.Questions
                .Where(x => x.IsActive)
                .OrderBy(x => x.Id)
                .Take(3)
                .ToList();

            for (var index = 0; index < questions.Count; index++)
            {
                evidence.QuestionSelections.Add(new ActivityQuestionSelection
                {
                    QuestionId = questions[index].Id,
                    DisplayOrder = index + 1,
                    OptionOrder = "ABCD"
                });
            }
        }

        if (legacyActivities.Count > 0)
            await db.SaveChangesAsync();
    }

    private static void UpsertQuestion(
        AppDbContext db,
        IDictionary<string, Question> questionsByKey,
        QuestionDefinition definition,
        int? activityTemplateId,
        bool isFinalExamQuestion)
    {
        if (!questionsByKey.TryGetValue(definition.Key, out var question))
        {
            question = new Question { ContentKey = definition.Key };
            questionsByKey[definition.Key] = question;
            db.Questions.Add(question);
        }

        question.ActivityTemplateId = activityTemplateId;
        question.IsFinalExamQuestion = isFinalExamQuestion;
        question.Prompt = definition.Prompt;
        question.OptionA = definition.A;
        question.OptionB = definition.B;
        question.OptionC = definition.C;
        question.OptionD = definition.D;
        question.CorrectOption = definition.Correct;
        question.Explanation = definition.Explanation;
        question.ReviewTopic = definition.Topic;
        question.IsActive = true;
    }

    private static async Task EnsureDemoAssignmentAsync(
        AppDbContext db,
        IServiceProvider services,
        PeruClock clock,
        int positionId)
    {
        if (await db.TrainingAssignments.AnyAsync()) return;

        var collaborator = await db.Users.FirstAsync(x => x.Role == AppRoles.Collaborator);
        var supervisor = await db.Users.FirstAsync(x => x.Role == AppRoles.Supervisor);
        var hr = await db.Users.FirstAsync(x => x.Role == AppRoles.Hr);
        var location = await db.Locations.FirstAsync();
        var assignment = new TrainingAssignment
        {
            CollaboratorId = collaborator.Id,
            SupervisorId = supervisor.Id,
            CreatedById = hr.Id,
            LocationId = location.Id,
            PositionId = positionId,
            AccessFrom = clock.Today.AddDays(-1),
            StartDate = clock.Today,
            EndDate = clock.Today.AddDays(20),
            Status = TrainingStatus.InTraining
        };

        var schedule = services.GetRequiredService<TrainingScheduleService>();
        await schedule.GenerateActivitiesAsync(assignment);
        db.TrainingAssignments.Add(assignment);
        await db.SaveChangesAsync();
    }

    private static IReadOnlyList<LearningModuleDefinition> BuildLearningModules() =>
    [
        new(1, "Tu rol como anfitrión", "Comprende el propósito del puesto, sus funciones esenciales y las conductas esperadas.", 5,
            new LearningModuleContent
            {
                Introduction = "Eres la cara de la operación. Tu buen trato y tu presencia hacen que el cliente se sienta seguro y quiera volver. El objetivo del puesto es contribuir a una experiencia de calidad mediante el cumplimiento de los procedimientos operativos y de atención al cliente.",
                Sections =
                [
                    new() { Title = "Funciones esenciales", Items = ["Atender y orientar al cliente.", "Realizar el cobro del estacionamiento cuando corresponda.", "Efectuar el plaqueo y control vehicular.", "Verificar que los accesos estén libres.", "Revisar equipos y stock.", "Registrar novedades.", "Comunicarse con el supervisor y con el relevo."] },
                    new() { Title = "Conductas esperadas", Items = ["Usar el uniforme completo, limpio y en buenas condiciones.", "Ser puntual y respetar los turnos.", "Mantener un buen trato y comunicarse con claridad.", "Actuar con integridad.", "Cuidar la caja, los bienes, los equipos y la seguridad."] }
                ]
            }),
        new(2, "Seguridad y prevención", "Aprende a reconocer peligros, prevenir la exposición y actuar con serenidad ante emergencias.", 7,
            new LearningModuleContent
            {
                Introduction = "La seguridad tiene prioridad sobre la continuidad de una tarea. Identifica los peligros, evita exponerte y comunica oportunamente cualquier condición insegura.",
                Sections =
                [
                    new() { Title = "Prevención durante el turno", Items = ["Identificar peligros y comunicar condiciones inseguras.", "Mantener despejadas las rutas de evacuación.", "Reconocer zonas seguras, extintores y medios de emergencia.", "Evitar trabajos que pongan en riesgo tu integridad.", "Mantener el área limpia y ordenada.", "Realizar pausas activas y evitar posturas forzadas."] },
                    new() { Title = "Ante una emergencia", Body = "Mantén la serenidad. Ante peligro inminente puedes interrumpir la actividad y comunicarla al responsable. En un incendio, alerta, sigue las rutas señalizadas y obedece a los brigadistas. Si hay humo, desplázate cerca del piso y no regreses por objetos personales.", Items = ["No bloquear accesos, extintores ni rutas de evacuación.", "No manipular equipos eléctricos averiados.", "No improvisar reparaciones sin autorización."] },
                    new() { Title = "Situaciones de campo", IsInferred = true, Items = ["Advertir provisionalmente alrededor de un charco mientras se comunica la condición.", "Informar el riesgo antes de continuar una actividad.", "Alejarse de equipos o instalaciones que presenten un peligro evidente."] }
                ]
            }),
        new(3, "Atención al cliente", "Practica una atención clara, respetuosa y basada únicamente en información confirmada.", 6,
            new LearningModuleContent
            {
                Introduction = "El anfitrión escucha, orienta y acompaña al cliente sin inventar respuestas, discutir ni prometer soluciones que no puede autorizar.",
                Sections =
                [
                    new() { Title = "Cómo orientar correctamente", Items = ["Saludar y escuchar al cliente.", "Orientarlo sobre accesos, salidas y formas de pago.", "Comunicar promociones solo cuando estén vigentes y confirmadas.", "Explicar las condiciones del servicio.", "Derivar consultas especializadas al módulo correspondiente.", "Derivar los reclamos al supervisor.", "Mantener un trato respetuoso aunque el cliente esté molesto."] },
                    new() { Title = "Secuencia sugerida de atención", IsInferred = true, Items = ["Saludar.", "Escuchar.", "Confirmar la consulta.", "Orientar con información segura.", "Derivar cuando corresponda.", "Confirmar que el cliente entendió."] }
                ],
                ConfirmationNote = "Las promociones, condiciones comerciales y el tratamiento exacto de reclamos deben confirmarse con Apparka y con la sede correspondiente."
            }),
        new(4, "Operación, plaqueo y control", "Conoce las verificaciones que sostienen una apertura ordenada y una operación trazable.", 8,
            new LearningModuleContent
            {
                Introduction = "El plaqueo digital registra el estado en el que ingresa un vehículo y ayuda a mantener el control de la operación. La revisión inicial permite anticipar fallas y faltantes.",
                Sections =
                [
                    new() { Title = "Condiciones de la playa", Items = ["Accesos habilitados y ausencia de congestión.", "Estado general de la playa.", "Limpieza y ausencia de desperdicios o charcos.", "Disponibilidad de tickets."] },
                    new() { Title = "Equipos y stock", Items = ["Stock de APS, POS, tablets y contómetros.", "Funcionamiento de radios, impresoras, linternas y equipos de cobro.", "Estado de atriles, sombrillas, cuadernos y letreros."] },
                    new() { Title = "Apertura operativa", IsInferred = true, Items = ["Registrar asistencia.", "Recibir información del relevo.", "Revisar equipos y stock.", "Verificar accesos.", "Revisar limpieza y riesgos.", "Informar observaciones.", "Iniciar la atención."] }
                ],
                ConfirmationNote = "Apparka debe confirmar los sistemas de plaqueo, los tipos de APS/POS y los procedimientos de cobro y contingencia utilizados en cada sede."
            }),
        new(5, "Incidencias, novedades y objetos encontrados", "Registra hechos claros y conserva la trazabilidad hasta el siguiente turno.", 7,
            new LearningModuleContent
            {
                Introduction = "Toda incidencia o accidente debe comunicarse al supervisor, registrarse y transmitirse al relevo cuando quede una acción pendiente.",
                Sections =
                [
                    new() { Title = "Datos de una novedad útil", Items = ["Fecha y hora.", "Lugar.", "Qué ocurrió.", "Personas o equipos involucrados.", "Acción realizada.", "A quién se informó.", "Situación pendiente."] },
                    new() { Title = "Objetos encontrados", Body = "Entrega cualquier objeto encontrado al responsable autorizado. No lo conserves personalmente ni lo entregues sin registro.", Items = ["Comunicar el hallazgo.", "Registrar la entrega.", "Identificar al responsable que lo recibe."] },
                    new() { Title = "Continuidad", Items = ["Proteger el área cuando exista un riesgo.", "Informar al supervisor.", "Registrar la actuación realizada.", "Comunicar al relevo los pendientes y riesgos."] }
                ],
                ConfirmationNote = "La cadena formal de custodia de objetos, el formato del cuaderno de novedades y la contingencia por falla de barreras deben confirmarse con Apparka."
            }),
        new(6, "Integridad y cierre de turno", "Cierra el turno con transparencia, entrega ordenada y comunicación de pendientes.", 6,
            new LearningModuleContent
            {
                Introduction = "La integridad protege al colaborador, al cliente y a la empresa. No ocultes errores ni diferencias: regístralos y repórtalos por el canal correspondiente.",
                Sections =
                [
                    new() { Title = "Actuar con integridad", Items = ["Cuidar la caja y los activos.", "No ocultar errores.", "Reportar diferencias.", "Entregar equipos y herramientas.", "Comunicar pendientes.", "Respetar las normas del establecimiento.", "No realizar funciones ajenas al ámbito laboral.", "Utilizar la línea ética cuando corresponda."] },
                    new() { Title = "Entrega al relevo", IsInferred = true, Items = ["Incidencias del turno.", "Equipos con fallas.", "Stock pendiente.", "Reclamos o consultas abiertas.", "Objetos encontrados.", "Accesos restringidos.", "Actividades que requieren seguimiento."] }
                ],
                ConfirmationNote = "La forma exacta de entrega del turno y los canales internos deben adecuarse a las políticas vigentes de Apparka."
            })
    ];

    private static IReadOnlyList<ActivityDefinition> BuildActivities() =>
    [
        new(1, 1, "Reconocimiento y seguridad", "Reconoce tu sede y demuestra que puedes identificar y comunicar riesgos antes de iniciar la operación.", "Recorre tu área e identifica dos riesgos, una ruta de evacuación, una zona segura, un extintor o equipo de emergencia y un equipo que debe verificarse antes de iniciar.",
        [
            Q("ACT-01-Q01", "Al iniciar tu turno encuentras varias cajas bloqueando parcialmente una ruta de evacuación. ¿Qué debes hacer?", "Dejarlas porque todavía se puede pasar.", "Comunicar la condición y gestionar que la ruta quede despejada.", "Esperar al siguiente turno.", "Colocar más cajas para que nadie pase.", "B", "Las rutas de evacuación deben mantenerse libres y una condición insegura debe comunicarse al responsable.", "Rutas de evacuación y comunicación de condiciones inseguras"),
            Q("ACT-01-Q02", "Observas un cable expuesto cerca de un equipo de cobro. El equipo todavía enciende. ¿Cuál es la acción correcta?", "Continuar usándolo porque todavía funciona.", "Cubrirlo con papel.", "Evitar su uso y reportarlo al supervisor.", "Intentar repararlo sin autorización.", "C", "Un equipo operativo no necesariamente es seguro. No se debe improvisar una reparación ni exponerse a un riesgo.", "Revisión de equipos y comunicación de condiciones inseguras"),
            Q("ACT-01-Q03", "¿Cuál es una conducta correcta ante una situación de peligro inminente?", "Continuar hasta terminar el turno.", "Interrumpir la actividad, alejarse del peligro y comunicarlo.", "Ocultar la situación para evitar problemas.", "Pedirle a un cliente que la resuelva.", "B", "La seguridad tiene prioridad sobre la continuidad de la tarea.", "Respuesta ante peligro inminente"),
            Q("ACT-01-Q04", "Detectas un charco en una zona de circulación de clientes. ¿Qué acción demuestra prevención?", "Ignorarlo mientras nadie se caiga.", "Comunicarlo y advertir temporalmente el riesgo hasta su atención.", "Esperar que se seque.", "Culpar al turno anterior.", "B", "Debe evitarse que el riesgo alcance a clientes o trabajadores y solicitarse una medida correctiva.", "Prevención de riesgos en zonas de circulación")
        ]),
        new(2, 1, "Atención y registro básico", "Aplica una atención respetuosa, orienta con información confirmada y demuestra el propósito del plaqueo.", "Recibe a un cliente, oriéntalo hacia una salida o medio de pago, explica una condición confirmada del servicio y realiza un plaqueo simulado o real cuando aplique.",
        [
            Q("ACT-02-Q01", "Un cliente pregunta por una promoción que no conoces. ¿Qué deberías responder?", "Inventar una condición para no hacerlo esperar.", "Confirmar la información o derivarlo al módulo correspondiente.", "Decirle que todas las promociones son iguales.", "Ignorar la consulta.", "B", "Solo debe brindarse información confirmada.", "Información comercial confirmada y derivación"),
            Q("ACT-02-Q02", "¿Cuál es el propósito principal del plaqueo digital?", "Registrar el estado de ingreso del vehículo y mantener control.", "Calcular automáticamente el sueldo.", "Registrar la asistencia del anfitrión.", "Reemplazar todos los equipos de la playa.", "A", "El plaqueo permite registrar el estado en el que ingresa el vehículo y mantener trazabilidad operativa.", "Plaqueo digital y control vehicular"),
            Q("ACT-02-Q03", "Un cliente se muestra molesto porque no encuentra la salida. ¿Cuál es la mejor forma de atenderlo?", "Decirle que observe las señales.", "Escucharlo, mantener la calma y orientarlo claramente.", "Responder con el mismo tono.", "Alejarse sin decir nada.", "B", "El anfitrión representa la experiencia de servicio y debe mantener un trato respetuoso.", "Atención a clientes molestos"),
            Q("ACT-02-Q04", "Un cliente presenta un reclamo que no puedes solucionar directamente. ¿Qué debes hacer?", "Prometer una compensación.", "Discutir para demostrar que la empresa tiene razón.", "Escuchar y derivar oportunamente al supervisor.", "Pedirle que vuelva otro día.", "C", "El anfitrión puede orientar, pero los reclamos que exceden su autoridad deben escalarse al responsable.", "Tratamiento y escalamiento de reclamos")
        ]),
        new(3, 2, "Operación acompañada", "Ejecuta la apertura operativa con acompañamiento y anticipa fallas o faltantes.", "Con acompañamiento, registra tu ingreso, revisa accesos y limpieza, comprueba equipos, verifica tickets y stock y reporta al menos una observación.",
        [
            Q("ACT-03-Q01", "¿Cuándo debe verificarse el stock de tickets y equipos principales?", "Solo cuando ocurre una falla.", "Al inicio de cada turno.", "Una vez al mes.", "Después de retirarse.", "B", "La verificación al inicio permite anticipar faltantes y fallas operativas.", "Verificación inicial de stock"),
            Q("ACT-03-Q02", "Al iniciar el turno, el POS no enciende. ¿Qué debes hacer primero?", "Guardarlo y no decir nada.", "Reportarlo y seguir el procedimiento de contingencia autorizado.", "Abrir el equipo para repararlo.", "Pedir al cliente que pague de cualquier forma.", "B", "Las fallas deben reportarse y no deben resolverse mediante improvisación.", "Fallas de equipos y contingencia autorizada"),
            Q("ACT-03-Q03", "¿Qué elementos deben revisarse al inicio del turno?", "Solo el teléfono personal.", "Radios, tablets, impresoras, linternas y equipos de cobro.", "Únicamente la caja.", "Solo los equipos que fallaron el día anterior.", "B", "La responsabilidad incluye comprobar la operatividad de los equipos asignados.", "Revisión de equipos asignados"),
            Q("ACT-03-Q04", "Notas que quedan pocos tickets, pero todavía alcanzan para algunas horas. ¿Qué deberías hacer?", "Esperar a que se terminen.", "Informar con anticipación para gestionar reposición.", "Ocultar el faltante.", "Usar papeles sin autorización.", "B", "La finalidad de revisar el stock es prevenir la interrupción del servicio.", "Reposición preventiva de stock")
        ]),
        new(4, 2, "Gestión de incidencias", "Protege el área, comunica el hecho y registra información que permita reconstruir lo ocurrido.", "Resuelve un caso simulado: identifica la incidencia, protege el área, informa al supervisor, registra lo ocurrido y explica qué comunicarías al relevo.",
        [
            Q("ACT-04-Q01", "¿Qué información es importante registrar en una novedad?", "Solo el nombre del colaborador.", "Fecha, hora, situación, acción realizada y responsable informado.", "Únicamente una opinión personal.", "Información que no esté relacionada.", "B", "Un registro útil debe permitir reconstruir lo ocurrido y conocer las acciones realizadas.", "Contenido del registro de novedades"),
            Q("ACT-04-Q02", "Un cliente entrega una billetera encontrada en la playa. ¿Qué debes hacer?", "Guardarla en tu mochila hasta el final del turno.", "Entregarla al responsable autorizado y registrar el hecho.", "Revisar el contenido y quedarte con ella.", "Dejarla donde fue encontrada.", "B", "Los objetos encontrados deben reportarse y entregarse siguiendo la cadena interna de responsabilidad.", "Registro y entrega de objetos encontrados"),
            Q("ACT-04-Q03", "Durante tu turno ocurre una falla en una barrera de acceso y se genera congestión. ¿Qué acción es prioritaria?", "Informar al supervisor y orientar el flujo de manera segura.", "Abandonar el puesto.", "Discutir con los conductores.", "Esperar sin comunicar nada.", "A", "Se debe controlar el riesgo inmediato, orientar al cliente y activar la corrección correspondiente.", "Falla de barrera, congestión y escalamiento"),
            Q("ACT-04-Q04", "¿Por qué debe informarse una novedad al relevo?", "Para que el siguiente turno conozca pendientes y riesgos.", "Para evitar que el supervisor se entere.", "Para transferirle la culpa.", "Porque reemplaza el registro escrito.", "A", "El relevo asegura continuidad operativa; no reemplaza el registro formal.", "Continuidad operativa durante el relevo")
        ]),
        new(5, 3, "Ejecución autónoma", "Prioriza seguridad, servicio y continuidad al ejecutar el proceso con mínima asistencia.", "Realiza con mínima asistencia el inicio del turno, la verificación del puesto, la atención a un cliente, el control de acceso, el registro de una novedad y su comunicación al supervisor.",
        [
            Q("ACT-05-Q01", "Hay una fila de vehículos y, al mismo tiempo, un cliente solicita información. ¿Qué conducta es más adecuada?", "Ignorar a todos.", "Mantener la calma, orientar brevemente y controlar el flujo de forma segura.", "Levantar la barrera sin verificar.", "Responder de manera apresurada y ofensiva.", "B", "La autonomía no significa improvisar; implica priorizar seguridad, atención y continuidad.", "Priorización ante demanda simultánea"),
            Q("ACT-05-Q02", "¿Qué demuestra integridad durante una operación de cobro?", "Ocultar una diferencia de caja pequeña.", "Registrar y reportar cualquier diferencia según el procedimiento.", "Compensarla con dinero de otro turno.", "Modificar el registro para que coincida.", "B", "La integridad exige transparencia y protección de los bienes de la empresa.", "Integridad y diferencias de caja"),
            Q("ACT-05-Q03", "Un cliente te pide que le permitas salir por una zona restringida porque tiene prisa. ¿Qué debes hacer?", "Permitirlo para evitar un reclamo.", "Explicar la ruta autorizada y mantener el procedimiento.", "Dejar que decida.", "Retirar la señalización.", "B", "El buen servicio no implica incumplir controles de seguridad.", "Accesos restringidos y orientación segura"),
            Q("ACT-05-Q04", "Cuando no conoces la solución a una situación operativa, lo correcto es:", "Improvisar rápidamente.", "Consultar o escalar al responsable correspondiente.", "Ocultar la situación.", "Pedir a otro colaborador que asuma la culpa.", "B", "Una decisión responsable reconoce los límites de autoridad y utiliza el canal correspondiente.", "Límites de autoridad y escalamiento")
        ]),
        new(6, 3, "Validación integral", "Demuestra el ciclo completo del puesto y entrega el turno con trazabilidad.", "Ejecuta el ciclo completo: registra asistencia, recibe el relevo, revisa equipos y stock, verifica accesos y seguridad, atiende a un cliente, resuelve o escala una incidencia, registra la novedad y entrega el puesto.",
        [
            Q("ACT-06-Q01", "¿Qué debe contener una entrega de turno adecuada?", "Solo un saludo al relevo.", "Equipos, stock, incidencias, pendientes y condiciones del puesto.", "Únicamente el horario de salida.", "Información personal de los clientes.", "B", "El relevo debe recibir información suficiente para mantener la continuidad.", "Contenido de la entrega de turno"),
            Q("ACT-06-Q02", "Al terminar tu turno existe una incidencia que aún no ha sido resuelta. ¿Qué debes hacer?", "Retirarte sin informar porque terminó tu horario.", "Registrar la incidencia e informar al supervisor y al relevo.", "Eliminar el registro.", "Declararla resuelta.", "B", "Los pendientes deben quedar documentados y comunicados a los responsables.", "Incidencias pendientes al cierre"),
            Q("ACT-06-Q03", "¿Cuál es la relación entre buen servicio y cumplimiento de procedimientos?", "Son objetivos opuestos.", "El buen servicio debe realizarse respetando los procedimientos.", "Los procedimientos pueden ignorarse cuando el cliente insiste.", "El servicio solo depende de la rapidez.", "B", "El objetivo del puesto combina experiencia del cliente y cumplimiento operativo.", "Servicio al cliente y cumplimiento operativo"),
            Q("ACT-06-Q04", "Un jefe te solicita realizar una acción que consideras peligrosa y fuera del procedimiento. ¿Qué corresponde?", "Realizarla sin preguntar.", "Comunicar el riesgo y no ejecutar una actividad que ponga en peligro tu integridad.", "Pedir a un cliente que la haga.", "Ocultar lo ocurrido.", "B", "Ninguna instrucción debe obligar al trabajador a exponerse a un peligro inminente.", "Derecho a interrumpir una actividad peligrosa")
        ])
    ];

    private static IReadOnlyList<QuestionDefinition> BuildFinalQuestions() =>
    [
        Q("FINAL-Q01", "¿Cuál es el objetivo principal del anfitrión?", "Controlar únicamente la caja.", "Contribuir a una experiencia de calidad cumpliendo procedimientos.", "Realizar funciones administrativas externas.", "Supervisar a todos los trabajadores.", "B", "El puesto integra la experiencia del cliente con el cumplimiento de los procedimientos operativos.", "Propósito del puesto"),
        Q("FINAL-Q02", "¿Qué representa el anfitrión frente al cliente?", "La cara de la operación.", "Un auditor externo.", "Un proveedor independiente.", "Un visitante.", "A", "El trato y la presencia del anfitrión influyen directamente en la confianza y experiencia del cliente.", "Rol del anfitrión frente al cliente"),
        Q("FINAL-Q03", "¿Para qué se realiza el plaqueo digital?", "Para registrar el estado de ingreso del vehículo.", "Para registrar la asistencia.", "Para controlar el uniforme.", "Para calcular promociones.", "A", "El plaqueo mantiene un registro del estado de ingreso y aporta control sobre la operación.", "Plaqueo y control vehicular"),
        Q("FINAL-Q04", "¿Qué debe hacerse cuando un acceso se encuentra congestionado?", "Ignorarlo.", "Informar al supervisor y apoyar el control seguro.", "Cerrar todos los accesos.", "Discutir con los conductores.", "B", "La congestión exige comunicación y orientación segura, siguiendo el procedimiento autorizado de la sede.", "Congestión y control seguro de accesos"),
        Q("FINAL-Q05", "¿Qué debe verificarse al inicio del turno?", "Equipos, stock, accesos y condiciones del puesto.", "Solo los mensajes personales.", "Únicamente el uniforme del relevo.", "Nada, si el turno anterior trabajó normalmente.", "A", "La revisión inicial permite anticipar fallas, faltantes y riesgos antes de atender.", "Apertura operativa"),
        Q("FINAL-Q06", "Una impresora presenta una falla. ¿Qué corresponde?", "Ocultar la falla.", "Reportarla y aplicar el procedimiento autorizado.", "Desarmarla inmediatamente.", "Continuar hasta que se dañe por completo.", "B", "Las fallas se comunican y se atienden mediante mecanismos autorizados, sin improvisar reparaciones.", "Fallas de equipos"),
        Q("FINAL-Q07", "¿Qué se debe hacer con un objeto encontrado?", "Conservarlo personalmente.", "Reportarlo, registrarlo y entregarlo al responsable.", "Dejarlo en cualquier lugar.", "Publicarlo en redes sociales.", "B", "El registro y la entrega al responsable mantienen la trazabilidad y protegen el objeto.", "Objetos encontrados"),
        Q("FINAL-Q08", "¿Cuál es la mejor respuesta ante un cliente molesto?", "Mantener la calma, escuchar y orientar o derivar.", "Responder con el mismo tono.", "Ignorarlo.", "Prometer cualquier solución.", "A", "La atención respetuosa exige escuchar y ofrecer una orientación segura dentro del alcance del puesto.", "Atención a clientes molestos"),
        Q("FINAL-Q09", "¿Cuándo debe registrarse una incidencia?", "Solo si genera pérdidas económicas.", "Cuando ocurre durante el turno y requiere trazabilidad.", "Únicamente al final del mes.", "Nunca, si ya fue comunicada verbalmente.", "B", "La comunicación verbal no reemplaza un registro que permita reconstruir lo ocurrido.", "Registro de incidencias"),
        Q("FINAL-Q10", "¿Qué debe hacerse ante una condición insegura?", "Comunicarla y prevenir la exposición.", "Esperar a que ocurra un accidente.", "Ocultarla.", "Continuar sin tomar medidas.", "A", "La prevención exige comunicar el riesgo y evitar que trabajadores o clientes queden expuestos.", "Prevención de condiciones inseguras"),
        Q("FINAL-Q11", "Una ruta de evacuación se encuentra bloqueada. ¿Qué corresponde?", "Mantenerla así hasta el cierre.", "Gestionar que sea despejada y comunicar el riesgo.", "Usarla como almacén.", "Colocar más materiales.", "B", "Las rutas de evacuación deben permanecer libres para responder ante una emergencia.", "Rutas de evacuación"),
        Q("FINAL-Q12", "Si aparece humo durante un incendio, ¿qué indica el material de seguridad?", "Permanecer de pie.", "Desplazarse cerca del piso y seguir la ruta de escape.", "Ocultarse en un vehículo.", "Regresar por objetos personales.", "B", "Cerca del piso suele existir mayor visibilidad y menor concentración de humo; se debe seguir la ruta señalizada y a los brigadistas.", "Respuesta ante humo e incendio"),
        Q("FINAL-Q13", "¿Qué demuestra puntualidad?", "Llegar cuando ya terminó el relevo.", "Respetar el turno y permitir una entrega ordenada.", "Cambiar el horario sin informar.", "Registrar asistencia por otro trabajador.", "B", "La puntualidad permite recibir el puesto y mantener la continuidad operativa.", "Puntualidad y relevo"),
        Q("FINAL-Q14", "¿Qué debe hacerse si un cliente pregunta por una promoción desconocida?", "Inventar una respuesta.", "Confirmar la información o derivarlo.", "Aplicar cualquier descuento.", "Negar que existan promociones.", "B", "Solo se comunica información comercial vigente y confirmada.", "Promociones e información confirmada"),
        Q("FINAL-Q15", "¿Por qué se revisa el stock antes de comenzar?", "Para anticipar faltantes y evitar interrupciones.", "Para retrasar la apertura.", "Para evitar comunicarse con el supervisor.", "Para reducir el número de clientes.", "A", "Revisar el stock permite gestionar reposiciones antes de que afecten el servicio.", "Control preventivo de stock"),
        Q("FINAL-Q16", "¿Qué debe comunicarse al relevo?", "Incidencias, equipos, stock y pendientes.", "Solo conversaciones personales.", "Información que ya fue eliminada.", "Nada, si el turno terminó.", "A", "El relevo necesita conocer el estado del puesto y las acciones que continúan pendientes.", "Entrega de turno"),
        Q("FINAL-Q17", "¿Cuál es una actuación íntegra ante una diferencia de caja?", "Ocultarla.", "Registrarla y reportarla.", "Alterar los registros.", "Compensarla con otro turno.", "B", "La integridad exige transparencia y prohíbe alterar u ocultar diferencias.", "Integridad y caja"),
        Q("FINAL-Q18", "¿Qué debe hacer un anfitrión cuando una tarea está fuera de su ámbito laboral o es inapropiada?", "Realizarla siempre.", "Comunicar la situación y utilizar los canales correspondientes.", "Pedir a un cliente que la realice.", "Ocultarla.", "B", "El colaborador debe reconocer los límites de su función y usar los canales internos cuando una solicitud sea inapropiada.", "Límites del puesto y canales internos"),
        Q("FINAL-Q19", "¿Por qué es importante mantener el uniforme limpio?", "Porque forma parte de la presentación y confianza transmitida.", "Porque reemplaza los procedimientos.", "Porque evita registrar asistencia.", "Porque permite modificar el horario.", "A", "La presentación personal influye en la imagen de la operación y en la confianza del cliente.", "Presentación personal"),
        Q("FINAL-Q20", "Al iniciar el turno encuentras pocos tickets, una linterna dañada y una zona con charco. ¿Qué debes hacer?", "Empezar normalmente y no informar.", "Reportar las tres observaciones, prevenir el riesgo del charco y solicitar las acciones correspondientes.", "Resolver todo improvisando.", "Esperar al siguiente turno.", "B", "La respuesta integra control de stock, reporte de equipos y prevención inmediata de riesgos.", "Verificación integral al inicio del turno")
    ];

    private static QuestionDefinition Q(
        string key,
        string prompt,
        string a,
        string b,
        string c,
        string d,
        string correct,
        string explanation,
        string topic) => new(key, prompt, a, b, c, d, correct, explanation, topic);

    private sealed record LearningModuleDefinition(
        int SortOrder,
        string Title,
        string Summary,
        int EstimatedMinutes,
        LearningModuleContent Content);

    private sealed record ActivityDefinition(
        int Sequence,
        int WeekNumber,
        string Title,
        string Instructions,
        string PracticalChallenge,
        IReadOnlyList<QuestionDefinition> Questions);

    private sealed record QuestionDefinition(
        string Key,
        string Prompt,
        string A,
        string B,
        string C,
        string D,
        string Correct,
        string Explanation,
        string Topic);
}
