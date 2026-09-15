import { Component, input } from '@angular/core';

@Component({
  selector: 'app-loading-spinner',
  standalone: true,
  template: `
    <div class="flex items-center justify-center gap-3 py-12">
      <div class="w-8 h-8 border-3 border-navy-mid/40 border-t-cyan-brand rounded-full animate-spin"></div>
      @if (label()) {
        <span class="text-sm text-text-muted">{{ label() }}</span>
      }
    </div>
  `,
})
export class LoadingSpinnerComponent {
  label = input<string>('');
}
