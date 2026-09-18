/*
    Backfill de Payments a partir de Inscriptions
    =======================================================================
    Contexto: la pestaña "Resumen" de /admin/pagos (KPIs, gráfico de
    recaudación mensual y "Transacciones recientes") lee EXCLUSIVAMENTE de
    la tabla Payments, no de Inscriptions. Los flujos normales de la app
    (captura de PayPal, canje de token de playa) crean una fila en Payments
    por cada Inscription. Si las inscripciones se cargaron masivamente por
    SQL directo a la tabla Inscriptions, nunca se generó la fila
    correspondiente en Payments, y por eso /admin/pagos aparece vacío aunque
    /admin/inscritos sí muestra los datos (esa pantalla lee Inscriptions
    directamente).

    Este script crea, para cada Inscription que todavía no tenga un Payment
    asociado, la fila equivalente en Payments:
      - Method  = Inscriptions.PaymentMethod   ('Paypal' / 'Beach')
      - Status  = 'Confirmado' si Inscriptions.EstadoAdmin = 'Pagado',
                  'Pendiente' en caso contrario
      - AmountUsd = Inscriptions.MontoUsd
      - Fecha / CreatedAtUtc / UpdatedAtUtc = Inscriptions.InscripcionAt
      - TransactionId = Inscriptions.TransaccionId, sufijado con el Id de la
        inscripción para garantizar unicidad (Payments.TransactionId tiene
        índice UNIQUE; el mismo código de token de playa puede repetirse en
        varias inscripciones cuando cubre varias categorías). Si
        TransaccionId es NULL (no seteado en la carga masiva), se usa
        'BULK' como prefijo.

    NOTA sobre las tarjetas KPI superiores ("Total recaudado (mes)", "Pagos
    PayPal confirmados", "Pagos en playa validados"): esas 3 solo suman
    Payments cuyo Fecha cae en el MES CALENDARIO ACTUAL (es el diseño de
    GetPaymentKpisQueryHandler). Si las inscripciones cargadas tienen
    InscripcionAt de meses anteriores, esas 3 tarjetas seguirán en $0
    después de correr este script — es esperado, no un bug. En cambio la
    tabla "Transacciones recientes" y el gráfico "Recaudación mensual"
    (últimos 5 meses) sí van a mostrar las filas nuevas.
    ("Membresías activas" es un KPI aparte, hardcodeado a 0 en el backend
    independientemente de los datos — no relacionado con este backfill.)

    El script corre dentro de una transacción y no hace COMMIT automático:
    revisá el SELECT final y luego decidí COMMIT o ROLLBACK.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

INSERT INTO Payments (Id, InscriptionId, Method, AmountUsd, TransactionId, Status, Notes, Fecha, CreatedAtUtc, UpdatedAtUtc)
SELECT
    NEWID(),
    i.Id,
    i.PaymentMethod,
    i.MontoUsd,
    LEFT(CONCAT(COALESCE(i.TransaccionId, 'BULK'), '-', CONVERT(varchar(36), i.Id)), 100),
    CASE WHEN i.EstadoAdmin = 'Pagado' THEN 'Confirmado' ELSE 'Pendiente' END,
    N'Backfill automático desde carga masiva de inscripciones.',
    CAST(CAST(i.InscripcionAt AS date) AS datetimeoffset),
    i.InscripcionAt,
    i.InscripcionAt
FROM Inscriptions i
LEFT JOIN Payments p ON p.InscriptionId = i.Id
WHERE p.Id IS NULL;

-- Resumen para revisar antes de confirmar
SELECT
    (SELECT COUNT(*) FROM Inscriptions) AS TotalInscripciones,
    (SELECT COUNT(*) FROM Payments) AS TotalPayments,
    (SELECT COUNT(*) FROM Payments WHERE Status = 'Confirmado') AS PaymentsConfirmados,
    (SELECT COUNT(*) FROM Payments WHERE Status = 'Pendiente') AS PaymentsPendientes,
    (SELECT SUM(AmountUsd) FROM Payments WHERE Status = 'Confirmado') AS TotalConfirmadoUsd;

-- Revisá el resultado de arriba. Si está correcto:
-- COMMIT TRANSACTION;
-- Si algo no cuadra:
-- ROLLBACK TRANSACTION;
