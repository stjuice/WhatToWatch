import yes from "../assets/yes.svg";
import { ImageButton } from "./ImageButton";
import type { ImageButtonProps } from "./ImageButton";

export type YesButtonProps = ImageButtonProps;

export const YesButton = (props: YesButtonProps) => <ImageButton {...props} art={yes} />;
