import popcornEmpty from "../assets/popcorn-empty.svg";
import { useAppState } from "../state/AppStateContext";
import { Button } from "./Button";
import "./ClearLinkButton.scss";

export function ClearLinkButton() {
  const { url, clearList } = useAppState();
  const canClear = url.trim().length > 0;

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
}
