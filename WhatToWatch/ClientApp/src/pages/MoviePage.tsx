import frame from "../assets/frame.svg";
import placeholder from "../assets/placeholder.svg";
import { RerollButton } from "../components/RerollButton";
import { text } from "../i18n/text";
import { useAppState } from "../state/AppStateContext";
import { getMovieCoreFields } from "./movieCoreFields";
import "./MoviePage.scss";

export const MoviePage = () => {
  const { movie, isPicking, pickError } = useAppState();
  const posterSrc = movie?.posterUrl || placeholder;
  const fields = getMovieCoreFields(movie);

  return (
    <div className="movie-detail">
      <div className="movie-detail__poster-stack" data-loading={isPicking}>
        <div className="movie-detail__window">
          <img
            className="movie-detail__poster"
            src={posterSrc}
            alt={
              movie?.title
                ? text("movie.posterAlt", { title: movie.title })
                : text("movie.posterAltFallback")
            }
          />
        </div>
        <img className="movie-detail__frame" src={frame} alt="" draggable={false} />
      </div>

      <div>
        {fields.title && <h1 className="movie-detail__title">{fields.title}</h1>}
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

      {pickError && (
        <p className="movie-detail__error" role="alert">
          {pickError}
        </p>
      )}

      <RerollButton />
    </div>
  );
};
