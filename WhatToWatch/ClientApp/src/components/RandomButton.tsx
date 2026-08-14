import { useNavigate } from "react-router-dom";
import popcornFull from "../assets/popcorn-full.svg";
import { text } from "../i18n/text";
import { Button } from "../primitives/Button";
import { routePaths } from "../routes/routePaths";
import { useAppState } from "../state/AppStateContext";
import "./RandomButton.scss";

type Props = {
  watchlistId?: string | null;
};

export const RandomButton = ({ watchlistId }: Props) => {
  const { isPicking, pickRandomMovie } = useAppState();
  const navigate = useNavigate();

  const handleClick = async () => {
    const picked = await pickRandomMovie(watchlistId);
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
      disabled={isPicking}
      aria-busy={isPicking}
      aria-label={text("movie.randomAria")}
    >
      <img src={popcornFull} alt="" draggable={false} />
    </Button>
  );
};
