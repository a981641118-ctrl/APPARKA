# Corrección del código temporal de validación

## Problema reportado

El supervisor generaba un código para la actividad, pero el colaborador recibía el mensaje de código inválido y no podía continuar con el proceso.

## Hallazgos técnicos

La implementación anterior tenía varios puntos frágiles:

1. La consulta filtraba desde el inicio únicamente códigos vigentes y no utilizados. Por ello, cualquier diferencia de vigencia, estado o actividad terminaba en un único mensaje genérico y no permitía diagnosticar el caso real.
2. El ingreso se normalizaba solo con `Trim()`. Espacios intermedios o guiones usados al copiar el código generaban un hash diferente.
3. Se utilizaban referencias de hora local y UTC en diferentes partes del flujo.
4. El consumo del código no era atómico, por lo que dos solicitudes simultáneas podían intentar usar la misma sesión.
5. `Program.cs` seguía usando `EnsureCreatedAsync`, aunque el proyecto ya contaba con migraciones.
6. El paquete de SQLite todavía permanecía en el proyecto.
7. Docker configuraba `POSTGRES_CONNECTION`, pero la aplicación no leía dicha variable.

## Correcciones aplicadas

- Se creó `ValidationCodeService` para utilizar el mismo algoritmo de generación, normalización y hash.
- El código se genera con `ToString("D6")`, asegurando exactamente seis dígitos y conservando ceros iniciales.
- Se aceptan espacios o guiones al ingresar el código y se normaliza a seis dígitos ASCII.
- La validación distingue entre código inexistente, vencido/reemplazado, utilizado o con formato inválido.
- Al crear un nuevo código se invalidan los códigos activos anteriores de la misma evidencia.
- El código se consume mediante una actualización atómica dentro de una transacción.
- Todas las fechas persistidas y comparadas utilizan UTC; la interfaz las presenta en hora de Lima.
- Se reemplazó `EnsureCreatedAsync()` por `MigrateAsync()`.
- Se eliminó el paquete `Microsoft.EntityFrameworkCore.Sqlite`.
- `Program.cs` admite `POSTGRES_CONNECTION` y `ConnectionStrings:DefaultConnection`.
- La interfaz muestra el código, la evidencia relacionada, la fecha exacta de vencimiento y un botón para copiar.

## Migraciones

No se modificó el modelo de datos, por lo que esta corrección no necesita una migración adicional. Se conserva la migración PostgreSQL existente.

## Casos de prueba

### Código válido

- Generar el código como supervisor.
- Ingresarlo en la misma evidencia como colaborador antes de su vencimiento.
- Resultado esperado: la actividad pasa a `InProgress`.

### Código reutilizado

- Intentar ingresar nuevamente el mismo código.
- Resultado esperado: mensaje específico indicando que ya fue utilizado.

### Código vencido

- Esperar a que supere la vigencia configurada.
- Resultado esperado: mensaje indicando que venció o fue reemplazado.

### Código reemplazado

- Generar un código y luego generar otro para la misma evidencia.
- Resultado esperado: el primer código deja de ser válido y el segundo funciona.

### Código de otra actividad

- Utilizar un código generado para una evidencia distinta.
- Resultado esperado: mensaje indicando que no corresponde a la actividad.

### Entrada con formato visual

- Ingresar el código como `123-456` o `123 456`.
- Resultado esperado: el servidor lo normaliza y lo valida correctamente.
