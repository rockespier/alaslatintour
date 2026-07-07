import { Component, input, computed } from '@angular/core';

const STATUS_COLORS: Record<string, string> = {
  'Activo': 'bg-green-500/20 text-green-400 border-green-500/30',
  'Inscripciones Abiertas': 'bg-green-500/20 text-green-400 border-green-500/30',
  'Próximamente': 'bg-blue-500/20 text-blue-400 border-blue-500/30',
  'Borrador': 'bg-yellow-500/20 text-yellow-400 border-yellow-500/30',
  'Completado': 'bg-gray-500/20 text-gray-400 border-gray-500/30',
  'Cancelado': 'bg-red-500/20 text-red-400 border-red-500/30',
  'Archivado': 'bg-gray-500/20 text-gray-400 border-gray-500/30',
  'Cerrado': 'bg-gray-500/20 text-gray-400 border-gray-500/30',
  'Pagado': 'bg-green-500/20 text-green-400 border-green-500/30',
  'Pendiente': 'bg-yellow-500/20 text-yellow-400 border-yellow-500/30',
  'Rechazado': 'bg-red-500/20 text-red-400 border-red-500/30',
};

@Component({
  selector: 'app-status-badge',
  standalone: true,
  template: `
    <span class="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium border {{ cssClass() }}">
      {{ status() }}
    </span>
  `,
})
export class StatusBadgeComponent {
  status = input.required<string>();
  cssClass = computed(() =>
    STATUS_COLORS[this.status()] ?? 'bg-gray-500/20 text-gray-400 border-gray-500/30'
  );
}
