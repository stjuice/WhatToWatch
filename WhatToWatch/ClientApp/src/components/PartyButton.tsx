import party from "../assets/tinder.svg";
import { ImageButton } from "./ImageButton";
import type { ImageButtonProps } from "./ImageButton";

export type PartyButtonProps = ImageButtonProps;

export const PartyButton = (props: PartyButtonProps) => (
  <ImageButton {...props} art={party} />
);
