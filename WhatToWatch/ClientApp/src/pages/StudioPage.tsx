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
        setError(err instanceof Error ? err.message : "Помилка");
      } finally {
        setIsBusy(false);
      }
    },
    [refreshWatchlists]
  );

  const handleLogin = (event: FormEvent) => {
    event.preventDefault();
    if (!adminKey.trim()) {
      setError("Введіть ключ адміністратора");
      return;
    }
    setAdminApiKey(adminKey);
    setIsAuthed(true);
    setError(null);
    setMessage("Увійшли в Studio");
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
        throw new Error("Вкажіть посилання на список IMDb");
      }
      const watchlist = await importWatchlistByUrl(trimmed);
      setMessage(`Імпортовано «${watchlist.name}» (${watchlist.movies.length})`);
      setUrl("");
    });

  const handleNativeImport = () =>
    runAction(async () => {
      const trimmed = url.trim();
      if (!trimmed) {
        throw new Error("Вкажіть посилання на список IMDb");
      }

      setImportLog([
        "Відкриваємо IMDb у захищеному вікні…",
        "Очікуємо завантаження списку або перевірки IMDb.",
      ]);

      try {
        const watchlist = await ImdbImportService.importFromImdb(trimmed, {
          onStatus: (entry) => setImportLog((current) => [...current.slice(-4), entry]),
        });
        setImportLog([
          `Список «${watchlist.name}» збережено на сервері.`,
          `Фільмів: ${watchlist.movies.length}.`,
        ]);
        setMessage(`Імпортовано «${watchlist.name}»`);
        setUrl("");
      } catch (err) {
        const text = err instanceof Error ? err.message : "Помилка імпорту";
        if (text === "cancelled") {
          setImportLog(["Імпорт скасовано."]);
          return;
        }
        setImportLog([`Помилка імпорту: ${text}`]);
        throw err;
      }
    });

  const handleRefresh = (list: WatchlistDto) =>
    runAction(async () => {
      const updated = await refreshWatchlist(list.id);
      setMessage(`Оновлено «${updated.name}» (${updated.movies.length})`);
    });

  const handleDelete = (list: WatchlistDto) =>
    runAction(async () => {
      if (!window.confirm(`Видалити «${list.name}»?`)) {
        return;
      }
      await deleteWatchlist(list.id);
      setMessage(`Видалено «${list.name}»`);
    });

  const handleSaveName = (id: string) =>
    runAction(async () => {
      const name = editingName.trim();
      if (!name) {
        throw new Error("Назва не може бути порожньою");
      }
      const updated = await updateWatchlist(id, { name });
      setEditingId(null);
      setMessage(`Збережено «${updated.name}»`);
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
            Увійти
          </button>
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
          Вийти
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
          aria-label="Посилання на список IMDb"
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
        {watchlistsLoading ? <p className="studio__status">Завантаження…</p> : null}
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
