import { Injectable, inject, signal, computed } from '@angular/core';
import { ApiService } from './api.service';

export interface LiveEvent {
  id: string;
  nombre: string;
  pais: string;
  ciudad: string;
  playa: string;
  fechaInicio: string;
  fechaFin: string;
  imagenUrl?: string | null;
}

export interface LiveStatus {
  isLive: boolean;
  event?: LiveEvent | null;
  youTubeVideoId?: string | null;
  youTubeWidth: number;
  youTubeHeight: number;
  schedulePdfUrl?: string | null;
  surfScoresEmbedUrl?: string | null;
  surfScoresWidth: number;
  surfScoresHeight: number;
}

/** Estado del evento en vivo, compartido entre el navbar y el home. */
@Injectable({ providedIn: 'root' })
export class LiveStatusService {
  private api = inject(ApiService);

  readonly status = signal<LiveStatus | null>(null);
  readonly isLive = computed(() => this.status()?.isLive ?? false);

  private loadPromise: Promise<void> | null = null;

  /** Dispara el fetch una única vez por sesión (se comparte entre componentes). */
  ensureLoaded(): void {
    if (this.loadPromise) return;
    this.loadPromise = this.refresh();
  }

  async refresh(): Promise<void> {
    try {
      const status = await this.api.get<LiveStatus>('/live');
      this.status.set(status ?? null);
    } catch {
      this.status.set(null);
    }
  }
}
