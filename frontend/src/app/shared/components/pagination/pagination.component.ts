import { Component, input, output, computed } from '@angular/core';

@Component({
  selector: 'app-pagination',
  standalone: true,
  template: `
    @if (totalPages() > 1) {
      <div class="flex items-center justify-center gap-2 mt-6">
        <button (click)="go(currentPage() - 1)" [disabled]="currentPage() === 1"
          class="px-3 py-1.5 rounded text-sm bg-navy-mid/10 hover:bg-navy-mid/20 disabled:opacity-30 disabled:cursor-not-allowed text-text-light border border-navy-mid/40">
          ‹
        </button>
        @for (page of pages(); track page) {
          <button (click)="go(page)"
            [class]="page === currentPage()
              ? 'px-3 py-1.5 rounded text-sm bg-cyan-brand text-white font-medium'
              : 'px-3 py-1.5 rounded text-sm bg-navy-mid/10 hover:bg-navy-mid/20 text-text-light border border-navy-mid/40'">
            {{ page }}
          </button>
        }
        <button (click)="go(currentPage() + 1)" [disabled]="currentPage() === totalPages()"
          class="px-3 py-1.5 rounded text-sm bg-navy-mid/10 hover:bg-navy-mid/20 disabled:opacity-30 disabled:cursor-not-allowed text-text-light border border-navy-mid/40">
          ›
        </button>
        <span class="text-xs text-text-muted ml-2">{{ totalItems() }} resultados</span>
      </div>
    }
  `,
})
export class PaginationComponent {
  currentPage = input.required<number>();
  totalPages = input.required<number>();
  totalItems = input<number>(0);
  pageChange = output<number>();

  pages = computed(() => {
    const total = this.totalPages();
    const current = this.currentPage();
    const delta = 2;
    const range: number[] = [];
    for (let i = Math.max(1, current - delta); i <= Math.min(total, current + delta); i++) {
      range.push(i);
    }
    return range;
  });

  go(page: number): void {
    if (page < 1 || page > this.totalPages()) return;
    this.pageChange.emit(page);
  }
}
