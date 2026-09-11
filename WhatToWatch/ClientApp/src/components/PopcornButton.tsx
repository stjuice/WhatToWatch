import type { MouseEventHandler } from "react";
import { Button } from "../primitives/Button";
import { artworkSizeClass, type ArtworkSize } from "./artworkSize";
import "./ArtworkSize.scss";
import "./PopcornButton.scss";

export interface PopcornButtonProps {
  art: string;
  label: string;
  busy?: boolean;
  disabled?: boolean;
  size: ArtworkSize;
  onClick: MouseEventHandler<HTMLButtonElement>;
  className?: string;
}

export const PopcornButton = ({
  art,
  label,
  busy = false,
  disabled = false,
  size,
  onClick,
  className,
}: PopcornButtonProps) => (
  <Button
    variant="icon"
    className={["popcorn-button", artworkSizeClass(size), className]
      .filter(Boolean)
      .join(" ")}
    aria-label={label}
    aria-busy={busy}
    disabled={disabled}
    onClick={onClick}
  >
    <img src={art} alt="" draggable={false} />
  </Button>
);
