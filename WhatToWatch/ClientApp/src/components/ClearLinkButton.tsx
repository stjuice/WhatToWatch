import popcornEmpty from "../assets/popcorn-empty.svg";
import { Button } from "../primitives/Button";
import { useAppState } from "../state/AppStateContext";
import "./ClearLinkButton.scss";

export const ClearLinkButton = () => {
  const { url, clearList, isImporting } = useAppState();
  const canClear = url.trim().length > 0 && !isImporting;

  return (
    <Button
      variant="icon"
      className="clear-link-button"
      onClick={clearList}
      disabled={!canClear}
      aria-label="Очистити посилання"
    >
      <img src={popcornEmpty} alt="" draggable={false} />
    </Button>
  );
};
