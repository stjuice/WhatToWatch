import { useAppState } from "../state/AppStateContext";
import { Button } from "../primitives/Button";
import "./AddLinkButton.scss";

export const AddLinkButton = () => {
  const { url, loadList, isImporting } = useAppState();
  const trimmedUrl = url.trim();

  return (
    <Button
      className="add-link-button"
      onClick={() => {
        void loadList();
      }}
      disabled={!trimmedUrl || isImporting}
    >
      {isImporting ? "Додаємо…" : "Додати список"}
    </Button>
  );
};
