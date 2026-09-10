import { Navigate, Route, Routes } from "react-router-dom";
import { HomeRoute } from "./HomeRoute";
import { ListRoute } from "./ListRoute";
import { MovieRoute } from "./MovieRoute";
import { PartyJoinRoute } from "./PartyJoinRoute";
import { PartyPlayRoute } from "./PartyPlayRoute";
import { PartyStartRoute } from "./PartyStartRoute";
import { StudioRoute } from "./StudioRoute";
import { WatchlistsRoute } from "./WatchlistsRoute";
import { routePaths } from "./routePaths";

export const AppRoutes = () => {
  return (
    <Routes>
      <Route path={routePaths.home} element={<HomeRoute />} />
      <Route path={routePaths.watchlists} element={<WatchlistsRoute />} />
      <Route path={routePaths.listPattern} element={<ListRoute />} />
      <Route path={routePaths.movie} element={<MovieRoute />} />
      <Route path={routePaths.party} element={<PartyStartRoute />} />
      <Route path={routePaths.partyJoinPattern} element={<PartyJoinRoute />} />
      <Route path={routePaths.partyPlayPattern} element={<PartyPlayRoute />} />
      <Route path={routePaths.studio} element={<StudioRoute />} />
      <Route path="*" element={<Navigate to={routePaths.home} replace />} />
    </Routes>
  );
};
