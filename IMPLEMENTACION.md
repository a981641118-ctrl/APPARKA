# Implementación funcional consolidada

## Propósito

La plataforma administra el entrenamiento de tres semanas de Apparka sin utilizar grabaciones como evidencia ordinaria. El colaborador realiza seis actividades con cuestionario automático y validación práctica; el supervisor interviene únicamente para generar el código de inicio y completar una rúbrica breve.

## Flujo obligatorio

1. RR. HH. registra al nuevo colaborador y define acceso previo, primer día, sede, puesto y supervisor.
2. El sistema crea un cronograma de 21 días con dos actividades por semana.
3. Antes del primer día, el colaborador revisa materiales de aprendizaje.
4. Cuando una actividad está disponible, el supervisor genera un código temporal de seis dígitos.
5. El colaborador ingresa el código desde su cuenta y dispositivo.
6. El sistema consume el código una sola vez y asigna un reto práctico aleatorio.
7. El colaborador responde el cuestionario sin intervención verbal del supervisor.
8. El supervisor completa una rúbrica de cinco criterios.
9. La evidencia se cierra con porcentaje, errores, respuesta correcta, justificación, comentario y trazabilidad.
10. Al completar seis evidencias se habilita el examen final, con un máximo de tres intentos.

## Código temporal corregido

- Se genera siempre con seis dígitos.
- Se guarda únicamente su hash SHA-256.
- Se vincula a una evidencia concreta.
- Tiene vigencia configurable.
- Es de un solo uso.
- Un código nuevo invalida al anterior.
- El consumo se realiza de forma atómica dentro de una transacción.
- El ingreso acepta seis dígitos con espacios o guiones y los normaliza.
- Los mensajes diferencian formato inválido, código de otra actividad, código utilizado y código vencido/reemplazado.
- La hora se almacena en UTC y se muestra en hora de Lima.

## Base de datos

La solución funciona exclusivamente con PostgreSQL. Se eliminaron el proveedor y las referencias de SQLite. La aplicación usa migraciones de Entity Framework Core mediante `Database.MigrateAsync()`.

La conexión se obtiene de:

1. `POSTGRES_CONNECTION`, para Docker o nube.
2. `ConnectionStrings:DefaultConnection`, para Visual Studio o instalación local.

## Sin reintentos en actividades

Las seis actividades son registros únicos del desempeño durante el periodo de prueba. Una calificación baja no crea otro intento; se conserva en el expediente y se brinda retroalimentación. Los tres intentos corresponden únicamente al examen final.

## Prevención de evasión

- Cuentas separadas para supervisor y colaborador.
- Código temporal generado por el supervisor.
- Reto práctico aleatorio.
- Cuestionario respondido desde la cuenta del colaborador.
- Rúbrica completada desde la cuenta del supervisor.
- Registro de IP, navegador, dispositivo, usuario y hora.
- Alerta por posible dispositivo compartido.
- Límite de intentos de código inválido.

Estos controles generan evidencia razonable sin emplear grabaciones invasivas. No sustituyen una autenticación biométrica ni una integración transaccional con los equipos de la sede.
