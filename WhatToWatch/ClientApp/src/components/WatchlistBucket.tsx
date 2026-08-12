import type { ChangeEvent } from "react";
import { useAppState } from "../state/AppStateContext";
import { AddLinkButton } from "./AddLinkButton";
import { ClearLinkButton } from "./ClearLinkButton";
import { RandomButton } from "./RandomButton";
import "./WatchlistBucket.scss";

export function WatchlistBucket() {
  const { url, setUrl, isListLoaded, importError } = useAppState();
  const isReadyToUpload = !isListLoaded && url.trim().length > 0;

  function handleUrlChange(event: ChangeEvent<HTMLInputElement>) {
    setUrl(event.target.value);
  }

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
}
