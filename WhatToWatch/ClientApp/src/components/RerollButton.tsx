import popcornPivot from "../assets/popcorn-pivot.svg";
import { Button } from "../primitives/Button";
import { useAppState } from "../state/AppStateContext";
import "./RerollButton.scss";

export const RerollButton = () => {
  const { isPicking, pickRandomMovie } = useAppState();

  return (
    <Button
      variant="icon"
      className="reroll-button"
      onClick={() => {
        void pickRandomMovie();
      }}
      disabled={isPicking}
      aria-busy={isPicking}
      aria-label="Інший випадковий фільм"
    >
      <img src={popcornPivot} alt="" draggable={false} />
    </Button>
  );
};
