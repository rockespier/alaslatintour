import { Component } from '@angular/core';

@Component({
  selector: 'app-surfscores-credit',
  standalone: true,
  template: `
    <p class="text-xs text-[#AAAAAA] text-center mt-2">
      Results by <a href="https://surfscores.com" target="_blank" rel="noopener"
        class="text-[#0081C6] hover:underline">SurfScores.com</a>
    </p>
  `,
})
export class SurfscoresCreditComponent {}
