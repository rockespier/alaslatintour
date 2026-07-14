# Integracion temporal de correo con Gmail SMTP

El backend envia correos mediante SMTP usando una contrasena de aplicacion de Gmail.

## Configuracion local

En `backend/src/AlasApp.Api/appsettings.Development.json`:

```json
"SmtpEmail": {
  "Enabled": true,
  "Host": "smtp.gmail.com",
  "Port": 587,
  "EnableSsl": true,
  "Username": "tu-cuenta@gmail.com",
  "Password": "tu-contrasena-de-aplicacion",
  "FromEmail": "tu-cuenta@gmail.com",
  "FromName": "ALAS Latin Tour"
}
```

Para Gmail, `Password` debe ser una contrasena de aplicacion, no la contrasena normal de la cuenta.

## Flujos conectados

- `POST /v1/auth/password-reset/request`: genera token de recuperacion y lo envia por correo al usuario si existe.
- `POST /v1/payments/beach/tokens/{tokenId}/approve`: aprueba el token de pago en playa y envia el codigo al correo del competidor.

El texto del correo de token de playa usa la plantilla de configuracion admin.

Placeholders soportados: `[EVENTO]`, `[TOKEN]`, `[CATEGORIA]`, `[COMPETIDOR]`, `[MONTO]`.

## Logs

El backend registra en consola:

- envio omitido cuando `SmtpEmail:Enabled=false`
- inicio de envio SMTP
- envio exitoso
- rechazo/error SMTP
