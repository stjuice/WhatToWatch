import { Navigate, Route, Routes } from "react-router-dom";
import { HomeRoute } from "./HomeRoute";
import { ListRoute } from "./ListRoute";
import { MovieRoute } from "./MovieRoute";
import { StudioRoute } from "./StudioRoute";
import { routePaths } from "./routePaths";

export const AppRoutes = () => {
  return (
    <Routes>
      <Route path={routePaths.home} element={<HomeRoute />} />
      <Route path={routePaths.listPattern} element={<ListRoute />} />
      <Route path={routePaths.movie} element={<MovieRoute />} />
      <Route path={routePaths.studio} element={<StudioRoute />} />
      <Route path="*" element={<Navigate to={routePaths.home} replace />} />
    </Routes>
  );
};
