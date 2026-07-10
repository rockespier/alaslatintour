import { Injectable, inject } from '@angular/core';
import { ApiService } from './api.service';

export interface RankingCategory {
  id: string;
  nombre: string;
  availableYears: number[];
}

export interface RankingRow {
  position: number;
  name: string;
  country: string;
  flag: string;
  points: number;
  events: number;
  change: number;
}

export interface RankingResult {
  categoryId: string;
  categoryName: string;
  year: number;
  cachedAt: Date | null;
  attribution: string;
  rows: RankingRow[];
  totalPages: number;
  currentPage: number;
  totalItems: number;
}

const FLAGS: Record<string, string> = {
  PE: '🇵🇪', BR: '🇧🇷', CL: '🇨🇱', AR: '🇦🇷', MX: '🇲🇽',
  CR: '🇨🇷', CO: '🇨🇴', EC: '🇪🇨', UY: '🇺🇾', PA: '🇵🇦',
  VE: '🇻🇪', BO: '🇧🇴', US: '🇺🇸', ES: '🇪🇸', PT: '🇵🇹',
};

const NAMES: Record<string, string> = {
  PE: 'Perú', BR: 'Brasil', CL: 'Chile', AR: 'Argentina', MX: 'México',
  CR: 'Costa Rica', CO: 'Colombia', EC: 'Ecuador', UY: 'Uruguay', PA: 'Panamá',
  VE: 'Venezuela', BO: 'Bolivia', US: 'EE.UU.', ES: 'España', PT: 'Portugal',
};

@Injectable({ providedIn: 'root' })
export class RankingService {
  private api = inject(ApiService);

  async getCategories(): Promise<RankingCategory[]> {
    const res = await this.api.get<any>('/rankings/categories');
    return (res?.data ?? []).map((d: any) => ({
      id: d.id ?? '',
      nombre: d.nombre ?? '',
      availableYears: Array.isArray(d.availableYears) ? d.availableYears : [],
    }));
  }

  async getRanking(
    categoryId: string,
    year?: number,
    page = 1,
    limit = 10,
  ): Promise<RankingResult> {
    let path = `/rankings?categoryId=${encodeURIComponent(categoryId)}&page=${page}&limit=${limit}`;
    if (year) path += `&year=${year}`;

    const res = await this.api.get<any>(path);

    return {
      categoryId: res?.categoryId ?? categoryId,
      categoryName: res?.categoryName ?? '',
      year: res?.year ?? new Date().getFullYear(),
      cachedAt: res?.cachedAt ? new Date(res.cachedAt) : null,
      attribution: res?.attribution ?? 'Results by SurfScores.com',
      rows: (res?.data ?? []).map((e: any, i: number): RankingRow => {
        const code = (e.country ?? '').toUpperCase();
        return {
          position: e.pos ?? i + 1,
          name: e.name ?? '',
          country: NAMES[code] ?? code,
          flag: FLAGS[code] ?? '🏄',
          points: e.points ?? 0,
          events: e.events ?? 0,
          change: e.variation ?? 0,
        };
      }),
      totalPages: res?.pagination?.totalPages ?? 1,
      currentPage: res?.pagination?.currentPage ?? 1,
      totalItems: res?.pagination?.totalItems ?? 0,
    };
  }

  flagOf(code: string): string {
    return FLAGS[(code ?? '').toUpperCase()] ?? '🏄';
  }

  cachedAgo(date: Date | null): string {
    if (!date) return '';
    const mins = Math.floor((Date.now() - date.getTime()) / 60000);
    if (mins < 1) return 'hace un momento';
    if (mins < 60) return `hace ${mins} min`;
    const hrs = Math.floor(mins / 60);
    return `hace ${hrs} h`;
  }
}
