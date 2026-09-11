import party from "../assets/tinder.svg";
import { ImageButton } from "./ImageButton";
import type { ImageButtonProps } from "./ImageButton";
import "./PartyButton.scss";

export type PartyButtonProps = ImageButtonProps;

export const PartyButton = ({ className, ...props }: PartyButtonProps) => (
  <ImageButton
    {...props}
    className={["party-button", className].filter(Boolean).join(" ")}
    art={party}
  />
);
