import type { FormEvent } from "react";
import { useCallback, useEffect, useState } from "react";
import {
  clearAdminApiKey,
  getAdminApiKey,
  setAdminApiKey,
} from "../api/http";
import {
  deleteWatchlist,
  importWatchlistByUrl,
  refreshWatchlist,
  updateWatchlist,
} from "../api/moviesApi";
import { ImportFromImdbButton } from "../components/ImportFromImdbButton";
import { ImdbImportService } from "../services/imdbImportService";
import { useAppState } from "../state/AppStateContext";
import type { WatchlistDto } from "../types/movie";
import "./StudioPage.scss";

export const StudioPage = () => {
  const { watchlists, refreshWatchlists, watchlistsLoading } = useAppState();
  const [adminKey, setAdminKeyInput] = useState("");
  const [isAuthed, setIsAuthed] = useState(() => Boolean(getAdminApiKey()));
  const [url, setUrl] = useState("");
  const [isBusy, setIsBusy] = useState(false);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [importLog, setImportLog] = useState<string[]>([]);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [editingName, setEditingName] = useState("");
  const hasNativeImporter = ImdbImportService.isAvailable();

  useEffect(() => {
    void refreshWatchlists();
  }, [refreshWatchlists]);

  const runAction = useCallback(
    async (action: () => Promise<void>) => {
      setIsBusy(true);
      setError(null);
      setMessage(null);
      try {
        await action();
        await refreshWatchlists();
      } catch (err) {
        setError(err instanceof Error ? err.message : "Something went wrong");
      } finally {
        setIsBusy(false);
      }
    },
    [refreshWatchlists]
  );

  const handleLogin = (event: FormEvent) => {
    event.preventDefault();
    if (!adminKey.trim()) {
      setError("Enter the admin API key");
      return;
    }
    setAdminApiKey(adminKey);
    setIsAuthed(true);
    setError(null);
    setMessage("Signed in to Studio");
  };

  const handleLogout = () => {
    clearAdminApiKey();
    setIsAuthed(false);
    setAdminKeyInput("");
    setMessage(null);
  };

  const handleServerImport = () =>
    runAction(async () => {
      const trimmed = url.trim();
      if (!trimmed) {
        throw new Error("Enter an IMDb list URL");
      }
      const watchlist = await importWatchlistByUrl(trimmed);
      setMessage(`Imported “${watchlist.name}” (${watchlist.movies.length})`);
      setUrl("");
    });

  const handleNativeImport = () =>
    runAction(async () => {
      const trimmed = url.trim();
      if (!trimmed) {
        throw new Error("Enter an IMDb list URL");
      }

      setImportLog([
        "Opening IMDb in a secure window…",
        "Waiting for the list to load or for IMDb verification.",
      ]);

      try {
        const watchlist = await ImdbImportService.importFromImdb(trimmed, {
          onStatus: (entry) => setImportLog((current) => [...current.slice(-4), entry]),
        });
        setImportLog([
          `List “${watchlist.name}” saved on the server.`,
          `Movies: ${watchlist.movies.length}.`,
        ]);
        setMessage(`Imported “${watchlist.name}”`);
        setUrl("");
      } catch (err) {
        const text = err instanceof Error ? err.message : "Import failed";
        if (text === "cancelled") {
          setImportLog(["Import cancelled."]);
          return;
        }
        setImportLog([`Import error: ${text}`]);
        throw err;
      }
    });

  const handleRefresh = (list: WatchlistDto) =>
    runAction(async () => {
      const listUrl = list.url?.trim();
      if (hasNativeImporter && listUrl) {
        const updated = await ImdbImportService.importFromImdb(listUrl, {
          onStatus: (entry) => setImportLog((current) => [...current.slice(-4), entry]),
        });
        setMessage(`Refreshed “${updated.name}” (${updated.movies.length})`);
        return;
      }

      const updated = await refreshWatchlist(list.id);
      setMessage(`Refreshed “${updated.name}” (${updated.movies.length})`);
    });

  const handleDelete = (list: WatchlistDto) =>
    runAction(async () => {
      if (!window.confirm(`Delete “${list.name}”?`)) {
        return;
      }
      await deleteWatchlist(list.id);
      setMessage(`Deleted “${list.name}”`);
    });

  const handleSaveName = (id: string) =>
    runAction(async () => {
      const name = editingName.trim();
      if (!name) {
        throw new Error("Name cannot be empty");
      }
      const updated = await updateWatchlist(id, { name });
      setEditingId(null);
      setMessage(`Saved “${updated.name}”`);
    });

  if (!isAuthed) {
    return (
      <div className="studio">
        <h1 className="studio__title">Studio</h1>
        <form className="studio__login" onSubmit={handleLogin}>
          <label className="studio__label" htmlFor="admin-key">
            Admin API key
          </label>
          <input
            id="admin-key"
            className="studio__input"
            type="password"
            autoComplete="current-password"
            value={adminKey}
            onChange={(event) => setAdminKeyInput(event.target.value)}
          />
          <button className="studio__button" type="submit">
            Sign in
          </button>
          <p className="studio__status">
            Same key as on the server. If <code>ADMIN_API_KEY</code> is not set on Render,
            use <code>dev-admin-key</code>.
          </p>
        </form>
        {error ? (
          <p className="studio__error" role="alert">
            {error}
          </p>
        ) : null}
      </div>
    );
  }

  return (
    <div className="studio">
      <div className="studio__header">
        <h1 className="studio__title">Studio</h1>
        <button className="studio__button studio__button--ghost" type="button" onClick={handleLogout}>
          Sign out
        </button>
      </div>

      <section className="studio__panel">
        <h2 className="studio__section-title">Import from IMDb</h2>
        <input
          className="studio__input"
          type="url"
          value={url}
          onChange={(event) => setUrl(event.target.value)}
          placeholder="https://www.imdb.com/list/…"
          disabled={isBusy}
          aria-label="IMDb list URL"
        />
        <div className="studio__actions">
          {hasNativeImporter ? (
            <ImportFromImdbButton
              disabled={isBusy || !url.trim()}
              onImport={() => {
                void handleNativeImport();
              }}
            />
          ) : (
            <button
              className="studio__button"
              type="button"
              disabled={isBusy || !url.trim()}
              onClick={() => {
                void handleServerImport();
              }}
            >
              Import (server)
            </button>
          )}
        </div>
        {importLog.length > 0 ? (
          <div className="studio__log" role="status" aria-live="polite">
            {importLog.map((entry, index) => (
              <div key={`${index}-${entry}`}>{entry}</div>
            ))}
          </div>
        ) : null}
      </section>

      <section className="studio__panel">
        <h2 className="studio__section-title">Watchlists</h2>
        {watchlistsLoading ? <p className="studio__status">Loading…</p> : null}
        <ul className="studio__lists">
          {watchlists.map((list) => (
            <li key={list.id} className="studio__list-item">
              {editingId === list.id ? (
                <div className="studio__edit-row">
                  <input
                    className="studio__input"
                    value={editingName}
                    onChange={(event) => setEditingName(event.target.value)}
                    disabled={isBusy}
                  />
                  <button
                    className="studio__button"
                    type="button"
                    disabled={isBusy}
                    onClick={() => {
                      void handleSaveName(list.id);
                    }}
                  >
                    Save
                  </button>
                  <button
                    className="studio__button studio__button--ghost"
                    type="button"
                    disabled={isBusy}
                    onClick={() => setEditingId(null)}
                  >
                    Cancel
                  </button>
                </div>
              ) : (
                <>
                  <div className="studio__list-meta">
                    <strong>{list.name}</strong>
                    <span>
                      {list.id} · {list.movies.length} movies
                    </span>
                  </div>
                  <div className="studio__list-actions">
                    <button
                      className="studio__button studio__button--ghost"
                      type="button"
                      disabled={isBusy}
                      onClick={() => {
                        setEditingId(list.id);
                        setEditingName(list.name);
                      }}
                    >
                      Edit
                    </button>
                    <button
                      className="studio__button studio__button--ghost"
                      type="button"
                      disabled={isBusy}
                      onClick={() => {
                        void handleRefresh(list);
                      }}
                    >
                      Refresh
                    </button>
                    <button
                      className="studio__button studio__button--danger"
                      type="button"
                      disabled={isBusy}
                      onClick={() => {
                        void handleDelete(list);
                      }}
                    >
                      Delete
                    </button>
                  </div>
                </>
              )}
            </li>
          ))}
        </ul>
      </section>

      {message ? <p className="studio__message">{message}</p> : null}
      {error ? (
        <p className="studio__error" role="alert">
          {error}
        </p>
      ) : null}
    </div>
  );
};
