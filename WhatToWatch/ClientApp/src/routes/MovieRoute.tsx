import { Navigate } from "react-router-dom";
import { MoviePage } from "../pages/MoviePage";
import { useAppState } from "../state/AppStateContext";
import { routePaths } from "./routePaths";

export const MovieRoute = () => {
  const { isListLoaded, movie, isRestoringMovie } = useAppState();

  if (isRestoringMovie) {
    return null;
  }

  if (!isListLoaded || !movie) {
    return <Navigate to={routePaths.home} replace />;
  }

  return <MoviePage />;
};
