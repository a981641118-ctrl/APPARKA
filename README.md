# Apparka Training Flow Online

Plataforma web responsive para gestionar el periodo de prueba de tres semanas de los nuevos colaboradores. Sustituye los seis videos por seis evidencias digitales de desempeño, conserva trazabilidad y reduce la carga operativa del supervisor.

## Tecnología

- ASP.NET Core MVC con .NET 8.
- Entity Framework Core.
- PostgreSQL como única base de datos.
- Npgsql para acceso a PostgreSQL.
- Autenticación por cookies y autorización por roles.
- Diseño mobile-first y PWA.
- Docker y Docker Compose.

## Configuración de PostgreSQL

El sistema obtiene la conexión en este orden:

1. Variable de entorno `POSTGRES_CONNECTION`.
2. `ConnectionStrings:DefaultConnection` de `appsettings.json`.

El proyecto ya no contiene configuración ni paquetes de SQLite.

Ejemplo:

```text
Host=localhost;Port=5432;Database=Apparka;Username=postgres;Password=tu_clave
```

## Abrir en Visual Studio

1. Instala Visual Studio 2022 con **Desarrollo de ASP.NET y web** y el SDK de .NET 8.
2. Abre `ApparkaTrainingFlowOnline.sln`.
3. Revisa la cadena PostgreSQL de `appsettings.json`.
4. Restaura los paquetes NuGet.
5. Aplica las migraciones:

```powershell
dotnet ef database update --project ApparkaTrainingFlowOnline/ApparkaTrainingFlowOnline.csproj
```

6. Ejecuta con `F5`.

Al iniciar, la aplicación ejecuta `Database.MigrateAsync()`. Si la migración ya fue aplicada, no vuelve a crear las tablas.

## Credenciales de demostración

| Rol | Usuario | Contraseña |
|---|---|---|
| Administrador | admin@demo.local | Admin123* |
| Recursos Humanos | rrhh@demo.local | Rrhh123* |
| Supervisor | supervisor@demo.local | Supervisor123* |
| Colaborador | colaborador@demo.local | Colaborador123* |

## Prueba del código temporal

1. Inicia sesión como supervisor.
2. En una actividad disponible, selecciona **Generar código de un uso**.
3. Copia los seis dígitos y verifica la hora de vencimiento mostrada.
4. Cierra sesión e ingresa como colaborador.
5. Abre esa misma actividad e introduce el código.
6. El sistema debe mostrar: **Código válido. La actividad fue iniciada correctamente.**
7. Intenta utilizar el mismo código nuevamente. Debe rechazarse como utilizado.
8. Genera un segundo código para la misma actividad. El anterior debe quedar reemplazado.

El código:

- Tiene exactamente seis dígitos, incluso cuando inicia con cero.
- Se almacena como hash SHA-256, no como texto visible.
- Dura el número de minutos configurado en `Training:ValidationCodeMinutes`.
- Solo puede utilizarse una vez.
- Está vinculado a la evidencia específica.
- Se consume de manera atómica para evitar reutilización simultánea.
- Acepta el ingreso con espacios o guiones, pero normaliza siempre a seis dígitos.

## Flujo general

1. RR. HH. registra al colaborador, la sede, el puesto, el supervisor y el primer día.
2. El sistema crea automáticamente seis actividades distribuidas en 21 días.
3. Antes del primer día, el colaborador accede únicamente a materiales de estudio.
4. Durante el entrenamiento, el supervisor genera el código temporal.
5. El colaborador ingresa el código desde su propia cuenta y realiza la actividad.
6. El cuestionario se corrige automáticamente.
7. El supervisor completa una rúbrica breve desde su cuenta.
8. La evidencia queda cerrada con puntaje, errores, explicación y trazabilidad.
9. Al terminar las seis evidencias se habilita el examen final con tres intentos como máximo.

## Docker

Desde la carpeta raíz:

```bash
docker compose up --build
```

La aplicación queda disponible en `http://localhost:8080`.

## Seguridad previa a producción

- Cambiar o eliminar las cuentas demo.
- Utilizar HTTPS.
- Guardar la cadena PostgreSQL y las credenciales SMTP como secretos del servidor.
- Implementar recuperación de contraseña y MFA para cuentas privilegiadas.
- Revisar la política de privacidad y retención de datos.
- Integrar registros transaccionales del estacionamiento cuando estén disponibles.
