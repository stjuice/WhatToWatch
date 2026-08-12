import { Navigate, Route, Routes } from "react-router-dom";
import { HomeRoute } from "./HomeRoute";
import { MovieRoute } from "./MovieRoute";
import { routePaths } from "./routePaths";

export const AppRoutes = () => {
  return (
    <Routes>
      <Route path={routePaths.home} element={<HomeRoute />} />
      <Route path={routePaths.movie} element={<MovieRoute />} />
      <Route path="*" element={<Navigate to={routePaths.home} replace />} />
    </Routes>
  );
};
