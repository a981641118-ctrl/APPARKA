# Requerimientos funcionales reformulados

## Objetivo

Desarrollar una plataforma web responsive, accesible desde cualquier dispositivo, que gestione y valide el entrenamiento de los nuevos colaboradores de Apparka durante un periodo de prueba de tres semanas. La plataforma reemplaza las grabaciones por evidencias digitales menos invasivas, reduce el trabajo del supervisor y entrega a RR. HH. información objetiva sobre el desempeño y aptitud del colaborador.

## Actores

- **Administrador:** configuración general, usuarios, sedes, puestos y auditoría.
- **Recursos Humanos:** alta del colaborador, asignación, fechas y revisión del resultado final.
- **Supervisor:** generación de códigos y validación de la ejecución práctica.
- **Nuevo colaborador:** estudio previo, actividades, retroalimentación y examen final.

## Reglas del periodo de prueba

- El periodo dura 21 días desde el primer día registrado.
- Se programan dos actividades por semana, seis en total.
- Cada actividad tiene fecha de apertura y vencimiento automáticas.
- Una actividad no se habilita hasta que llegue su fecha y se complete la anterior.
- Las seis actividades son evidencias únicas y no permiten reintento.
- Una calificación baja se conserva como diagnóstico y no obliga a repetir la actividad.
- El colaborador visualiza sus errores, la respuesta correcta y la explicación justificada.
- Al completar las seis actividades se habilita el examen final.
- El examen final permite tres intentos como máximo.
- El colaborador queda APTO al alcanzar el puntaje mínimo y NO APTO al agotar los tres intentos.

## Validación obligatoria de la actividad

Para iniciar una actividad deben concurrir dos identidades:

1. El supervisor genera un código temporal para una evidencia específica.
2. El colaborador lo ingresa desde su propia cuenta.

El código debe ser de seis dígitos, de un solo uso, con vigencia configurable, asociado a la evidencia y almacenado mediante hash. Un código nuevo reemplaza al anterior. Cada uso registra fecha, hora, IP y dispositivo.

## Eficiencia del supervisor

El colaborador responde el cuestionario desde su propio dispositivo. La plataforma corrige automáticamente las respuestas. El supervisor no realiza la encuesta verbal ni presta su teléfono; solo genera el código y completa una rúbrica de cinco criterios. Su panel prioriza actividades pendientes, vencimientos, resultados deficientes y alertas de coincidencia de dispositivo.

## Preparación previa

Una vez seleccionado, el colaborador recibe un enlace de activación antes de su primer día. Durante esta etapa accede a contenidos sobre sus tareas, seguridad y atención al cliente. Las actividades prácticas permanecen bloqueadas hasta la fecha de inicio.

## Trazabilidad

La plataforma registra usuario, rol, acción, entidad, fecha y hora, dirección IP, navegador, resultado y alertas. RR. HH. puede revisar el expediente digital completo del colaborador.

## Requerimientos técnicos

- Aplicación ASP.NET Core MVC .NET 8.
- PostgreSQL como única base de datos.
- Entity Framework Core con migraciones.
- Autorización basada en roles.
- Diseño mobile-first y PWA.
- Validaciones críticas ejecutadas en el servidor.
- Protección CSRF, cookies seguras, limitación de intentos y auditoría.
- Configuración por `appsettings.json` o variables de entorno.
