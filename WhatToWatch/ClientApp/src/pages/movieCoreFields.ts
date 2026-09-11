import type { Movie } from "../types/movie";

export const GENRE_SEPARATOR = " | ";

export const getImdbTitleUrl = (id: string): string =>
  `https://www.imdb.com/title/${id}/`;

export type MovieCoreFields = {
  title: string | null;
  year: string | null;
  genres: string | null;
  duration: string | null;
  director: string | null;
  description: string | null;
  rating: string | null;
};

export const formatRuntime = (minutes?: number): string | null => {
  if (minutes == null || Number.isNaN(minutes) || minutes < 0) {
    return null;
  }

  const hours = Math.floor(minutes / 60);
  const mins = minutes % 60;
  return `${hours}:${mins.toString().padStart(2, "0")}`;
};

export const formatGenres = (genres?: string[]): string | null => {
  const list = genres?.filter(Boolean) ?? [];
  return list.length > 0 ? list.join(GENRE_SEPARATOR) : null;
};

export const formatRating = (rating?: number): string | null => {
  if (rating == null || Number.isNaN(rating)) {
    return null;
  }

  return rating.toFixed(1);
};

export const formatYear = (year?: number): string | null => {
  if (year == null || Number.isNaN(year)) {
    return null;
  }

  return String(year);
};

export const getMovieCoreFields = (
  movie: Pick<
    Movie,
    "title" | "year" | "genres" | "runtimeMinutes" | "rating" | "director" | "plot"
  > | null
): MovieCoreFields => ({
  title: movie?.title ? movie.title : null,
  year: formatYear(movie?.year),
  genres: formatGenres(movie?.genres),
  duration: formatRuntime(movie?.runtimeMinutes),
  director: movie?.director ? movie.director : null,
  description: movie?.plot ? movie.plot : null,
  rating: formatRating(movie?.rating),
});
