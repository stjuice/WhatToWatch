import frame from "../assets/frame.svg";
import placeholder from "../assets/placeholder.svg";
import { RerollButton } from "../components/RerollButton";
import { useAppState } from "../state/AppStateContext";
import "./MoviePage.scss";

const formatRuntime = (minutes?: number): string | null => {
  if (minutes == null || Number.isNaN(minutes) || minutes < 0) {
    return null;
  }

  const hours = Math.floor(minutes / 60);
  const mins = minutes % 60;
  return `${hours}:${mins.toString().padStart(2, "0")}`;
};

export const MoviePage = () => {
  const { movie } = useAppState();
  const posterSrc = movie?.posterUrl || placeholder;
  const genres = movie?.genres?.filter(Boolean) ?? [];
  const runtime = formatRuntime(movie?.runtimeMinutes);

  return (
    <div className="movie-detail">
      <div className="movie-detail__poster-stack">
        <img
          className="movie-detail__under"
          src={posterSrc}
          alt={movie?.title ? `Постер: ${movie.title}` : "Постер фільму"}
        />
        <img className="movie-detail__frame" src={frame} alt="" draggable={false} />
      </div>

      {movie?.title ? <h1 className="movie-detail__title">{movie.title}</h1> : null}

      {genres.length > 0 ? (
        <p className="movie-detail__genres">{genres.join(" | ")}</p>
      ) : null}

      {runtime ? <p className="movie-detail__runtime">{runtime}</p> : null}

      {movie?.plot ? <p className="movie-detail__plot">{movie.plot}</p> : null}

      {movie?.director ? (
        <p className="movie-detail__director">{movie.director}</p>
      ) : null}

      {movie?.rating != null ? (
        <p className="movie-detail__rating">
          <span className="movie-detail__rating-star" aria-hidden="true">
            ★
          </span>
          <span>{movie.rating.toFixed(1)}</span>
        </p>
      ) : null}

      <RerollButton />
    </div>
  );
};
