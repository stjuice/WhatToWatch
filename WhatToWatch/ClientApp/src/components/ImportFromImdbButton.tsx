import { Button } from "../primitives/Button";

type Props = {
  disabled?: boolean;
  onImport: () => void;
};

/**
 * Opens the in-app IMDb WebView import flow. Used from Studio on Android.
 */
export const ImportFromImdbButton = ({ disabled = false, onImport }: Props) => {
  return (
    <Button onClick={onImport} disabled={disabled}>
      Імпорт з IMDb
    </Button>
  );
};
