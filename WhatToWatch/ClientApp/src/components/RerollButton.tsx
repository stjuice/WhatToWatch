import { text } from "../i18n/text";
import { useAppState } from "../state/AppStateContext";
import { RandomButton } from "./RandomButton";
import "./RerollButton.scss";

export const RerollButton = () => {
  const { isPicking, pickRandomMovie } = useAppState();

  return (
    <RandomButton
      size="small"
      label={text("movie.rerollAria")}
      className="reroll-button"
      onClick={() => {
        void pickRandomMovie();
      }}
      disabled={isPicking}
      aria-busy={isPicking}
    />
  );
};
