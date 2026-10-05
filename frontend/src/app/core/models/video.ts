/** Latest videos of the official YouTube channel (`GET /v1/videos`). */
export interface Video {
  id: string;
  title: string;
  publishedAt: string;
  thumbnailUrl: string;
  url: string;
}
