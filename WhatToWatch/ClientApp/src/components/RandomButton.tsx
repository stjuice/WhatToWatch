import popcornPivot from "../assets/popcorn-pivot.svg";
import { ImageButton } from "./ImageButton";
import type { ImageButtonProps } from "./ImageButton";

export type RandomButtonProps = ImageButtonProps;

export const RandomButton = (props: RandomButtonProps) => (
  <ImageButton {...props} art={popcornPivot} />
);
