import { useState } from "react";
import { Button } from "../primitives/Button";
import { ImdbImportService } from "../services/imdbImportService";
import { useAppState } from "../state/AppStateContext";

/**
 * Opens the in-app IMDb WebView import flow. Renders only in the native app,
 * where the Capacitor plugin exists.
 */
export const ImportFromImdbButton = () => {
  const { url } = useAppState();
  const [isImporting, setIsImporting] = useState(false);

  if (!ImdbImportService.isAvailable()) {
    return null;
  }

  const handleClick = async () => {
    setIsImporting(true);

    try {
      await ImdbImportService.importFromImdb(url);
    } catch (error) {
      console.error("[ImdbImport] import failed", error);
    } finally {
      setIsImporting(false);
    }
  };

  return (
    <Button
      onClick={() => {
        void handleClick();
      }}
      disabled={isImporting || !url.trim()}
    >
      {isImporting ? "Імпортуємо…" : "Імпорт з IMDb"}
    </Button>
  );
};
