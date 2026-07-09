// Backend already serializes /v1/galleries with camelCase field names
// matching this shape 1:1 — no mapping layer needed (see gallery.md).
export interface GalleryPhoto {
  id: string;
  url: string;
  width: number;
  height: number;
}

export interface Gallery {
  id: string;
  slug: string;
  title: string;
  eventDate: string | null;
  pressDownloadLink: string | null;
  photos: GalleryPhoto[];
}
