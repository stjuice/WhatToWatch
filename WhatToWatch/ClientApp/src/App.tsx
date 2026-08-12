import { Navigate, Route, Routes, useLocation } from "react-router-dom";
import logo from "./assets/logo.svg";
import { MovieDetail } from "./components/MovieDetail";
import { WatchlistBucket } from "./components/WatchlistBucket";
import { routes } from "./routes";
import { AppStateProvider, useAppState } from "./state/AppStateContext";
import "./App.scss";

function MovieRoute() {
  const { isListLoaded } = useAppState();

  if (!isListLoaded) {
    return <Navigate to={routes.home} replace />;
  }

  return <MovieDetail />;
}

function AppScreens() {
  const { pathname } = useLocation();
  const isMovieScreen = pathname === routes.movie;

  return (
    <div className="app">
      {isMovieScreen ? null : (
        <header className="app__brand">
          <img className="app__logo" src={logo} alt="WhatToWatch" />
        </header>
      )}

      <main className="app__main">
        <Routes>
          <Route path={routes.home} element={<WatchlistBucket />} />
          <Route path={routes.movie} element={<MovieRoute />} />
          <Route path="*" element={<Navigate to={routes.home} replace />} />
        </Routes>
      </main>
    </div>
  );
}

export function App() {
  return (
    <AppStateProvider>
      <AppScreens />
    </AppStateProvider>
  );
}
