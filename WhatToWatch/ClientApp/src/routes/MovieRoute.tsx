import { Navigate } from "react-router-dom";
import { MoviePage } from "../pages/MoviePage";
import { useAppState } from "../state/AppStateContext";
import { routePaths } from "./routePaths";

export const MovieRoute = () => {
  const { isListLoaded } = useAppState();

  if (!isListLoaded) {
    return <Navigate to={routePaths.home} replace />;
  }

  return <MoviePage />;
};
