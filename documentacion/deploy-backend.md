# Deploy Backend ALAS

## Objetivo

Este documento resume lo necesario para desplegar el backend de `AlasApp.Api` en un ambiente nuevo o existente.

Aplica a:

- migraciones EF Core
- configuración de base de datos SQL Server
- variables de entorno
- bootstrap del primer `Super Admin`
- validaciones mínimas post-deploy

---

## 1. Requisitos

- `.NET SDK 9`
- SQL Server accesible desde el backend
- cadena de conexión válida para `ConnectionStrings:AlasApp`
- acceso al proyecto:
  - `backend/src/AlasApp.Api`
  - `backend/src/AlasApp.Infrastructure`

---

## 2. Configuración mínima

### Base de datos

La API usa SQL Server.

Configurar:

```json
{
  "ConnectionStrings": {
    "AlasApp": "Server=localhost;Initial Catalog=AlasApp;Integrated Security=True;TrustServerCertificate=True;Encrypt=True;"
  }
}
```

También puede configurarse por variable de entorno:

```powershell
$env:ConnectionStrings__AlasApp="Server=localhost;Initial Catalog=AlasApp;Integrated Security=True;TrustServerCertificate=True;Encrypt=True;"
```

### JWT

Configurar al menos:

```json
{
  "Jwt": {
    "Issuer": "AlasApp.Api",
    "Audience": "AlasApp.Client",
    "SigningKey": "CAMBIAR-ESTA-CLAVE-EN-PRODUCCION",
    "AccessTokenExpirationMinutes": 60,
    "RememberMeExpirationDays": 30
  }
}
```

### WordPress

Configurar:

```json
{
  "WordPressConfig": {
    "PostsBaseUrl": "https://alasglobaltour.rtres.net/wp-json/wp/v2/posts",
    "GalleriesBaseUrl": "https://alasglobaltour.rtres.net/wp-json/wp/v2/gallery",
    "Username": "dotnet-bff-service",
    "AppPassword": "CAMBIAR-O-CONFIGURAR-SEGUN-AMBIENTE"
  }
}
```

Notas:

- `BaseUrl` sigue existiendo como fallback por compatibilidad.
- Se recomienda usar explícitamente `PostsBaseUrl` y `GalleriesBaseUrl`.

---

## 3. Aplicar migraciones

Desde `backend/src/AlasApp.Infrastructure`:

```powershell
dotnet ef database update --project . --startup-project ..\AlasApp.Api --context AlasAppDbContext
```

Si necesitas crear una nueva migración:

```powershell
dotnet ef migrations add NombreMigracion --project . --startup-project ..\AlasApp.Api --context AlasAppDbContext --output-dir Persistence\Migrations
```

Notas:

- No usar espacios en el nombre de migración.
- Ejemplo correcto: `Lote7`, `Lote7_WordPress`, `BootstrapAdmin`.

---

## 4. Bootstrap del primer Super Admin

El primer `Super Admin` no se crea desde un endpoint público.

La API soporta bootstrap automático al arranque.

### Configuración en `appsettings`

```json
{
  "BootstrapAdmin": {
    "Enabled": true,
    "Email": "superadmin@alas.local",
    "Password": "Password1!",
    "Nombre": "Super",
    "Apellido": "Admin"
  }
}
```

### Configuración por variables de entorno

```powershell
$env:BootstrapAdmin__Enabled="true"
$env:BootstrapAdmin__Email="superadmin@alas.local"
$env:BootstrapAdmin__Password="Password1!"
$env:BootstrapAdmin__Nombre="Super"
$env:BootstrapAdmin__Apellido="Admin"
```

### Comportamiento

- Si `Enabled = false`, no hace nada.
- Si ya existe un usuario con rol `Super Admin`, no crea otro.
- Si falta `Email`, `Password`, `Nombre` o `Apellido`, no crea nada.
- Si el email configurado ya existe pero no hay `Super Admin`, no promueve automáticamente esa cuenta.

### Flujo recomendado

1. Configurar `BootstrapAdmin.Enabled = true`.
2. Desplegar o reiniciar la API.
3. Hacer login con ese usuario.
4. Verificar acceso a `/v1/admin/dashboard`.
5. Volver a dejar `BootstrapAdmin.Enabled = false`.

---

## 5. Ejecutar la API

Desde `backend/src/AlasApp.Api`:

```powershell
dotnet run
```

Al arrancar:

1. La API crea el host.
2. Si el ambiente no es `Testing`, ejecuta `Database.MigrateAsync()`.
3. Ejecuta el bootstrap del `Super Admin`.
4. Expone los endpoints `v1`.

---

## 6. Validaciones post-deploy

### Salud mínima funcional

Verificar:

1. Login:

```http
POST /v1/auth/login
```

2. Endpoint admin protegido:

```http
GET /v1/admin/dashboard
Authorization: Bearer {{access_token}}
```

3. WordPress noticias:

```http
GET /v1/articles?page=1&limit=5
```

4. WordPress galerías:

```http
GET /v1/galleries
GET /v1/galleries/{slug}
```

5. Rankings:

```http
GET /v1/rankings/categories
```

### Resultados esperados

- `auth/login` responde `200`
- `/v1/admin/dashboard` responde `200` con un `Super Admin`
- `/v1/articles` responde `200`
- `/v1/galleries` responde `200`
- `/v1/rankings/categories` responde `200`

---

## 7. Problemas conocidos y diagnóstico rápido

### Error de EF: no se puede crear `DbContext`

Si aparece un error como:

`Unable to create a 'DbContext'...`

usar:

```powershell
dotnet ef database update --project . --startup-project ..\AlasApp.Api --context AlasAppDbContext
```

Ya existe una `IDesignTimeDbContextFactory` para `AlasAppDbContext`.

### Error con objetos faltantes en SQL Server

Si aparecen errores como:

`No se encuentra el objeto 'Competitors'`

normalmente indica que:

- la base no está migrada
- hay una migración parcial
- se apuntó a una base incorrecta

Acción:

1. verificar `ConnectionStrings:AlasApp`
2. ejecutar `dotnet ef database update`

### Error de archivos bloqueados al compilar

Si `dotnet build` falla porque `AlasApp.Api.dll` o dependencias están bloqueadas:

- cerrar la ejecución local de la API
- detener debugging en Visual Studio
- volver a correr `dotnet build`

### Bootstrap admin no crea el usuario

Revisar:

1. `BootstrapAdmin.Enabled = true`
2. `Email`, `Password`, `Nombre`, `Apellido` completos
3. que no exista ya un `Super Admin`
4. que la API tenga permisos para escribir en la base

---

## 8. Recomendaciones operativas

- No dejar `BootstrapAdmin.Enabled = true` permanentemente.
- Cambiar `Jwt:SigningKey` por una clave fuerte por ambiente.
- Mantener `WordPressConfig` por variables de entorno en producción.
- Ejecutar migraciones antes de abrir tráfico al frontend.
- Verificar manualmente `auth`, `admin`, `articles` y `galleries` después de cada deploy.

### *DEPLOY PARA STAGE*

api   --> https://AlasAppApi.gestionaminegocio.com
front --> https://Alasglobaltour.gestionaminegocio.com
wp	  --> https://alasglobaltour.rtres.net

