import { useNavigate } from "react-router-dom";
import popcornFull from "../assets/popcorn-full.svg";
import { Button } from "../primitives/Button";
import { routePaths } from "../routes/routePaths";
import { useAppState } from "../state/AppStateContext";
import "./RandomButton.scss";

export const RandomButton = () => {
  const { watchlistId, isPicking, pickRandomMovie } = useAppState();
  const navigate = useNavigate();

  const handleClick = async () => {
    const picked = await pickRandomMovie();

    if (picked) {
      navigate(routePaths.movie);
    }
  };

  return (
    <Button
      variant="icon"
      className="random-button"
      onClick={() => {
        void handleClick();
      }}
      disabled={!watchlistId || isPicking}
      aria-busy={isPicking}
      aria-label="Отримати випадковий фільм"
    >
      <img src={popcornFull} alt="" draggable={false} />
    </Button>
  );
};
