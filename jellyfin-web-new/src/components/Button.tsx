import { type ButtonHTMLAttributes, type ReactNode } from 'react';

import styles from './Button.module.css';

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
    icon?: ReactNode;
    tone?: 'primary' | 'secondary' | 'quiet' | 'danger';
}

export function Button({ children, className = '', icon, tone = 'secondary', type = 'button', ...props }: ButtonProps) {
    return (
        <button className={`${styles.button} ${styles[tone]} ${className}`} type={type} {...props}>
            {icon}
            {children}
        </button>
    );
}
