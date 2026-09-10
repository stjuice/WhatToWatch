import frame from "../assets/frame.svg";
import placeholder from "../assets/placeholder.svg";
import { text } from "../i18n/text";
import { ErrorText } from "../primitives/ErrorText";
import { useAppState } from "../state/AppStateContext";
import type { MovieDto } from "../types/movie";
import { getImdbTitleUrl, getMovieCoreFields } from "./movieCoreFields";
import "./MoviePage.scss";

export interface MoviePageProps {
  movie?: MovieDto | null;
  showImdbLink?: boolean;
}

export const MoviePage = ({
  movie: movieOverride,
  showImdbLink = true,
}: MoviePageProps = {}) => {
  const {
    movie: contextMovie,
    isPicking: contextIsPicking,
    pickError: contextPickError,
  } = useAppState();
  const usesContextMovie = movieOverride === undefined;
  const movie = usesContextMovie ? contextMovie : movieOverride;
  const isPicking = usesContextMovie && contextIsPicking;
  const pickError = usesContextMovie ? contextPickError : null;
  const posterSrc = movie?.posterUrl || placeholder;
  const fields = getMovieCoreFields(movie);
  const posterAlt = movie?.title
    ? text("movie.posterAlt", { title: movie.title })
    : text("movie.posterAltFallback");
    
  const poster = (
    <img
      className="movie-detail__poster"
      src={posterSrc}
      alt={posterAlt}
    />
  );

  return (
    <div className="movie-detail">
      <div className="movie-detail__poster-stack" data-loading={isPicking}>
        <div className="movie-detail__window">
          {movie?.id && showImdbLink ? (
            <a
              className="movie-detail__poster-link"
              href={getImdbTitleUrl(movie.id)}
              target="_blank"
              rel="noopener noreferrer"
              aria-label={text("movie.openOnImdb", { title: movie.title })}
            >
              {poster}
            </a>
          ) : (
            poster
          )}
        </div>
        <img className="movie-detail__frame" src={frame} alt="" draggable={false} />
      </div>

      <div>
        {fields.title && <h1 className="movie-detail__title">{fields.title}</h1>}
        {fields.year && <p className="movie-detail__year">{fields.year}</p>}
        {fields.director && <p className="movie-detail__director">{fields.director}</p>}
      </div>

      {fields.genres && <p className="movie-detail__genres">{fields.genres}</p>}

      {fields.duration && <p className="movie-detail__runtime">{fields.duration}</p>}


      {fields.description && <p className="movie-detail__plot">{fields.description}</p>}

      {fields.rating && (
        <p className="movie-detail__rating">
          <span className="movie-detail__rating-star" aria-hidden="true">
            ★
          </span>
          <span>{fields.rating}</span>
        </p>
      )}

      <ErrorText className="movie-detail__error">{pickError}</ErrorText>
    </div>
  );
};
