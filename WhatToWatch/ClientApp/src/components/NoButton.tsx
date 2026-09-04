import no from "../assets/no.svg";
import { ImageButton } from "./ImageButton";
import type { ImageButtonProps } from "./ImageButton";

export type NoButtonProps = ImageButtonProps;

export const NoButton = (props: NoButtonProps) => <ImageButton {...props} art={no} />;
