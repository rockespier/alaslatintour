export interface UserInfo {
  id: string;
  email: string;
  fullName: string;
  tipo: 'espectador' | 'competidor';
  adminRole?: 'Super Admin' | 'Admin' | 'Árbitro' | 'Revisor';
  competitorId?: string;
}

export interface PaginationMeta {
  currentPage: number;
  itemsPerPage: number;
  totalItems: number;
  totalPages: number;
}

export interface PagedResponse<T> {
  data: T[];
  pagination: PaginationMeta;
}
