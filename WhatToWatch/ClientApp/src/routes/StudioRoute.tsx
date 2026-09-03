import { Navigate } from "react-router-dom";
import { StudioPage } from "../pages/StudioPage";
import { isAndroidApk } from "../platform/isAndroidApk";
import { routePaths } from "./routePaths";

export const StudioRoute = () => {
  if (!isAndroidApk()) {
    return <Navigate to={routePaths.home} replace />;
  }

  return <StudioPage />;
};
