import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { getRandomMovie } from "../api/moviesApi";
import popcornFull from "../assets/popcorn-full.svg";
import { Button } from "../primitives/Button";
import { routePaths } from "../routes/routePaths";
import { useAppState } from "../state/AppStateContext";
import "./RandomButton.scss";

export const RandomButton = () => {
  const { watchlistId, setMovie } = useAppState();
  const navigate = useNavigate();
  const [isLoading, setIsLoading] = useState(false);

  const handleClick = async () => {
    if (!watchlistId || isLoading) {
      return;
    }

    setIsLoading(true);
    try {
      const movie = await getRandomMovie(watchlistId);
      setMovie(movie);
      navigate(routePaths.movie);
    } catch {
      setMovie(null);
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <Button
      variant="icon"
      className="random-button"
      onClick={() => {
        void handleClick();
      }}
      disabled={!watchlistId || isLoading}
      aria-label="Отримати випадковий фільм"
    >
      <img src={popcornFull} alt="" draggable={false} />
    </Button>
  );
};
