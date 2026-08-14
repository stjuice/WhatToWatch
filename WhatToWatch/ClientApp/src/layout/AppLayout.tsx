import { Link, matchPath, useLocation } from "react-router-dom";
import logo from "../assets/logo.svg";
import { RerollButton } from "../components/RerollButton";
import { useGestureBack } from "../hooks/useGestureBack";
import { text } from "../i18n/text";
import { AppRoutes } from "../routes/AppRoutes";
import { routePaths } from "../routes/routePaths";
import { useAppState } from "../state/AppStateContext";

export const AppLayout = () => {
  const { pathname } = useLocation();
  const { watchlistId } = useAppState();
  const isMovieScreen = pathname === routePaths.movie;
  const isListScreen = Boolean(matchPath(routePaths.listPattern, pathname));
  const isStudio = pathname.startsWith(routePaths.studio);

  useGestureBack({
    enabled: isListScreen || isMovieScreen,
    fallbackTo: isMovieScreen && watchlistId ? routePaths.list(watchlistId) : routePaths.home,
  });

  return (
    <div className="app">
      <header className={`app__brand${isMovieScreen ? " app__brand--compact" : ""}`}>
        <Link className="app__brand-link" to={routePaths.home} aria-label={text("nav.homeAria")}>
          <img className="app__logo" src={logo} alt="WhatToWatch" draggable={false} />
        </Link>
      </header>

      <main className="app__main">
        <AppRoutes />
      </main>

      {isMovieScreen ? (
        <footer className="app__footer app__footer--reroll">
          <RerollButton />
        </footer>
      ) : !isStudio ? (
        <footer className="app__footer">
          <Link className="app__studio-link" to={routePaths.studio}>
            Studio
          </Link>
        </footer>
      ) : null}
    </div>
  );
};
