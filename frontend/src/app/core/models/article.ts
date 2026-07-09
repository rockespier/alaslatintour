/**
 * The API serializes articles with Spanish field names (titulo, resumen, categoria,
 * imagenUrl, fechaPublicacion, tiempoLecturaMin — see ArticlesController /
 * ApiContractMapper.ToContract in the backend). These mappers translate the wire
 * shape into the camelCase English shape the article components render.
 */
export interface ArticleSummary {
  id: string;
  slug: string;
  title: string;
  excerpt: string;
  category: string;
  imageUrl?: string;
  publishedAt: string;
  featured?: boolean;
  readingTime?: number;
}

export interface ArticleDetail extends ArticleSummary {
  content: string;
  author?: { name: string; role: string };
  tags?: string[];
}

export function mapArticleSummary(raw: any): ArticleSummary {
  return {
    id: raw.id,
    slug: raw.slug,
    title: raw.titulo,
    excerpt: raw.resumen,
    category: raw.categoria,
    imageUrl: raw.imagenUrl,
    publishedAt: raw.fechaPublicacion,
    featured: raw.featured,
    readingTime: raw.tiempoLecturaMin,
  };
}

export function mapArticleDetail(raw: any): ArticleDetail {
  return {
    ...mapArticleSummary(raw),
    content: raw.content,
    author: raw.author,
    tags: raw.tags,
  };
}
