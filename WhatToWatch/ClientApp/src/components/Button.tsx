import type { ButtonHTMLAttributes, ReactNode } from "react";
import "./Button.scss";

export type ButtonVariant = "primary" | "icon";

export interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: ButtonVariant;
  children: ReactNode;
}

export function Button({ variant = "primary", className, children, ...rest }: ButtonProps) {
  const classNames = ["button", `button--${variant}`, className].filter(Boolean).join(" ");

  return (
    <button type="button" className={classNames} {...rest}>
      {children}
    </button>
  );
}
