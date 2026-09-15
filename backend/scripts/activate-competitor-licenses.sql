/*
    Activación masiva de licencias + asignación de categorías habilitadas
    =======================================================================
    Para TODOS los competidores de la tabla Competitors:
      1) Pone LicenseStatus = 'Activa' (si no tenían número de licencia, se les
         genera uno correlativo tipo ALAS-<año>-0001; si ya tenían uno, se respeta).
      2) Recalcula LicenseNumberLong con el mismo criterio que usa el backend
         (UpdateCompetitorLicenseCommandHandler): "{LicenseNumber}-{añoVencimiento}".
      3) Reconstruye CompetitorLicenseCategories con las categorías activas
         (Categories.Status = 'Activo') compatibles con:
           - Género: la categoría es 'Ambos', o coincide con Competitors.Genero
             ('Masculino'/'Femenino'). Los competidores con Genero =
             'PrefieroNoIndicar' solo califican para categorías 'Ambos'.
           - Edad: si la categoría no tiene restricción de edad (AgeRestriction=0)
             aplica siempre; si la tiene, la edad del competidor (calculada a la
             fecha de hoy a partir de FechaNacimiento) debe estar entre
             Categories.MinAge y Categories.MaxAge.

    ADVERTENCIA: el paso 3 reemplaza POR COMPLETO el contenido de
    CompetitorLicenseCategories (DELETE + INSERT) para dejarlo consistente con
    la regla de género/edad. Si ya habías curado categorías manualmente para
    algún competidor, ese ajuste manual se pierde y queda solo lo que la regla
    de elegibilidad determine.

    El script corre dentro de una transacción y no hace COMMIT automático:
    revisá el SELECT final y luego decidí COMMIT o ROLLBACK.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @ExpirationDate datetimeoffset(7) = DATEADD(YEAR, 1, SYSDATETIMEOFFSET());
DECLARE @AsOfDate date = CAST(SYSDATETIMEOFFSET() AS date);

BEGIN TRANSACTION;

-- 1) Activar licencia y generar número correlativo solo donde falte
;WITH Numerado AS (
    SELECT Id,
           ROW_NUMBER() OVER (ORDER BY CreatedAtUtc, Id) AS Secuencia
    FROM Competitors
)
UPDATE c
SET c.LicenseStatus = 'Activa',
    c.LicenseNumber = CASE
        WHEN c.LicenseNumber IS NULL OR c.LicenseNumber = ''
            THEN CONCAT('ALAS-', YEAR(@ExpirationDate), '-', RIGHT('0000' + CAST(n.Secuencia AS varchar(4)), 4))
        ELSE c.LicenseNumber
    END,
    c.LicenseExpirationDate = CASE
        WHEN c.LicenseExpirationDate IS NULL OR c.LicenseExpirationDate <= SYSDATETIMEOFFSET()
            THEN @ExpirationDate
        ELSE c.LicenseExpirationDate
    END,
    c.UpdatedAtUtc = SYSDATETIMEOFFSET()
FROM Competitors c
JOIN Numerado n ON n.Id = c.Id;

-- Recalcular el número "largo" con el mismo formato que usa el backend
UPDATE Competitors
SET LicenseNumberLong = CONCAT(LicenseNumber, '-', YEAR(LicenseExpirationDate))
WHERE LicenseNumber IS NOT NULL AND LicenseNumber <> '';

-- 2) Reconstruir categorías habilitadas según género + edad
DELETE FROM CompetitorLicenseCategories;

INSERT INTO CompetitorLicenseCategories (CompetitorId, CategoryId)
SELECT c.Id, cat.Id
FROM Competitors c
CROSS JOIN Categories cat
WHERE cat.Status = 'Activo'
  AND (
        cat.Gender = 'Ambos'
        OR (cat.Gender = 'Masculino' AND c.Genero = 'Masculino')
        OR (cat.Gender = 'Femenino'  AND c.Genero = 'Femenino')
      )
  AND (
        cat.AgeRestriction = 0
        OR (
             DATEDIFF(YEAR, CAST(c.FechaNacimiento AS date), @AsOfDate)
             - CASE
                 WHEN (MONTH(@AsOfDate) * 100 + DAY(@AsOfDate))
                      < (MONTH(c.FechaNacimiento) * 100 + DAY(c.FechaNacimiento))
                 THEN 1 ELSE 0
               END
           ) BETWEEN cat.MinAge AND cat.MaxAge
      );

-- 3) Resumen para revisar antes de confirmar
SELECT
    (SELECT COUNT(*) FROM Competitors) AS TotalCompetidores,
    (SELECT COUNT(*) FROM Competitors WHERE LicenseStatus = 'Activa') AS ConLicenciaActiva,
    (SELECT COUNT(*) FROM CompetitorLicenseCategories) AS AsignacionesCategoria,
    (SELECT COUNT(DISTINCT CompetitorId) FROM CompetitorLicenseCategories) AS CompetidoresConAlMenosUnaCategoria;

-- Revisá el resultado de arriba. Si está correcto:
-- COMMIT TRANSACTION;
-- Si algo no cuadra:
-- ROLLBACK TRANSACTION;
