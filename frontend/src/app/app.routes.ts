import { Routes } from '@angular/router';
import { adminGuard } from './core/guards/admin.guard';
import { moduleGuard } from './core/guards/module.guard';
import { competitorGuard } from './core/guards/competitor.guard';
import { langGuard, langPrefixMatcher } from './core/i18n/lang.guard';
// mi-panel route moved inside public-layout to avoid double navbar

// Public site + competitor panel + auth. Mounted twice: at the root (Spanish)
// and behind the `/en` | `/pt` prefix. Route titles are Transloco keys.
const appChildren: Routes = [
  // ── Portal Público ───────────────────────────────────────────
  {
    path: '',
    loadComponent: () => import('./layouts/public-layout/public-layout.component').then(m => m.PublicLayoutComponent),
    children: [
      {
        path: '',
        loadComponent: () => import('./features/public/home/home.component').then(m => m.HomeComponent),
        title: 'titles.home',
      },
      {
        path: 'quienes-somos',
        loadComponent: () => import('./features/public/quienes-somos/quienes-somos.component').then(m => m.QuienesSomosComponent),
        title: 'titles.about',
      },
      {
        path: 'noticias',
        loadComponent: () => import('./features/public/noticias/noticias.component').then(m => m.NoticiasComponent),
        title: 'titles.news',
      },
      {
        path: 'noticias/:slug',
        loadComponent: () => import('./features/public/noticia-detalle/noticia-detalle.component').then(m => m.NoticiaDetalleComponent),
      },
      {
        path: 'galerias/:slug',
        loadComponent: () => import('./features/public/galeria-detalle/galeria-detalle.component').then(m => m.GaleriaDetalleComponent),
        title: 'titles.gallery',
      },
      {
        path: 'ranking',
        loadComponent: () => import('./features/public/ranking/ranking.component').then(m => m.RankingComponent),
        title: 'titles.ranking',
      },
      {
        path: 'eventos',
        loadComponent: () => import('./features/competitor/eventos/eventos.component').then(m => m.EventosComponent),
        title: 'titles.events',
      },
      {
        path: 'calendario',
        redirectTo: 'eventos',
        pathMatch: 'full',
      },
      {
        path: 'inscripcion/:eventId',
        canActivate: [competitorGuard],
        loadComponent: () => import('./features/competitor/inscripcion/inscripcion.component').then(m => m.InscripcionComponent),
        title: 'titles.inscription',
      },
      {
        path: 'pago-playa/:inscriptionId',
        canActivate: [competitorGuard],
        loadComponent: () => import('./features/competitor/pago-playa/pago-playa.component').then(m => m.PagoPlayaComponent),
        title: 'titles.beachPayment',
      },
      {
        path: 'paypal/retorno',
        canActivate: [competitorGuard],
        loadComponent: () => import('./features/competitor/paypal-return/paypal-return.component').then(m => m.PaypalReturnComponent),
        title: 'titles.paypalReturn',
      },
      {
        path: 'paypal/cancelado',
        canActivate: [competitorGuard],
        loadComponent: () => import('./features/competitor/paypal-return/paypal-return.component').then(m => m.PaypalReturnComponent),
        title: 'titles.paypalCancelled',
      },
      // ── Panel Competidor (nested inside public-layout for shared navbar) ──
      {
        path: 'mi-panel',
        canActivate: [competitorGuard],
        loadComponent: () => import('./layouts/mi-panel-layout/mi-panel-layout.component').then(m => m.MiPanelLayoutComponent),
        children: [
          { path: '', redirectTo: 'inscripciones', pathMatch: 'full' },
          {
            path: 'inscripciones',
            loadComponent: () => import('./features/competitor/mi-panel/mis-inscripciones/mis-inscripciones.component').then(m => m.MisInscripcionesComponent),
            title: 'titles.myInscriptions',
          },
          {
            path: 'historial',
            loadComponent: () => import('./features/competitor/mi-panel/historial-puntos/historial-puntos.component').then(m => m.HistorialPuntosComponent),
            title: 'titles.pointsHistory',
          },
          {
            path: 'calendario',
            loadComponent: () => import('./features/competitor/mi-panel/mi-calendario/mi-calendario.component').then(m => m.MiCalendarioComponent),
            title: 'titles.myCalendar',
          },
          {
            path: 'datos',
            loadComponent: () => import('./features/competitor/mi-panel/datos-personales/datos-personales.component').then(m => m.DatosPersonalesComponent),
            title: 'titles.personalData',
          },
        ],
      },
    ],
  },

  // ── Auth ───────────────────────────────────────────────────
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login/login.component').then(m => m.LoginComponent),
    title: 'titles.login',
  },
  {
    path: 'registro',
    loadComponent: () => import('./features/auth/registro/registro.component').then(m => m.RegistroComponent),
    title: 'titles.register',
  },
  {
    path: 'recuperar-password',
    loadComponent: () => import('./features/auth/recuperar-password/recuperar-password.component').then(m => m.RecuperarPasswordComponent),
    title: 'titles.forgotPassword',
  },
  {
    path: 'restablecer-password',
    loadComponent: () => import('./features/auth/restablecer-password/restablecer-password.component').then(m => m.RestablecerPasswordComponent),
    title: 'titles.resetPassword',
  },
];

export const routes: Routes = [
  // ── Admin (solo español, sin prefijo de idioma) ──────────
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
        canActivate: [moduleGuard],
        data: { module: 'Usuarios' },
        loadComponent: () => import('./features/admin/usuarios/usuarios.component').then(m => m.UsuariosComponent),
        title: 'Usuarios — ALAS Admin',
      },
      {
        path: 'competidores',
        canActivate: [moduleGuard],
        data: { module: 'Usuarios' },
        loadComponent: () => import('./features/admin/competidores/competidores.component').then(m => m.CompetidoresComponent),
        title: 'Competidores — ALAS Admin',
      },
      {
        path: 'circuitos',
        canActivate: [moduleGuard],
        data: { module: 'Circuitos' },
        loadComponent: () => import('./features/admin/circuitos/circuitos.component').then(m => m.CircuitosComponent),
        title: 'Circuitos — ALAS Admin',
      },
      {
        path: 'eventos',
        canActivate: [moduleGuard],
        data: { module: 'Eventos' },
        loadComponent: () => import('./features/admin/eventos/admin-eventos.component').then(m => m.AdminEventosComponent),
        title: 'Eventos — ALAS Admin',
      },
      {
        path: 'categorias',
        canActivate: [moduleGuard],
        data: { module: 'Categorias' },
        loadComponent: () => import('./features/admin/categorias/categorias.component').then(m => m.CategoriasComponent),
        title: 'Categorías — ALAS Admin',
      },
      {
        path: 'inscritos',
        canActivate: [moduleGuard],
        data: { module: 'Inscripciones' },
        loadComponent: () => import('./features/admin/inscritos/inscritos.component').then(m => m.InscritosComponent),
        title: 'Inscritos — ALAS Admin',
      },
      {
        path: 'pagos',
        canActivate: [moduleGuard],
        data: { module: 'Pagos' },
        loadComponent: () => import('./features/admin/pagos/pagos.component').then(m => m.PagosComponent),
        title: 'Pagos — ALAS Admin',
      },
      {
        path: 'tokens',
        canActivate: [moduleGuard],
        data: { module: 'Tokens' },
        loadComponent: () => import('./features/admin/tokens/admin-tokens.component').then(m => m.AdminTokensComponent),
        title: 'Tokens — ALAS Admin',
      },
      {
        path: 'configuracion',
        canActivate: [moduleGuard],
        data: { module: 'Configuracion' },
        loadComponent: () => import('./features/admin/configuracion/configuracion.component').then(m => m.ConfiguracionComponent),
        title: 'Configuración — ALAS Admin',
      },
      {
        path: 'perfil',
        loadComponent: () => import('./features/admin/perfil/perfil.component').then(m => m.AdminPerfilComponent),
        title: 'Mi Perfil — ALAS Admin',
      },
    ],
  },

  // ── Sitio localizado ──────────────────────────────────────
  { matcher: langPrefixMatcher, canActivate: [langGuard], children: appChildren },
  { path: '', canActivate: [langGuard], children: appChildren },

  // ── Fallback ───────────────────────────────────────────────
  { path: '**', redirectTo: '' },
];
