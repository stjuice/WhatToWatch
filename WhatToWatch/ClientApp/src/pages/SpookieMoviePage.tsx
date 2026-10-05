import { useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import { getSpookieMovie } from "../api/spookieNightApi";
import spider from "../assets/season/halloween/spider.svg";
import web from "../assets/season/halloween/web.svg";
import { text } from "../i18n/text";
import { useSpookieWatchLink } from "../layout/spookieWatchLink";
import { ErrorText } from "../primitives/ErrorText";
import { StatusText } from "../primitives/StatusText";
import type { Movie } from "../types/movie";
import { MoviePage } from "./MoviePage";
import { getSpookiePoster } from "./spookiePosters";
import "./SpookieMoviePage.scss";

export const SpookieMoviePage = () => {
  const { key } = useParams<{ key: string }>();
  const { setLink } = useSpookieWatchLink();
  const [movie, setMovie] = useState<Movie | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!key) {
      setMovie(null);
      setLink(null);
      setError(text("spookie.notFound"));
      setLoading(false);
      return;
    }

    let cancelled = false;
    setLoading(true);
    setError(null);
    setLink(null);

    const load = async () => {
      try {
        const loaded = await getSpookieMovie(key);
        if (!cancelled) {
          setMovie({
            ...loaded.movie,
            posterUrl: getSpookiePoster(key) ?? loaded.movie.posterUrl,
          });
          setLink(loaded.link?.trim() || null);
        }
      } catch {
        if (!cancelled) {
          setMovie(null);
          setLink(null);
          setError(text("spookie.notFound"));
        }
      } finally {
        if (!cancelled)
          setLoading(false);
      }
    };

    void load();
    return () => {
      cancelled = true;
      setLink(null);
    };
  }, [key, setLink]);

  return (
    <div className="spookie-movie">
      <img className="spookie-movie__web" src={web} alt="" draggable={false} />
      <img className="spookie-movie__spider" src={spider} alt="" draggable={false} />

      {loading ? (
        <StatusText className="spookie-movie__status">{text("spookie.loading")}</StatusText>
      ) : null}
      <ErrorText className="spookie-movie__error">{error}</ErrorText>

      {movie ? <MoviePage movie={movie} /> : null}
    </div>
  );
};
