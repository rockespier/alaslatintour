# Informe de Avance del Proyecto — Plataforma ALAS Latin Tour

**Fecha:** 15 de julio de 2026

---

## 1. Resumen general

El desarrollo de la nueva plataforma web (sitio público + panel del competidor + panel de administración) avanza según lo planeado. La mayoría de las pantallas ya están **conectadas y funcionando con datos reales**, no con datos de ejemplo (maqueta).

En términos simples: el sitio ya no es solo un diseño visual, es una aplicación funcional que guarda y consulta información real de la base de datos.

---

## 2. ¿Qué ya está funcionando con datos reales?

### Sitio público (visitantes, sin necesidad de iniciar sesión)
- Página de inicio, con eventos, noticias y ranking en vivo.
- Sección de Noticias y Galerías de fotos (traídas automáticamente desde WordPress).
- Ranking de surfistas en vivo. (Aun no tenemos datos)
- Calendario público de eventos.
- Página "Quiénes somos" (contenido fijo, no necesita conexión a base de datos).

### Registro e inicio de sesión
- Login, registro de nuevos usuarios y recuperación de contraseña: **funcionando completamente**.

### Panel del competidor (usuario logueado)
- Ver eventos disponibles e inscribirse.
- Proceso de inscripción a competencias.
- Pago en playa (solicitud de código/token).
- Mis inscripciones, historial de puntos, mi calendario y mis datos personales.

Todo este flujo, de principio a fin, ya está operativo.

### Panel de administración
- Dashboard general con indicadores (KPIs), eventos activos e inscripciones recientes.
- Gestión de usuarios y roles/permisos.
- Gestión de circuitos, eventos y categorías.
- Listado de inscritos, resultados por evento (enlazados a SurfScores, tal como exige la reglamentación) y cálculo de premios en efectivo.
- Gestión de pagos, membresías y tokens de pago en playa (aprobar/rechazar).
- Configuración general del sistema (ranking, integraciones, notificaciones, modo en vivo).

**En resumen: prácticamente todas las pantallas planeadas están conectadas y operativas.**

---

## 3. Estado técnico del servidor (backend)

El servidor (backend) ya cuenta con todo lo necesario para instalarse en un ambiente nuevo o de producción:

- Proceso de instalación y configuración documentado y probado.
- Creación automática del primer usuario "Super Administrador" al iniciar el sistema.
- Conexión a la base de datos, seguridad (login) y conexión con WordPress, ya configurables por ambiente (pruebas, producción, etc.).
- Pasos de verificación después de cada instalación para asegurar que todo responda correctamente (login, panel admin, noticias, galerías, ranking).

Esto significa que desplegar la plataforma en un servidor real (por ejemplo, para pruebas del cliente o para producción) ya es un proceso ordenado y repetible, no manual ni improvisado.

---

## 4. Puntos pendientes o a revisar antes de producción

Para ser transparentes, estos son los temas identificados que **todavía requieren atención**:

1. **Seguridad de algunos endpoints administrativos**: Se detectó que algunas funciones internas (pagos, membresías, tokens de pago en playa, inscripciones) hoy pueden ser alcanzadas sin haber iniciado sesión como administrador. Las pantallas de Usuarios, Roles, Dashboard y Configuración sí están correctamente protegidas. **Se recomienda corregir esto antes de salir a producción.**
2. **Matriz de tarifas por categoría**: el backend ya permite configurar tarifas según la categoría y las "estrellas" del evento, pero todavía falta construir la pantalla visual para administrar esa matriz cómodamente.
3. **Gráfica de recaudación mensual**: el diseño original contemplaba un gráfico con la recaudación de los últimos 6 meses en la sección de Pagos. Se decidió no mostrarlo porque no existe todavía una fuente de datos histórica real detrás; se prefirió no inventar números en lugar de mostrar información falsa.
4. **Edición limitada de usuarios**: actualmente, al editar un usuario ya creado, solo se puede cambiar su rol y su estado (activo/inactivo), no su nombre, apellido o correo. Esto es una decisión de diseño actual, no un error; se puede ampliar si el cliente lo requiere.

---

## 5. Próximos pasos sugeridos

- Cerrar el punto de seguridad de los endpoints administrativos (prioridad alta).
- Construir la pantalla de matriz de tarifas por categoría.
- Definir con el cliente si se necesita un histórico real de recaudación (implicaría guardar esa información a futuro para poder graficarla).
- Preparar el ambiente de producción utilizando el proceso de instalación ya documentado.

---

*Este documento resume el estado técnico en lenguaje simple para seguimiento del proyecto. Para el detalle técnico completo, consultar `deploy-backend.md` y `estado-pantallas-frontend.md` en el repositorio.*
