import type { ChangeEvent } from "react";
import { AddLinkButton } from "../components/AddLinkButton";
import { ClearLinkButton } from "../components/ClearLinkButton";
import { RandomButton } from "../components/RandomButton";
import { useAppState } from "../state/AppStateContext";
import "./HomePage.scss";

export const HomePage = () => {
  const { url, setUrl, isListLoaded, importError } = useAppState();
  const isReadyToUpload = !isListLoaded && url.trim().length > 0;

  const handleUrlChange = (event: ChangeEvent<HTMLInputElement>) => {
    setUrl(event.target.value);
  };

  return (
    <div className="watchlist-bucket">
      <div className="watchlist-bucket__stage">
        {isListLoaded ? <RandomButton /> : <ClearLinkButton />}
      </div>

      {isListLoaded ? null : (
        <div className="watchlist-bucket__controls">
          <input
            className="watchlist-bucket__input"
            type="url"
            value={url}
            onChange={handleUrlChange}
            placeholder="ВСТАВИТИ ПОСИЛАННЯ"
            aria-label="Посилання на список IMDb"
          />

          {isReadyToUpload ? <AddLinkButton /> : null}

          {importError ? (
            <p className="watchlist-bucket__error" role="alert">
              {importError}
            </p>
          ) : null}
        </div>
      )}
    </div>
  );
};
