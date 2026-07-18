
## Auth - Registro con documento de identidad

`POST /v1/auth/register` ahora espera `multipart/form-data` para soportar adjuntos.

Campos principales:

- `email` string requerido.
- `password` string requerido.
- `nombre` string requerido.
- `apellido` string requerido.
- `tipo` string requerido: `espectador` o `competidor`.
- `terminos` boolean requerido.
- `newsletter` boolean opcional.
- Para `competidor`: `reglamento`, `fechaNacimiento`, `genero`, `postura`, `tallaCamiseta`, `pais` e `identityDocument` son requeridos.
- `identityDocument` archivo: JPG, PNG, WebP o PDF.

Ejemplo form-data para competidor:

```text
email=competidor@example.com
password=Password1
nombre=Ana
apellido=García
tipo=competidor
terminos=true
newsletter=true
reglamento=true
pais=Perú
fechaNacimiento=2002-05-12
genero=Femenino
postura=Regular
tallaCamiseta=M
identityDocument=@dni.jpg
```
