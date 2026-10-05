export interface GalleryCard {
  id: string;
  slug: string;
  title: string;
  eventDate: string | null;
  /** Publication date set in WordPress. */
  publishedAt: string | null;
  coverImageUrl: string;
  photoCount: number;
}

export interface GalleryAsset {
  id: string;
  type: 'photo' | 'video';
  url: string;
  width: number;
  height: number;
  caption: string | null;
}

export interface GalleryDay {
  dayName: string;
  assets: GalleryAsset[];
}

export interface GalleryDetail {
  id: string;
  slug: string;
  title: string;
  eventDate: string | null;
  /** Publication date set in WordPress. */
  publishedAt: string | null;
  pressDownloadLink: string | null;
  coverImageUrl: string;
  photoCount: number;
  galleryDays: GalleryDay[];
  /** Slug of this gallery per language (Polylang), e.g. `{ es: 'dia-4', en: 'day-4' }`. */
  translations?: Record<string, string>;
}
