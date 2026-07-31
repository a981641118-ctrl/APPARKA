-- Diagnóstico de sesiones de código temporal en PostgreSQL.
-- Ejecutar después de generar un código desde el panel del supervisor.

SELECT
    vs."Id",
    vs."EvidenceId",
    ae."AssignmentId",
    ae."Sequence",
    ta."CollaboratorId",
    ta."SupervisorId",
    vs."GeneratedById",
    vs."ExpiresAt",
    vs."UsedAt",
    vs."UsedById",
    CASE
        WHEN vs."UsedAt" IS NOT NULL THEN 'UTILIZADO'
        WHEN vs."ExpiresAt" <= NOW() THEN 'VENCIDO_O_REEMPLAZADO'
        ELSE 'ACTIVO'
    END AS "EstadoCalculado"
FROM "ValidationSessions" vs
INNER JOIN "ActivityEvidences" ae ON ae."Id" = vs."EvidenceId"
INNER JOIN "TrainingAssignments" ta ON ta."Id" = ae."AssignmentId"
ORDER BY vs."Id" DESC
LIMIT 30;

-- Debe existir como máximo un código activo por evidencia.
SELECT
    "EvidenceId",
    COUNT(*) AS "CodigosActivos"
FROM "ValidationSessions"
WHERE "UsedAt" IS NULL
  AND "ExpiresAt" > NOW()
GROUP BY "EvidenceId"
HAVING COUNT(*) > 1;
