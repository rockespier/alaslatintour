import { Component, input } from '@angular/core';

@Component({
  selector: 'app-loading-spinner',
  standalone: true,
  template: `
    <div class="flex items-center justify-center gap-3 py-12">
      <div class="w-8 h-8 border-3 border-white/20 border-t-[#0081C6] rounded-full animate-spin"></div>
      @if (label()) {
        <span class="text-sm text-[#AAAAAA]">{{ label() }}</span>
      }
    </div>
  `,
})
export class LoadingSpinnerComponent {
  label = input<string>('');
}
