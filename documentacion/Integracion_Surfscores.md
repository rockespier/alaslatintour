Integración con surfscores.com

1. Eventos (/admin/eventos)

{url}:http://surfscores.com/api/v1/

En /admin/eventos


Debe exisitir un boton para importar datos. Abre un modal donde se debe seleccionar el circuito donde se crearan los eventos. 

Para los datos del evento, invocar al endpoint POST: {url}/events/organization
Enviando el token y el id de organizacion.
Ej.
{

  "token": "888|oHiZ77D8VPFDKia0klcgHgZTzwNaEydRifhSqJWZ9d7c241c" ,

   "id": 13

}

- Nombre = events.name
- Fecha inicio =  events.start_date
- Fecha Fin = events.end_date
- Pais = events.country
- Playa = events.place
- Codigo Surfscores = events.id

Para los datos de categorias habilitadas, invocar el endpoint POST: {url}/events/categories
Enviando el token y el Id de evento (codigo Surfscores).

Ej.

{

  "token": "888|oHiZ77D8VPFDKia0klcgHgZTzwNaEydRifhSqJWZ9d7c241c" ,

   "id": 478

}

Usar el campo de categoria: Codigo SurfScores para hacer el match con la respuesta de
categories.id

Reglas: 

- Si existen campos obligatorios, autocompletar con valores default.
- Todos los eventos creados con esta interfaz, se crean con estado "borrador"
- Se debe verificar si existe un eventos con el nombre similar, mostrando la advertencia y no crearlo para evitar duplicados.

2. 