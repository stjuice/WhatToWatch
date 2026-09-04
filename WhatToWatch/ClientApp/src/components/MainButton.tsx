import popcornFull from "../assets/popcorn-full.svg";
import { ImageButton } from "./ImageButton";
import type { ImageButtonProps } from "./ImageButton";

export type MainButtonProps = ImageButtonProps;

export const MainButton = (props: MainButtonProps) => (
  <ImageButton {...props} art={popcornFull} />
);
