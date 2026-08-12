import { useState } from "react";
import popcornPivot from "../assets/popcorn-pivot.svg";
import { getRandomMovie } from "../api/moviesApi";
import { Button } from "../primitives/Button";
import { useAppState } from "../state/AppStateContext";
import "./RerollButton.scss";

export const RerollButton = () => {
  const { watchlistId, setMovie } = useAppState();
  const [isLoading, setIsLoading] = useState(false);

  const handleClick = async () => {
    if (!watchlistId || isLoading) {
      return;
    }

    setIsLoading(true);
    try {
      const movie = await getRandomMovie(watchlistId);
      setMovie(movie);
    } catch {
      // Keep the current movie if re-roll fails.
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <Button
      variant="icon"
      className="reroll-button"
      onClick={() => {
        void handleClick();
      }}
      disabled={!watchlistId || isLoading}
      aria-label="Інший випадковий фільм"
    >
      <img src={popcornPivot} alt="" draggable={false} />
    </Button>
  );
};
