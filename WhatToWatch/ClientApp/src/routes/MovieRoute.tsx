import { MoviePage } from "../pages/MoviePage";
import { Navigate } from "react-router-dom";
import { useAppState } from "../state/AppStateContext";
import { routePaths } from "./routePaths";

export const MovieRoute = () => {
  const { movie, isRestoringMovie } = useAppState();

  if (isRestoringMovie) {
    return null;
  }

  if (!movie) {
    return <Navigate to={routePaths.home} replace />;
  }

  return <MoviePage />;
};
