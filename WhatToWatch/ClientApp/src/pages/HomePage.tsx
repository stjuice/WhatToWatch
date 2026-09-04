import { useNavigate } from "react-router-dom";
import { MainButton } from "../components/MainButton";
import { WatchlistBucket } from "../components/WatchlistBucket";
import { useViewportZoom } from "../hooks/useViewportZoom";
import { text } from "../i18n/text";
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
  const zoomedIn = useViewportZoom();

  const handleRandomAll = async () => {
    const picked = await pickRandomMovie(null);
    if (picked) {
      navigate(routePaths.movie);
    }
  };

  return (
    <div className="public-home">
      <MainButton
        size="large"
        label={text("home.randomAllAria")}
        className="public-home__random"
        onClick={() => {
          void handleRandomAll();
        }}
        disabled={isPicking || watchlists.length === 0}
        aria-busy={isPicking}
      />

      <p className="public-home__hint">{text("home.randomAll")}</p>

      {watchlistsLoading ? <p className="public-home__status">{text("home.loading")}</p> : null}
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
        <p className="public-home__status">{text("home.empty")}</p>
      ) : null}

      <ul className={`public-home__lists${zoomedIn ? " public-home__lists--large" : ""}`}>
        {watchlists.map((list) => (
          <li key={list.id} className="public-home__list-item">
            <WatchlistBucket
              id={list.id}
              name={list.name}
              size={zoomedIn ? "large" : "compact"}
            />
          </li>
        ))}
      </ul>
    </div>
  );
};
