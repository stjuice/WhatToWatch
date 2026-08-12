import { useLocation } from "react-router-dom";
import logo from "../assets/logo.svg";
import { AppRoutes } from "../routes/AppRoutes";
import { routePaths } from "../routes/routePaths";

export const AppLayout = () => {
  const { pathname } = useLocation();
  const isMovieScreen = pathname === routePaths.movie;

  return (
    <div className="app">
      <header className={`app__brand${isMovieScreen ? " app__brand--compact" : ""}`}>
        <img className="app__logo" src={logo} alt="WhatToWatch" />
      </header>

      <main className="app__main">
        <AppRoutes />
      </main>
    </div>
  );
};
