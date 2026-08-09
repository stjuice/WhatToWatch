export interface MovieDto {
  id: string;
  title: string;
  year?: number;
  posterUrl?: string;
  rating?: number;
  genres: string[];
}

export interface WatchlistDto {
  id: string;
  name: string;
  lastRefreshedAt?: string;
  movies: MovieDto[];
}

export interface MovieFilterRequest {
  query?: string;
  yearFrom?: number;
  yearTo?: number;
  minRating?: number;
  genres?: string[];
}
