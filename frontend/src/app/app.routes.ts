import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { adminGuard } from './core/guards/admin.guard';
// mi-panel route moved inside public-layout to avoid double navbar

export const routes: Routes = [
  // ── Portal Público ───────────────────────────────────────────
  {
    path: '',
    loadComponent: () => import('./layouts/public-layout/public-layout.component').then(m => m.PublicLayoutComponent),
    children: [
      {
        path: '',
        loadComponent: () => import('./features/public/home/home.component').then(m => m.HomeComponent),
        title: 'ALAS Latin Tour — Surf Profesional Latinoamericano',
      },
      {
        path: 'quienes-somos',
        loadComponent: () => import('./features/public/quienes-somos/quienes-somos.component').then(m => m.QuienesSomosComponent),
        title: 'Quiénes Somos — ALAS Latin Tour',
      },
      {
        path: 'noticias',
        loadComponent: () => import('./features/public/noticias/noticias.component').then(m => m.NoticiasComponent),
        title: 'Noticias — ALAS Latin Tour',
      },
      {
        path: 'noticias/:slug',
        loadComponent: () => import('./features/public/noticia-detalle/noticia-detalle.component').then(m => m.NoticiaDetalleComponent),
      },
      {
        path: 'galerias/:slug',
        loadComponent: () => import('./features/public/galeria-detalle/galeria-detalle.component').then(m => m.GaleriaDetalleComponent),
        title: 'Galería — ALAS Latin Tour',
      },
      {
        path: 'ranking',
        loadComponent: () => import('./features/public/ranking/ranking.component').then(m => m.RankingComponent),
        title: 'Ranking — ALAS Latin Tour',
      },
      {
        path: 'eventos',
        loadComponent: () => import('./features/competitor/eventos/eventos.component').then(m => m.EventosComponent),
        title: 'Eventos — ALAS Latin Tour',
      },
      {
        path: 'calendario',
        loadComponent: () => import('./features/public/calendario/calendario.component').then(m => m.CalendarioComponent),
        title: 'Calendario de Eventos — ALAS Latin Tour',
      },
      {
        path: 'inscripcion/:eventId',
        canActivate: [authGuard],
        loadComponent: () => import('./features/competitor/inscripcion/inscripcion.component').then(m => m.InscripcionComponent),
        title: 'Inscripción — ALAS Latin Tour',
      },
      {
        path: 'pago-playa/:inscriptionId',
        canActivate: [authGuard],
        loadComponent: () => import('./features/competitor/pago-playa/pago-playa.component').then(m => m.PagoPlayaComponent),
        title: 'Pago en Playa — ALAS Latin Tour',
      },
      // ── Panel Competidor (nested inside public-layout for shared navbar) ──
      {
        path: 'mi-panel',
        canActivate: [authGuard],
        loadComponent: () => import('./layouts/mi-panel-layout/mi-panel-layout.component').then(m => m.MiPanelLayoutComponent),
        children: [
          { path: '', redirectTo: 'inscripciones', pathMatch: 'full' },
          {
            path: 'inscripciones',
            loadComponent: () => import('./features/competitor/mi-panel/mis-inscripciones/mis-inscripciones.component').then(m => m.MisInscripcionesComponent),
            title: 'Mis Inscripciones — ALAS Latin Tour',
          },
          {
            path: 'historial',
            loadComponent: () => import('./features/competitor/mi-panel/historial-puntos/historial-puntos.component').then(m => m.HistorialPuntosComponent),
            title: 'Historial de Puntos — ALAS Latin Tour',
          },
          {
            path: 'calendario',
            loadComponent: () => import('./features/competitor/mi-panel/mi-calendario/mi-calendario.component').then(m => m.MiCalendarioComponent),
            title: 'Mi Calendario — ALAS Latin Tour',
          },
          {
            path: 'datos',
            loadComponent: () => import('./features/competitor/mi-panel/datos-personales/datos-personales.component').then(m => m.DatosPersonalesComponent),
            title: 'Datos Personales — ALAS Latin Tour',
          },
        ],
      },
    ],
  },

  // ── Auth ───────────────────────────────────────────────────
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login/login.component').then(m => m.LoginComponent),
    title: 'Iniciar Sesión — ALAS Latin Tour',
  },
  {
    path: 'registro',
    loadComponent: () => import('./features/auth/registro/registro.component').then(m => m.RegistroComponent),
    title: 'Registro — ALAS Latin Tour',
  },
  {
    path: 'recuperar-password',
    loadComponent: () => import('./features/auth/recuperar-password/recuperar-password.component').then(m => m.RecuperarPasswordComponent),
    title: 'Recuperar Contraseña — ALAS Latin Tour',
  },
  {
    path: 'restablecer-password',
    loadComponent: () => import('./features/auth/restablecer-password/restablecer-password.component').then(m => m.RestablecerPasswordComponent),
    title: 'Restablecer Contraseña — ALAS Latin Tour',
  },

  // ── Admin ──────────────────────────────────────────────────
  {
    path: 'admin',
    canActivate: [adminGuard],
    loadComponent: () => import('./layouts/admin-layout/admin-layout.component').then(m => m.AdminLayoutComponent),
    children: [
      {
        path: '',
        loadComponent: () => import('./features/admin/dashboard/dashboard.component').then(m => m.AdminDashboardComponent),
        title: 'Dashboard — ALAS Admin',
      },
      {
        path: 'usuarios',
        loadComponent: () => import('./features/admin/usuarios/usuarios.component').then(m => m.UsuariosComponent),
        title: 'Usuarios — ALAS Admin',
      },
      {
        path: 'circuitos',
        loadComponent: () => import('./features/admin/circuitos/circuitos.component').then(m => m.CircuitosComponent),
        title: 'Circuitos — ALAS Admin',
      },
      {
        path: 'eventos',
        loadComponent: () => import('./features/admin/eventos/admin-eventos.component').then(m => m.AdminEventosComponent),
        title: 'Eventos — ALAS Admin',
      },
      {
        path: 'categorias',
        loadComponent: () => import('./features/admin/categorias/categorias.component').then(m => m.CategoriasComponent),
        title: 'Categorías — ALAS Admin',
      },
      {
        path: 'inscritos',
        loadComponent: () => import('./features/admin/inscritos/inscritos.component').then(m => m.InscritosComponent),
        title: 'Inscritos — ALAS Admin',
      },
      {
        path: 'pagos',
        loadComponent: () => import('./features/admin/pagos/pagos.component').then(m => m.PagosComponent),
        title: 'Pagos — ALAS Admin',
      },
      {
        path: 'tokens',
        loadComponent: () => import('./features/admin/tokens/admin-tokens.component').then(m => m.AdminTokensComponent),
        title: 'Tokens — ALAS Admin',
      },
      {
        path: 'configuracion',
        loadComponent: () => import('./features/admin/configuracion/configuracion.component').then(m => m.ConfiguracionComponent),
        title: 'Configuración — ALAS Admin',
      },
    ],
  },

  // ── Fallback ───────────────────────────────────────────────
  { path: '**', redirectTo: '' },
];
