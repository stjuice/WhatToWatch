import { useNavigate } from "react-router-dom";
import popcornFull from "../assets/popcorn-full.svg";
import { PopcornButton } from "../components/PopcornButton";
import { WatchlistBucket } from "../components/WatchlistBucket";
import { useViewportZoom } from "../hooks/useViewportZoom";
import { text } from "../i18n/text";
import { ErrorText } from "../primitives/ErrorText";
import { StatusText } from "../primitives/StatusText";
import { routePaths } from "../routes/routePaths";
import { useAppState } from "../state/AppStateContext";
import "./WatchlistsPage.scss";

export const WatchlistsPage = () => {
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
    <div className="watchlists-page">
      <PopcornButton
        art={popcornFull}
        label={text("watchlists.randomAllAria")}
        size="l"
        className="watchlists-page__random"
        onClick={() => {
          void handleRandomAll();
        }}
        disabled={isPicking || watchlists.length === 0}
        busy={isPicking}
      />

      <StatusText className="watchlists-page__hint">
        {text("watchlists.randomAll")}
      </StatusText>

      {watchlistsLoading ? (
        <StatusText className="watchlists-page__status">
          {text("watchlists.loading")}
        </StatusText>
      ) : null}
      <ErrorText className="watchlists-page__error">{watchlistsError}</ErrorText>
      <ErrorText className="watchlists-page__error">{pickError}</ErrorText>

      {!watchlistsLoading && watchlists.length === 0 && !watchlistsError ? (
        <StatusText className="watchlists-page__status">
          {text("watchlists.empty")}
        </StatusText>
      ) : null}

      <ul
        className={`watchlists-page__lists${
          zoomedIn ? " watchlists-page__lists--large" : ""
        }`}
      >
        {watchlists.map((list) => (
          <li key={list.id} className="watchlists-page__list-item">
            <WatchlistBucket
              id={list.id}
              name={list.name}
              size="ml"
            />
          </li>
        ))}
      </ul>
    </div>
  );
};
