export interface MovieDto {
  id: string;
  title: string;
  year?: number;
  posterUrl?: string;
  rating?: number;
  plot?: string;
  runtimeMinutes?: number;
  director?: string;
  genres: string[];
}

export interface WatchlistDto {
  id: string;
  name: string;
  url?: string;
  lastRefreshedAt?: string;
  movies: MovieDto[];
}

export interface MovieFilterRequest {
  watchlistId?: string;
  query?: string;
  yearFrom?: number;
  yearTo?: number;
  minRating?: number;
  genres?: string[];
}

export interface CreateWatchlistRequest {
  url: string;
}

export interface UpdateWatchlistRequest {
  name: string;
}

export interface ImportImdbMovieRequest {
  imdbId: string;
  title: string;
  year: number | null;
  imageUrl: string | null;
  rating?: number | null;
  plot?: string | null;
  runtimeMinutes?: number | null;
  director?: string | null;
  genres?: string[];
  titleType?: string | null;
}

export interface ImportImdbWatchlistRequest {
  listId: string;
  title: string;
  url?: string;
  movies: ImportImdbMovieRequest[];
}
