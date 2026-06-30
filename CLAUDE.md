## Documento SDD: Plataforma ALAS Latin Tour PWA
## 1. Visión: Qué hace el producto y qué problema resuelve
El producto es la nueva plataforma web oficial del ALAS Latin Tour (Asociación Latinoamericana de Surfistas Profesionales). Se trata de una solución integral web y móvil (PWA) que centraliza el contenido corporativo, las noticias, la gestión de eventos, el ranking de surfistas y los procesos de inscripción de competencias.
Resuelve tres problemas principales: reemplaza la plataforma obsoleta escrita en ASP clásico por un stack moderno, simplifica los dolores y fricciones durante los procesos de pago e inscripción de los deportistas, y elimina la doble digitación de resultados al enlazarse automatizadamente con el sistema de jueces (API Refresh SurfScores).

## 2. Usuarios y Casos de Uso
El sistema contará con tres perfiles principales de interacción:

Espectador (Público): Navega por el sitio viendo las secciones de inicio, quiénes somos, eventos, ranking en vivo, noticias, fotos y formulario de contacto. Consume estadísticas y resultados de olas sin necesidad de iniciar sesión.

Competidor: Se registra para acceder a un panel propio. Se inscribe a las competencias en un flujo simplificado de 3 pasos, realiza pagos de inscripción online (vía PayPal) o solicita un token para pago en la playa. Exporta su calendario e historial de puntos.

Organizador / Administrador: Posee acceso a dos consolas. Utiliza WordPress para la carga de contenido editorial (Noticias y Fotos). Y usa el CMS de la aplicación en .NET para configurar circuitos, parámetros de ranking, eventos, categorías, tarifas basadas en las "estrellas" del evento, visualizar inscritos, validar pagos físicos y acceder al dashboard financiero.

## 3. Flujo de Usuario y Arquitectura (Vista General)

Flujo General: El competidor o espectador accede a la plataforma por web. El sistema carga una aplicación ultrarrápida compilada del lado del servidor (SSR) para mantener un posicionamiento óptimo. Si un usuario intenta pagar en físico, activa el "flujo de token temporal" que requiere autorización administrativa.

Manejo de errores generales: Si el backend pierde contacto temporal con la API externa de resultados, el sistema utilizará la caché del servidor en .NET para servir el último ranking extraído, evitando un quiebre de la interfaz. Si un token de inscripción física expira (pasados los 20 minutos estipulados), la interfaz de Angular invitará amigablemente a solicitar otro.

## 4. Visión del producto (Resumen Ejecutivo)
La plataforma ALAS Latin Tour es una Progressive Web App (PWA) de última generación que unifica la gestión de contenidos, pagos de inscripción e integración de puntajes en tiempo real. Su propósito es elevar la experiencia digital de deportistas y fans, optimizando la labor administrativa del circuito latinoamericano de surf.

## 5. Funcionalidades (Módulos de Alto Nivel)

Módulo Editorial (Headless CMS): Consumo de artículos ("Noticias") y galerías ("Fotos") desde el panel externo de WordPress.

Módulo de Competencias y Usuarios (Core .NET): Gestión de permisos/roles, creación de circuitos, eventos, categorías de surf, asignación de tarifas dinámicas, puestos e inscritos. Panel del competidor (exportación de calendario).

Módulo de Pagos: Check-out simplificado con pasarela PayPal y motor de generación de códigos alfanuméricos enlazados a correos electrónicos.

Módulo de Rankings (Integración Refresh API): Sincronización basada en códigos externos para enlazar circuitos y deportistas con la API de SurfScores, extrayendo posiciones de liga, resultados de hits y detalles de olas.

## 6. Flujos de usuario (Inscripción con Pago en Playa)
Para documentar un flujo crítico exacto:

El Competidor inicia sesión, entra al evento y selecciona su categoría.

En la pantalla de pago de la página de inscripción, selecciona la opción "Pago en playa (Efectivo)" y hace clic en solicitar código.

El sistema en .NET notifica vía correo al administrador.

El administrador aprueba la solicitud a través del CMS.

El sistema genera un token aleatorio de uso único con una validez de 24 horas y lo envía al correo del competidor. El token es válido para todas las categorías seleccionadas en el mismo proceso de inscripción.

El competidor introduce el token en la PWA; su inscripción se habilita pero el sistema marca internamente el estado financiero como "pendiente".

Escenario de error: Si el competidor introduce el código después de 24 horas de su emisión, el BFF de .NET rechaza el token devolviendo un estado HTTP 400. Angular le notifica el error y le da un botón de "Re-solicitar token".

## 7. Arquitectura Técnica

Frontend PWA: Construido en Angular versión moderna, aplicando de forma obligatoria Server-Side Rendering (SSR). Permite acceso al hardware e implementa Service Workers para recibir notificaciones push.

Backend / BFF: Aplicación bajo .NET Core 9 utilizando Clean Architecture. Actuará como escudo y mediador de los datos.

Conexiones de Red: * El Backend .NET llamará a la API REST de WordPress para extraer el JSON de las noticias, lo limpiará y se lo pasará a Angular.

El Backend .NET utilizará credenciales seguras (correo, contraseña y confirmación de política) mediante HTTPS JSON hacia surfscores.com/api/v1/users/login para recibir el token JWT de la sesión. Con este token se extraerán los rankings de competidores mediante los endpoints especificados.

## 8. Requisitos no funcionales

Políticas de Consumo y Rate Limiting (Refresh API): El sistema no debe usarse en ningún caso como un "marcador en tiempo real" (Live Heatboard), pues SurfScores lo prohíbe explícitamente y puede bloquear las direcciones IP de la plataforma si se detecta polling excesivo. Se debe implementar un sistema de caché de al menos unos minutos en .NET.

Aspectos Legales UI: Por estricto mandato de la API externa, cualquier pantalla de la PWA que muestre puntajes deberá tener visible la leyenda "Results by SurfScores.com" enlazada a su portal.

Rendimiento y SEO: Todas las vistas públicas deben ser pre-renderizadas con SSR para permitir la indexación por parte de los motores de búsqueda.

Definition of Done (DoD): Una tarea o User Story estará terminada cuando: 
1) El código pase las validaciones en .NET 9 y Angular. 
2) La vista se adapte visualmente a dispositivos móviles (PWA). 
3) Se verifique que el contenido cuenta con SEO dinámico (SSR). 
4) Los tokens y secretos de API nunca se expongan en el entorno de Angular.

## 9. Reglas de Negocio

Capacidad de eventos: Cada evento y categoría tendrá un número máximo de inscritos configurable. El sistema debe bloquear nuevas inscripciones al alcanzar el cupo y notificar al competidor que el evento está lleno. El administrador puede ajustar el cupo desde el CMS.

Token de pago en playa: Un solo token generado y aprobado es válido para todas las categorías que el competidor haya seleccionado en el mismo proceso de inscripción. No se requieren tokens adicionales por categoría adicional dentro de la misma sesión de inscripción. Duración del token: 24 horas desde su emisión.

Categoría sucesiva: Cada categoría con restricción de edad debe tener configurada una "categoría sucesiva" a la que el competidor pasa automáticamente al superar el límite de edad. Esta relación se configura desde el módulo de Categorías del CMS. Ejemplo: Sub-14 → Sub-16 → Sub-18 → Open.

Dominio de correos institucionales: Todas las comunicaciones del sistema (notificaciones, tokens, confirmaciones) deben enviarse desde direcciones con el dominio @alasglobaltour.com.

Reporte de inscritos: El reporte de inscritos por evento debe incluir la posición del ranking del año anterior (temporada pasada) y la posición del ranking del año en curso, por cada competidor y categoría.