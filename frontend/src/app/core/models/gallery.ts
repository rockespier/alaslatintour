export interface GalleryCard {
  id: string;
  slug: string;
  title: string;
  eventDate: string | null;
  coverImageUrl: string;
  photoCount: number;
}

export interface GalleryAsset {
  id: string;
  type: 'photo' | 'video';
  url: string;
  width: number;
  height: number;
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
  pressDownloadLink: string | null;
  coverImageUrl: string;
  photoCount: number;
  galleryDays: GalleryDay[];
}
