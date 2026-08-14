import { useEffect, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { getWatchlist } from "../api/moviesApi";
import popcornFull from "../assets/popcorn-full.svg";
import { Button } from "../primitives/Button";
import { routePaths } from "../routes/routePaths";
import { useAppState } from "../state/AppStateContext";
import type { WatchlistDto } from "../types/movie";
import "./ListPage.scss";

export const ListPage = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { isPicking, pickError, pickRandomMovie, setActiveWatchlistId } = useAppState();
  const [watchlist, setWatchlist] = useState<WatchlistDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!id) {
      setError("Список не знайдено");
      setLoading(false);
      return;
    }

    let cancelled = false;
    setLoading(true);
    setError(null);
    setActiveWatchlistId(id);

    const load = async () => {
      try {
        const list = await getWatchlist(id);
        if (!cancelled) {
          setWatchlist(list);
        }
      } catch (err) {
        if (!cancelled) {
          setWatchlist(null);
          setError(err instanceof Error ? err.message : "Не вдалося завантажити список");
        }
      } finally {
        if (!cancelled) {
          setLoading(false);
        }
      }
    };

    void load();
    return () => {
      cancelled = true;
    };
  }, [id, setActiveWatchlistId]);

  const handleRandom = async () => {
    if (!id) {
      return;
    }
    const picked = await pickRandomMovie(id);
    if (picked) {
      navigate(routePaths.movie);
    }
  };

  return (
    <div className="list-page">
      <Link className="list-page__back" to={routePaths.home}>
        ← Усі списки
      </Link>

      {loading ? <p className="list-page__status">Завантаження…</p> : null}
      {error ? (
        <p className="list-page__error" role="alert">
          {error}
        </p>
      ) : null}

      {watchlist ? (
        <>
          <h1 className="list-page__title">{watchlist.name}</h1>
          <p className="list-page__meta">{watchlist.movies.length} фільмів</p>

          <Button
            variant="icon"
            className="list-page__random"
            onClick={() => {
              void handleRandom();
            }}
            disabled={isPicking || watchlist.movies.length === 0}
            aria-busy={isPicking}
            aria-label={`Випадковий фільм зі списку ${watchlist.name}`}
          >
            <img src={popcornFull} alt="" draggable={false} />
          </Button>

          {pickError ? (
            <p className="list-page__error" role="alert">
              {pickError}
            </p>
          ) : null}

          <ul className="list-page__movies">
            {watchlist.movies.map((movie) => (
              <li key={movie.id} className="list-page__movie">
                <span className="list-page__movie-title">{movie.title}</span>
                {movie.year != null ? (
                  <span className="list-page__movie-year">{movie.year}</span>
                ) : null}
              </li>
            ))}
          </ul>
        </>
      ) : null}
    </div>
  );
};
