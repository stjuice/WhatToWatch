import { useState } from "react";
import { useNavigate } from "react-router-dom";
import popcornFull from "../assets/popcorn-full.svg";
import { getRandomMovie } from "../api/moviesApi";
import { routes } from "../routes";
import { useAppState } from "../state/AppStateContext";
import { Button } from "./Button";
import "./RandomButton.scss";

export function RandomButton() {
  const { watchlistId, setMovie } = useAppState();
  const navigate = useNavigate();
  const [isLoading, setIsLoading] = useState(false);

  async function handleClick() {
    if (!watchlistId || isLoading) {
      return;
    }

    setIsLoading(true);
    try {
      const movie = await getRandomMovie(watchlistId);
      setMovie(movie);
      navigate(routes.movie);
    } catch {
      setMovie(null);
    } finally {
      setIsLoading(false);
    }
  }

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
}
