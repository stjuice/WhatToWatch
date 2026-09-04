import type { MouseEventHandler } from "react";
import { Button } from "../primitives/Button";
import "./PopcornButton.scss";

export interface PopcornButtonProps {
  art: string;
  label: string;
  busy?: boolean;
  disabled?: boolean;
  onClick: MouseEventHandler<HTMLButtonElement>;
  className?: string;
}

export const PopcornButton = ({
  art,
  label,
  busy = false,
  disabled = false,
  onClick,
  className,
}: PopcornButtonProps) => (
  <Button
    variant="icon"
    className={["popcorn-button", className].filter(Boolean).join(" ")}
    aria-label={label}
    aria-busy={busy}
    disabled={disabled}
    onClick={onClick}
  >
    <img src={art} alt="" draggable={false} />
  </Button>
);
