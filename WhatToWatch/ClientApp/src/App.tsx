import { AppLayout } from "./layout/AppLayout";
import { AppStateProvider } from "./state/AppStateContext";
import "./App.scss";

export const App = () => {
  return (
    <AppStateProvider>
      <AppLayout />
    </AppStateProvider>
  );
};
