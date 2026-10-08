# Página "Quiénes somos" editable en WordPress

La web lee los textos de introducción, historia, misión y visión desde una página de WordPress
(`GET /v1/pages/quienes-somos?lang=`). Si la página no existe, falta una sección o WordPress no
responde, la web muestra el texto que trae por defecto. Los contadores, la línea de tiempo y el
equipo siguen en el código.

## Configuración inicial (una vez)

1. **Snippet de Polylang**: copiar en WordPress la versión actual de `documentacion/functions.php`
   (bloque "Fuerza el filtro por idioma…"). Ahora incluye `page` además de `post` y `gallery`.
2. **Polylang → Ajustes → Tipos de contenido**: verificar que **Páginas** sea traducible.
3. **Crear la página en español**: Páginas → Añadir nueva.
   - Título: `Quiénes somos`. **Slug: `quienes-somos`** (obligatorio, es como la encuentra la web).
   - Editor → menú ⋮ → *Editor de código* → pegar el contenido de `es.html` → *Salir del editor de código*.
   - Publicar.
4. **Traducciones**: en la columna de idiomas de Polylang, pulsar **+** de EN y PT, pegar `en.html` / `pt.html`
   del mismo modo y publicar. El slug de las traducciones puede ser cualquiera (ej. `about-us`, `quem-somos`).

## Cómo se edita

La estructura de la página es fija, en este orden:

| Parte de la página | Qué representa en la web |
|---|---|
| Párrafos **antes del primer título** | Introducción (debajo de "Quiénes Somos") |
| 1.er título (H2) + párrafos | Historia ("De un torneo de naciones…") |
| 2.º título (H2) + párrafos | Los orígenes |
| 3.er título (H2) + párrafos | Tour Latino Profesional |
| 4.º título (H2) + párrafos | Tarjeta **Misión** |
| 5.º título (H2) + párrafos | Tarjeta **Visión** |
| 6.º título (H2) + párrafos | Tarjeta **Alcance** |

- Se pueden cambiar libremente los textos de títulos y párrafos, y añadir o quitar párrafos.
- **No** cambiar el orden ni el nivel de los títulos (deben ser *Encabezado H2*). Para un subtítulo dentro de una sección, usar negrita, no otro H2.
- Se muestra solo texto: negritas, enlaces o imágenes no se ven en la web.
- Si una sección queda sin párrafos, la web usa su texto por defecto.
- Los cambios se ven en la web en un máximo de **10 minutos** (caché del backend).
