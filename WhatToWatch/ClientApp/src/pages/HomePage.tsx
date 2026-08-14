import { Link, useNavigate } from "react-router-dom";
import popcornFull from "../assets/popcorn-full.svg";
import { Button } from "../primitives/Button";
import { routePaths } from "../routes/routePaths";
import { useAppState } from "../state/AppStateContext";
import "./HomePage.scss";

export const HomePage = () => {
  const {
    watchlists,
    watchlistsLoading,
    watchlistsError,
    isPicking,
    pickError,
    pickRandomMovie,
  } = useAppState();
  const navigate = useNavigate();

  const handleRandomAll = async () => {
    const picked = await pickRandomMovie(null);
    if (picked) {
      navigate(routePaths.movie);
    }
  };

  return (
    <div className="public-home">
      <Button
        variant="icon"
        className="public-home__random"
        onClick={() => {
          void handleRandomAll();
        }}
        disabled={isPicking || watchlists.length === 0}
        aria-busy={isPicking}
        aria-label="Випадковий фільм з усіх списків"
      >
        <img src={popcornFull} alt="" draggable={false} />
      </Button>

      <p className="public-home__hint">Випадковий фільм з усіх списків</p>

      {watchlistsLoading ? <p className="public-home__status">Завантаження…</p> : null}
      {watchlistsError ? (
        <p className="public-home__error" role="alert">
          {watchlistsError}
        </p>
      ) : null}
      {pickError ? (
        <p className="public-home__error" role="alert">
          {pickError}
        </p>
      ) : null}

      {!watchlistsLoading && watchlists.length === 0 && !watchlistsError ? (
        <p className="public-home__status">Поки немає жодного списку.</p>
      ) : null}

      <ul className="public-home__lists">
        {watchlists.map((list) => (
          <li key={list.id}>
            <Link className="public-home__list-link" to={routePaths.list(list.id)}>
              <span className="public-home__list-name">{list.name}</span>
              <span className="public-home__list-count">{list.movies.length}</span>
            </Link>
          </li>
        ))}
      </ul>
    </div>
  );
};
