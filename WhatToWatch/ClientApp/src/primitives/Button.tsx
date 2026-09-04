import type {
  AnchorHTMLAttributes,
  ButtonHTMLAttributes,
  ReactNode,
} from "react";
import { Link } from "react-router-dom";
import type { LinkProps } from "react-router-dom";
import "./Button.scss";

export type ButtonVariant = "primary" | "icon" | "plain";

interface CommonProps {
  variant?: ButtonVariant;
  className?: string;
  children: ReactNode;
}

type RouterLinkButtonProps = CommonProps &
  { to: string; href?: never } &
  Omit<LinkProps, "to" | "href" | "className" | "children">;

type AnchorButtonProps = CommonProps &
  { href: string; to?: never } &
  Omit<AnchorHTMLAttributes<HTMLAnchorElement>, "href" | "className" | "children">;

type NativeButtonProps = CommonProps &
  { to?: never; href?: never } &
  Omit<ButtonHTMLAttributes<HTMLButtonElement>, "className" | "children">;

export type ButtonProps = RouterLinkButtonProps | AnchorButtonProps | NativeButtonProps;

export const Button = ({
  variant = "primary",
  className,
  children,
  ...elementProps
}: ButtonProps) => {
  const classNames = ["button", `button--${variant}`, className].filter(Boolean).join(" ");

  if ("to" in elementProps && elementProps.to !== undefined) {
    return (
      <Link className={classNames} {...elementProps}>
        {children}
      </Link>
    );
  }

  if ("href" in elementProps && elementProps.href !== undefined) {
    return (
      <a className={classNames} {...elementProps}>
        {children}
      </a>
    );
  }

  const { type = "button", ...buttonProps } = elementProps;
  return (
    <button type={type} className={classNames} {...buttonProps}>
      {children}
    </button>
  );
};
