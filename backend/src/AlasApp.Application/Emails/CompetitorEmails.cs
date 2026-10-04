using AlasApp.Application.Payments.Models;

namespace AlasApp.Application.Emails;

/// <summary>
/// Fixed copy of the transactional emails sent to competitors, in the account's preferred language.
/// Emails to ALAS staff stay in Spanish and don't go through here.
/// </summary>
public static class CompetitorEmails
{
    private static string Footer(string lang) => EmailLanguage.Pick(lang,
        "Este mensaje fue enviado automaticamente por ALAS Global Tour.",
        "This message was sent automatically by ALAS Global Tour.",
        "Esta mensagem foi enviada automaticamente pelo ALAS Global Tour.");

    public static EmailContent PasswordReset(string lang, string token)
    {
        string T(string es, string en, string pt) => EmailLanguage.Pick(lang, es, en, pt);

        var subject = T(
            "Recuperacion de contrasena ALAS Global Tour",
            "ALAS Global Tour password recovery",
            "Recuperação de senha ALAS Global Tour");

        var text = T(
            $"""
            Recibimos una solicitud para restablecer tu contrasena de ALAS Global Tour.

            Usa este token para confirmar la nueva contrasena:
            {token}

            El token expira en 30 minutos. Si no solicitaste este cambio, ignora este correo.
            """,
            $"""
            We received a request to reset your ALAS Global Tour password.

            Use this token to confirm your new password:
            {token}

            The token expires in 30 minutes. If you didn't request this change, ignore this email.
            """,
            $"""
            Recebemos uma solicitação para redefinir sua senha do ALAS Global Tour.

            Use este token para confirmar a nova senha:
            {token}

            O token expira em 30 minutos. Se você não solicitou esta alteração, ignore este e-mail.
            """);

        var html = TransactionalEmailTemplate.Render(
            lang,
            T("Seguridad", "Security", "Segurança"),
            T("Restablece tu contrasena", "Reset your password", "Redefina sua senha"),
            T(
                "Recibimos una solicitud para restablecer tu contrasena de ALAS Global Tour. Ingresa este token en la pantalla de recuperacion para continuar.",
                "We received a request to reset your ALAS Global Tour password. Enter this token on the recovery screen to continue.",
                "Recebemos uma solicitação para redefinir sua senha do ALAS Global Tour. Insira este token na tela de recuperação para continuar."),
            T("Token de recuperacion", "Recovery token", "Token de recuperação"),
            token,
            [
                new EmailDetail(T("Validez", "Valid for", "Validade"), T("30 minutos", "30 minutes", "30 minutos")),
                new EmailDetail(T("Uso", "Use", "Uso"), T("Un solo intento de recuperacion", "A single recovery attempt", "Uma única tentativa de recuperação"))
            ],
            T(
                "Si no solicitaste este cambio, puedes ignorar este correo. Tu contrasena actual seguira siendo valida.",
                "If you didn't request this change, you can ignore this email. Your current password will remain valid.",
                "Se você não solicitou esta alteração, pode ignorar este e-mail. Sua senha atual continuará válida."),
            Footer(lang));

        return new EmailContent(subject, text, html);
    }

    /// <param name="instructions">
    /// Body configured by the admin in AdminSettings (free text, Spanish only for now): it is used
    /// as-is in every language; only the fixed text around it is translated.
    /// </param>
    public static EmailContent BeachTokenApproved(string lang, BeachTokenAdminDto token, string instructions)
    {
        string T(string es, string en, string pt) => EmailLanguage.Pick(lang, es, en, pt);

        var expiration = token.ExpiracionAt?.ToString("dd/MM/yyyy HH:mm")
            ?? T("24 horas desde la aprobacion", "24 hours from approval", "24 horas a partir da aprovação");

        var html = TransactionalEmailTemplate.Render(
            lang,
            T("Pago en playa", "Beach payment", "Pagamento na praia"),
            T("Tu token de pago fue aprobado", "Your payment token was approved", "Seu token de pagamento foi aprovado"),
            instructions,
            T("Codigo token", "Token code", "Código do token"),
            token.TokenCode ?? string.Empty,
            [
                new EmailDetail(T("Evento", "Event", "Evento"), token.Event),
                new EmailDetail(T("Categoria", "Category", "Categoria"), token.Category),
                new EmailDetail(T("Competidor", "Competitor", "Competidor"), token.CompetitorName),
                new EmailDetail(T("Monto", "Amount", "Valor"), $"USD {token.AmountUsd:0.##}"),
                new EmailDetail(T("Valido hasta", "Valid until", "Válido até"), expiration)
            ],
            T(
                "Usa este codigo en la web, sección 'Mis Inscripciones' para completar tu inscripción con pago en efectivo. El token es personal y vence a las 24 horas.",
                "Use this code on the website, in 'My Registrations', to complete your registration with cash payment. The token is personal and expires after 24 hours.",
                "Use este código no site, na seção 'Minhas Inscrições', para concluir sua inscrição com pagamento em dinheiro. O token é pessoal e expira em 24 horas."),
            Footer(lang));

        var subject = T(
            $"Token de pago para {token.Event}",
            $"Payment token for {token.Event}",
            $"Token de pagamento para {token.Event}");

        return new EmailContent(subject, instructions, html);
    }

    public static EmailContent PaymentConfirmed(
        string lang,
        string competitorName,
        string eventName,
        string categoryName,
        decimal amountUsd)
    {
        string T(string es, string en, string pt) => EmailLanguage.Pick(lang, es, en, pt);

        var html = TransactionalEmailTemplate.Render(
            lang,
            T("Pago confirmado", "Payment confirmed", "Pagamento confirmado"),
            T("Tu pago fue confirmado", "Your payment was confirmed", "Seu pagamento foi confirmado"),
            T(
                $"Hola {competitorName}, tu inscripción a {eventName} ha sido validada. ¡Ya estás oficialmente inscrito!",
                $"Hi {competitorName}, your registration for {eventName} has been validated. You're officially registered!",
                $"Olá {competitorName}, sua inscrição em {eventName} foi validada. Você já está oficialmente inscrito!"),
            T("Evento", "Event", "Evento"),
            eventName,
            [
                new EmailDetail(T("Categoría", "Category", "Categoria"), categoryName),
                new EmailDetail(T("Monto pagado", "Amount paid", "Valor pago"), $"USD {amountUsd:0.##}"),
                new EmailDetail(T("Estado", "Status", "Status"), T("Confirmado ✓", "Confirmed ✓", "Confirmado ✓")),
            ],
            T(
                "Conserva este correo como comprobante de tu inscripción.",
                "Keep this email as proof of your registration.",
                "Guarde este e-mail como comprovante da sua inscrição."),
            T(
                "Notificación automática de ALAS Global Tour.",
                "Automatic notification from ALAS Global Tour.",
                "Notificação automática do ALAS Global Tour."));

        var text = T(
            $"Hola {competitorName}, tu pago para {eventName} / {categoryName} fue confirmado. Monto: USD {amountUsd:0.##}.",
            $"Hi {competitorName}, your payment for {eventName} / {categoryName} was confirmed. Amount: USD {amountUsd:0.##}.",
            $"Olá {competitorName}, seu pagamento para {eventName} / {categoryName} foi confirmado. Valor: USD {amountUsd:0.##}.");

        var subject = T(
            $"Pago confirmado — {eventName}",
            $"Payment confirmed — {eventName}",
            $"Pagamento confirmado — {eventName}");

        return new EmailContent(subject, text, html);
    }
}
