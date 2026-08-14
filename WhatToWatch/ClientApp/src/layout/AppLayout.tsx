import { Link, useLocation } from "react-router-dom";
import logo from "../assets/logo.svg";
import { AppRoutes } from "../routes/AppRoutes";
import { routePaths } from "../routes/routePaths";

export const AppLayout = () => {
  const { pathname } = useLocation();
  const isMovieScreen = pathname === routePaths.movie;
  const isStudio = pathname.startsWith(routePaths.studio);

  return (
    <div className="app">
      <header className={`app__brand${isMovieScreen ? " app__brand--compact" : ""}`}>
        <Link className="app__brand-link" to={routePaths.home} aria-label="На головну">
          <img className="app__logo" src={logo} alt="WhatToWatch" draggable={false} />
        </Link>
      </header>

      <main className="app__main">
        <AppRoutes />
      </main>

      {!isStudio && !isMovieScreen ? (
        <footer className="app__footer">
          <Link className="app__studio-link" to={routePaths.studio}>
            Studio
          </Link>
        </footer>
      ) : null}
    </div>
  );
};
