using ApparkaTrainingFlowOnline.Models;
using ApparkaTrainingFlowOnline.Services;
using Microsoft.EntityFrameworkCore;

namespace ApparkaTrainingFlowOnline.Data;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<AppDbContext>();
        var clock = services.GetRequiredService<PeruClock>();
        var passwords = services.GetRequiredService<PasswordService>();

        if (!await db.Users.AnyAsync())
        {
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
                Name = "Operador de estacionamiento",
                Description = "Atención, registro de operaciones, seguridad y manejo de incidencias."
            });
        }
        await db.SaveChangesAsync();

        var positionId = await db.Positions.Select(x => x.Id).FirstAsync();
        if (!await db.LearningMaterials.AnyAsync())
        {
            db.LearningMaterials.AddRange(
                new LearningMaterial { PositionId = positionId, SortOrder = 1, Title = "Bienvenida y funciones", Summary = "Conoce el propósito del puesto, responsabilidades, horarios y canales de soporte." },
                new LearningMaterial { PositionId = positionId, SortOrder = 2, Title = "Seguridad y prevención", Summary = "Revisa los controles básicos, situaciones de riesgo y procedimiento de escalamiento." },
                new LearningMaterial { PositionId = positionId, SortOrder = 3, Title = "Atención al cliente", Summary = "Estudia el protocolo de saludo, comunicación clara y manejo respetuoso de consultas." }
            );
        }

        if (!await db.ActivityTemplates.AnyAsync())
        {
            var activities = new[]
            {
                (1, 1, "Reconocimiento y seguridad", "Identifica las zonas, equipos y riesgos principales de la sede.", "Identifica dos riesgos de la sede y explica su control.||Realiza el recorrido operativo y señala el canal de evacuación.||Ubica los equipos críticos y explica cuándo deben reportarse."),
                (2, 1, "Atención y registro básico", "Aplica el protocolo de atención y registra una operación básica.", "Realiza una atención completa con saludo, registro y cierre.||Atiende un caso con una consulta frecuente y confirma la información.||Ejecuta un registro básico y verifica el resultado antes de cerrar."),
                (3, 2, "Operación acompañada", "Ejecuta el proceso habitual con acompañamiento mínimo.", "Completa una operación real respetando la secuencia definida.||Detecta un error simulado antes del cierre y corrígelo.||Ejecuta el proceso y explica los dos controles más importantes."),
                (4, 2, "Gestión de incidencias", "Identifica y comunica correctamente una incidencia frecuente.", "Resuelve una incidencia de registro y comunica el escalamiento.||Explica qué harías ante una falla de equipo.||Atiende una discrepancia y registra la incidencia correctamente."),
                (5, 3, "Ejecución autónoma", "Realiza el proceso sin asistencia continua y verifica el resultado.", "Ejecuta la actividad principal y realiza una comprobación final.||Realiza el proceso sin ayuda y explica cómo verificaste el resultado.||Completa la operación y detecta un dato inconsistente."),
                (6, 3, "Validación integral", "Demuestra dominio general del puesto antes del examen final.", "Completa un ciclo integral y explica cómo actuar ante un error crítico.||Resuelve un caso integral con atención, registro y verificación.||Ejecuta el proceso completo y responde una incidencia asignada.")
            };

            foreach (var item in activities)
            {
                var template = new ActivityTemplate
                {
                    Sequence = item.Item1,
                    WeekNumber = item.Item2,
                    Title = item.Item3,
                    Instructions = item.Item4,
                    PracticalChallenge = item.Item5
                };
                foreach (var question in BuildActivityQuestions(item.Item1)) template.Questions.Add(question);
                db.ActivityTemplates.Add(template);
            }
        }

        if (!await db.Questions.AnyAsync(x => x.IsFinalExamQuestion))
        {
            foreach (var question in BuildFinalQuestions()) db.Questions.Add(question);
        }
        await db.SaveChangesAsync();

        if (!await db.TrainingAssignments.AnyAsync())
        {
            var collaborator = await db.Users.FirstAsync(x => x.Role == AppRoles.Collaborator);
            var supervisor = await db.Users.FirstAsync(x => x.Role == AppRoles.Supervisor);
            var hr = await db.Users.FirstAsync(x => x.Role == AppRoles.Hr);
            var location = await db.Locations.FirstAsync();
            var position = await db.Positions.FirstAsync();
            var assignment = new TrainingAssignment
            {
                CollaboratorId = collaborator.Id,
                SupervisorId = supervisor.Id,
                CreatedById = hr.Id,
                LocationId = location.Id,
                PositionId = position.Id,
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
    }

    private static IEnumerable<Question> BuildActivityQuestions(int sequence)
    {
        return new[]
        {
            new Question { Prompt = $"¿Cuál es el primer objetivo de la actividad {sequence}?", OptionA = "Seguir el procedimiento y priorizar la seguridad", OptionB = "Terminar sin verificar", OptionC = "Omitir el registro", OptionD = "Esperar que otro lo haga", CorrectOption = "A", Explanation = "La ejecución debe respetar la secuencia y los controles de seguridad antes de buscar rapidez." },
            new Question { Prompt = "¿Qué debe hacerse cuando aparece una incidencia no contemplada?", OptionA = "Ocultarla", OptionB = "Comunicarla y aplicar el canal de escalamiento", OptionC = "Improvisar sin informar", OptionD = "Cerrar el turno", CorrectOption = "B", Explanation = "Una incidencia debe registrarse y escalarse para proteger al cliente, al colaborador y a la operación." },
            new Question { Prompt = "¿Qué demuestra mejor que el proceso terminó correctamente?", OptionA = "Solo la rapidez", OptionB = "La opinión de un tercero", OptionC = "La verificación final y el registro", OptionD = "No recibir preguntas", CorrectOption = "C", Explanation = "La verificación y el registro generan trazabilidad y permiten detectar errores antes de cerrar la actividad." }
        };
    }

    private static IEnumerable<Question> BuildFinalQuestions()
    {
        var prompts = new[]
        {
            "Ante un riesgo operativo, ¿qué debe priorizarse?", "¿Cómo se registra una incidencia?", "¿Qué completa una atención correcta?",
            "¿Por qué se verifica una operación?", "¿Cuándo debe escalarse un problema?", "¿Qué evidencia es válida en la plataforma?",
            "¿Quién valida la práctica?", "¿Qué ocurre después de seis actividades?", "¿Cuál es el máximo de intentos finales?",
            "¿Qué sucede si no se aprueba en tres intentos?", "¿Qué debe hacer el colaborador antes del primer día?", "¿Qué reduce el sobretrabajo del supervisor?"
        };
        return prompts.Select((prompt, index) => new Question
        {
            IsFinalExamQuestion = true,
            Prompt = prompt,
            OptionA = index % 4 == 0 ? "Aplicar el procedimiento definido" : "Omitir los controles",
            OptionB = index % 4 == 1 ? "Registrar y comunicar por el canal establecido" : "Responder sin verificar",
            OptionC = index % 4 == 2 ? "Completar el proceso y confirmar el resultado" : "Delegar siempre",
            OptionD = index % 4 == 3 ? "Revisar la evidencia y actuar según la política" : "Ignorar la situación",
            CorrectOption = new[] { "A", "B", "C", "D" }[index % 4],
            Explanation = "La respuesta correcta se sustenta en el procedimiento, la trazabilidad y la responsabilidad compartida."
        });
    }
}
