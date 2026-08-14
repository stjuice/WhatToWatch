import { useEffect, useState } from "react";
import type { ChangeEvent, KeyboardEvent } from "react";
import { AddLinkButton } from "../components/AddLinkButton";
import { ClearLinkButton } from "../components/ClearLinkButton";
import { ImportFromImdbButton } from "../components/ImportFromImdbButton";
import { RandomButton } from "../components/RandomButton";
import { useAppState } from "../state/AppStateContext";
import "./HomePage.scss";

type BucketState = "idle" | "readyToUpload" | "importing" | "loaded";

const resolveBucketState = (
  isListLoaded: boolean,
  isImporting: boolean,
  url: string
): BucketState => {
  if (isListLoaded) {
    return "loaded";
  }
  if (isImporting) {
    return "importing";
  }

  return url.trim().length > 0 ? "readyToUpload" : "idle";
};

export const HomePage = () => {
  const { url, setUrl, isListLoaded, isImporting, importError, pickError, loadList } =
    useAppState();

  // Dev-only switch to preview the empty/upload states without dropping the imported list.
  const [isEmptyPreview, setIsEmptyPreview] = useState(false);
  const showAsLoaded = isListLoaded && !(import.meta.env.DEV && isEmptyPreview);
  const bucketState = resolveBucketState(showAsLoaded, isImporting, url);
  const error = importError ?? pickError;

  useEffect(() => {
    if (isImporting || !isListLoaded)
      setIsEmptyPreview(false);

  }, [isImporting, isListLoaded]);

  const handleUrlChange = (event: ChangeEvent<HTMLInputElement>) => {
    setUrl(event.target.value);
  };

  const handleUrlKeyDown = (event: KeyboardEvent<HTMLInputElement>) => {
    if (event.key !== "Enter")
      return;

    event.preventDefault();
    void loadList();
  };

  return (
    <div className="watchlist-bucket" data-state={bucketState}>
      <div className="watchlist-bucket__stage">
        {bucketState === "loaded" ? <RandomButton /> : <ClearLinkButton />}

        {bucketState === "loaded" ? null : (
          <div className="watchlist-bucket__controls">
            <input
              className="watchlist-bucket__input"
              type="url"
              value={url}
              onChange={handleUrlChange}
              onKeyDown={handleUrlKeyDown}
              disabled={isImporting}
              placeholder="ВСТАВИТИ ПОСИЛАННЯ"
              aria-label="Посилання на список IMDb"
            />

            <div className="watchlist-bucket__action">
              {bucketState === "idle" ? null : <AddLinkButton />}
              <ImportFromImdbButton />
            </div>
          </div>
        )}
      </div>

      {error ? (
        <p className="watchlist-bucket__error" role="alert">
          {error}
        </p>
      ) : null}

      {import.meta.env.DEV && isListLoaded ? (
        <button
          type="button"
          className="watchlist-bucket__preview-toggle"
          data-active={isEmptyPreview ? "true" : undefined}
          onClick={() => {
            setIsEmptyPreview((current) => !current);
          }}
        >
          {isEmptyPreview ? "dev: повний кошик" : "dev: порожній кошик"}
        </button>
      ) : null}
    </div>
  );
};
