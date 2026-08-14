import { Button } from "../primitives/Button";
import { useAppState } from "../state/AppStateContext";

/**
 * Opens the in-app IMDb WebView import flow. Renders only in the native app,
 * where the Capacitor plugin exists.
 */
export const ImportFromImdbButton = () => {
  const { url, isImporting, importFromImdb } = useAppState();

  return (
    <Button
      onClick={() => {
        void importFromImdb();
      }}
      disabled={isImporting || !url.trim()}
    >
      {isImporting ? "Імпортуємо…" : "Імпорт з IMDb"}
    </Button>
  );
};
