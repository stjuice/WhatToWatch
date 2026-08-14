import popcornPivot from "../assets/popcorn-pivot.svg";
import { text } from "../i18n/text";
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
      aria-label={text("movie.rerollAria")}
    >
      <img src={popcornPivot} alt="" draggable={false} />
    </Button>
  );
};
