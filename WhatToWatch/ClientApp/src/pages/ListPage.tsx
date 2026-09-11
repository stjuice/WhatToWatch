import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { getWatchlist } from "../api/moviesApi";
import listFrameLong from "../assets/list-frame-long.svg";
import listFrameShort from "../assets/list-frame-short.svg";
import { MainButton } from "../components/MainButton";
import { text } from "../i18n/text";
import { Button } from "../primitives/Button";
import { ErrorText } from "../primitives/ErrorText";
import { StatusText } from "../primitives/StatusText";
import { routePaths } from "../routes/routePaths";
import { useAppState } from "../state/AppStateContext";
import type { Watchlist } from "../types/movie";
import { LIST_VISIBLE_ROWS, splitWatchlistHeadline } from "./listMarquee";
import "./ListPage.scss";

export const ListPage = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { isPicking, pickError, pickRandomMovie, setActiveWatchlistId } = useAppState();
  const [watchlist, setWatchlist] = useState<Watchlist | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [expanded, setExpanded] = useState(false);

  useEffect(() => {
    setExpanded(false);

    if (!id) {
      setError(text("list.notFound"));
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
        if (!cancelled)
          setWatchlist(list);
      } catch (err) {
        if (!cancelled) {
          setWatchlist(null);
          setError(err instanceof Error ? err.message : text("list.loadFailed"));
        }
      } finally {
        if (!cancelled)
          setLoading(false);
      }
    };

    void load();
    return () => {
      cancelled = true;
    };
  }, [id, setActiveWatchlistId]);

  const handleRandom = async () => {
    if (!id)
      return;

    const picked = await pickRandomMovie(id);
    if (picked)
      navigate(routePaths.movie);
  };

  const headline = watchlist ? splitWatchlistHeadline(watchlist.name) : null;
  const movieCount = watchlist?.movies.length ?? 0;

  return (
    <div className={`list-page${expanded ? " list-page--expanded" : ""}`}>
      <div className="list-page__frame-preload" aria-hidden="true">
        <img src={listFrameShort} alt="" />
        <img src={listFrameLong} alt="" />
      </div>
      {loading ? (
        <StatusText className="list-page__status">{text("list.loading")}</StatusText>
      ) : null}
      <ErrorText className="list-page__error">{error}</ErrorText>

      {watchlist && headline ? (
        <>
          <div
            className={`list-page__marquee list-page__marquee--${expanded ? "long" : "short"}`}
            style={{ ["--list-visible-rows" as string]: String(LIST_VISIBLE_ROWS) }}
          >
            <img
              className={`list-page__marquee-art${expanded ? "" : " list-page__marquee-art--visible"}`}
              src={listFrameShort}
              alt=""
              draggable={false}
              decoding="async"
            />
            <img
              className={`list-page__marquee-art${expanded ? " list-page__marquee-art--visible" : ""}`}
              src={listFrameLong}
              alt=""
              draggable={false}
              decoding="async"
            />
            <div className="list-page__marquee-window">
              <h1 className="list-page__title">{headline.title}</h1>
              {headline.subtitle ? (
                <p className="list-page__subtitle">{headline.subtitle}</p>
              ) : null}
              <Button
                variant="plain"
                className="list-page__count"
                onClick={() => {
                  setExpanded((open) => !open);
                }}
                aria-expanded={expanded}
                aria-label={text(expanded ? "list.collapseAria" : "list.expandAria")}
              >
                {text("list.movieCount", { count: movieCount })}
              </Button>

              {expanded ? (
                <ul className="list-page__movies">
                  {watchlist.movies.map((movie) => (
                    <li key={movie.id} className="list-page__movie">
                      <span className="list-page__movie-title">{movie.title}</span>
                    </li>
                  ))}
                </ul>
              ) : null}
            </div>
          </div>

          <MainButton
            size="ml"
            label={text("list.randomAria", { name: watchlist.name })}
            className="list-page__random"
            onClick={() => {
              void handleRandom();
            }}
            disabled={isPicking || movieCount === 0}
            aria-busy={isPicking}
          />

          <ErrorText className="list-page__error">{pickError}</ErrorText>
        </>
      ) : null}
    </div>
  );
};
