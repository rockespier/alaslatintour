import { Component, inject, input, output, signal, effect } from '@angular/core';
import { ApiService } from '../../../core/services/api.service';

export interface ImportErrorItem {
  rowNumber?: number;
  message: string;
}

export interface ImportResult {
  processedRows: number;
  createdCount: number;
  updatedCount: number;
  errors: ImportErrorItem[];
}

@Component({
  selector: 'app-import-excel-modal',
  standalone: true,
  template: `
    @if (open()) {
      <div class="fixed inset-0 z-50 flex items-center justify-center p-4"
           style="background:rgba(0,35,89,0.8)" (click)="close.emit()">
        <div class="bg-navy-dark border border-navy-mid rounded-xl w-full max-w-md max-h-[90vh] overflow-y-auto"
             (click)="$event.stopPropagation()">
          <div class="flex items-center justify-between p-6 border-b border-navy-mid">
            <h2 class="font-heading text-xl text-white">Importar {{ entityLabel() }} desde Excel</h2>
            <button (click)="close.emit()" class="text-text-muted hover:text-white transition">
              <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>
              </svg>
            </button>
          </div>

          <div class="p-6 space-y-4">
            @if (!result()) {
              <p class="text-sm text-text-muted">
                Selecciona un archivo <code class="text-cyan-brand">.xlsx</code> basado en la plantilla descargada.
                Si una fila trae <code>Id</code> o coincide por código de SurfScores, se actualiza; si no, se crea.
              </p>
              <input type="file" accept=".xlsx" (change)="onFileSelected($event)" [disabled]="uploading()"
                     class="block w-full text-sm text-text-light file:mr-3 file:py-2 file:px-3 file:rounded-md file:border-0 file:bg-cyan-brand file:text-navy-deepest file:font-accent file:uppercase file:text-xs file:tracking-wider">
              @if (uploading()) {
                <p class="text-xs text-text-muted">Subiendo e importando...</p>
              }
              @if (error()) {
                <p class="text-error-brand text-xs">{{ error() }}</p>
              }
            } @else {
              <div class="space-y-2 text-sm">
                <p class="text-text-light">Filas procesadas: <strong>{{ result()!.processedRows }}</strong></p>
                <p class="text-success-brand">Creadas: {{ result()!.createdCount }}</p>
                <p class="text-cyan-brand">Actualizadas: {{ result()!.updatedCount }}</p>
                @if (result()!.errors.length > 0) {
                  <div class="text-warning-brand text-xs mt-2">
                    <p class="font-accent uppercase tracking-wider mb-1">Errores ({{ result()!.errors.length }})</p>
                    <ul class="list-disc list-inside space-y-0.5">
                      @for (e of result()!.errors; track trackError($index, e)) {
                        <li>
                          @if (e.rowNumber) {
                            <span>Fila {{ e.rowNumber }}: </span>
                          }
                          <span>{{ e.message }}</span>
                        </li>
                      }
                    </ul>
                  </div>
                }
              </div>
            }
          </div>

          <div class="px-6 py-4 border-t border-navy-mid flex justify-end gap-3">
            <button (click)="close.emit()"
                    class="px-4 py-2 rounded-md border border-navy-mid text-text-muted hover:border-cyan-brand hover:text-text-light font-accent uppercase text-xs tracking-wider transition">
              {{ result() ? 'Cerrar' : 'Cancelar' }}
            </button>
          </div>
        </div>
      </div>
    }
  `,
})
export class ImportExcelModalComponent {
  private api = inject(ApiService);

  open = input.required<boolean>();
  importPath = input.required<string>();
  entityLabel = input<string>('registros');

  close = output<void>();
  imported = output<ImportResult>();

  uploading = signal(false);
  error = signal<string | null>(null);
  result = signal<ImportResult | null>(null);

  constructor() {
    effect(() => {
      if (this.open()) {
        this.error.set(null);
        this.result.set(null);
      }
    });
  }

  async onFileSelected(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    this.error.set(null);
    this.uploading.set(true);
    try {
      const formData = new FormData();
      formData.append('file', file);
      const res = await this.api.upload<any>(this.importPath(), formData);
      const normalized: ImportResult = {
        processedRows: Number(res?.processedRows ?? 0),
        createdCount: Number(res?.createdCount ?? 0),
        updatedCount: Number(res?.updatedCount ?? 0),
        errors: Array.isArray(res?.errors)
          ? res.errors.map((item: any) => typeof item === 'string'
              ? { message: item }
              : {
                  rowNumber: typeof item?.rowNumber === 'number' ? item.rowNumber : undefined,
                  message: String(item?.message ?? item),
                })
          : [],
      };
      this.result.set(normalized);
      this.imported.emit(normalized);
    } catch (err: any) {
      this.error.set(err?.body?.message ?? err?.message ?? 'No se pudo importar el archivo.');
    } finally {
      this.uploading.set(false);
      input.value = '';
    }
  }

  trackError(index: number, error: ImportErrorItem): string {
    return `${error.rowNumber ?? index}-${error.message}`;
  }
}
