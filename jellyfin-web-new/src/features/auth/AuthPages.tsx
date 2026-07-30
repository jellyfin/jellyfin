import { type FormEvent, useState } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';

import { ApiError } from '../../api';
import { useSession } from '../../auth';
import styles from './AuthPages.module.css';

export type Translate = (
    key: string,
    values?: Record<string, number | string>
) => string;

interface PageProps {
    t: Translate;
}

interface RegistrationValues {
    confirmation: string;
    password: string;
    username: string;
}

export function validateRegistration(
    values: RegistrationValues,
    minimumPasswordLength: number
): 'confirmation' | 'password' | 'username' | null {
    if (!values.username.trim()) return 'username';
    if (values.password.length < minimumPasswordLength) return 'password';
    if (values.password !== values.confirmation) return 'confirmation';
    return null;
}

function errorMessage(error: unknown, t: Translate): string {
    if (!(error instanceof ApiError)) return t('errors.unknown');
    if (error.code === 'rate_limited') {
        return t('errors.rateLimited', { seconds: error.retryAfterSeconds ?? 60 });
    }

    const keys = {
        bad_request: 'errors.badRequest',
        conflict: 'errors.conflict',
        forbidden: 'errors.forbidden',
        network: 'errors.network',
        not_found: 'errors.notFound',
        unauthorized: 'errors.invalidCredentials',
        unavailable: 'errors.unavailable',
        unknown: 'errors.unknown'
    } as const;
    return t(keys[error.code]);
}

function registrationErrorMessage(error: unknown, t: Translate): string {
    if (!(error instanceof ApiError)) return t('errors.unknown');
    if (error.code === 'rate_limited') {
        return t('auth.register.errors.rateLimited', {
            seconds: error.retryAfterSeconds ?? 60
        });
    }

    const keys: Partial<Record<number, string>> = {
        400: 'auth.register.errors.invalid',
        403: 'auth.register.errors.disabled',
        409: 'auth.register.errors.conflict'
    };
    return t(keys[error.status || 0] || 'errors.unknown');
}

function AuthFrame({
    children,
    kicker,
    title
}: {
    children: React.ReactNode;
    kicker: string;
    title: string;
}) {
    return (
        <main className={styles.page}>
            <section className={styles.panel}>
                <Link aria-label="Jellyfin" className={styles.brand} to="/login">
                    JELLYFIN<span>VIEW</span>
                </Link>
                <p className={styles.kicker}>{kicker}</p>
                <h1>{title}</h1>
                {children}
            </section>
        </main>
    );
}

export function LoginPage({ t }: PageProps) {
    const { login, publicInfo } = useSession();
    const location = useLocation();
    const navigate = useNavigate();
    const [error, setError] = useState('');
    const [pending, setPending] = useState(false);

    async function submit(event: FormEvent<HTMLFormElement>) {
        event.preventDefault();
        const data = new FormData(event.currentTarget);
        setPending(true);
        setError('');
        try {
            await login(String(data.get('username') ?? ''), String(data.get('password') ?? ''));
            const from = (location.state as { from?: { pathname?: string } } | null)?.from?.pathname;
            navigate(from || '/profiles', { replace: true });
        } catch (cause) {
            setError(errorMessage(cause, t));
        } finally {
            setPending(false);
        }
    }

    return (
        <AuthFrame kicker={publicInfo?.ServerName || t('auth.server')} title={t('auth.login.title')}>
            <form className={styles.form} onSubmit={submit}>
                <label>
                    {t('auth.username')}
                    <input autoComplete="username" name="username" required />
                </label>
                <label>
                    {t('auth.password')}
                    <input autoComplete="current-password" name="password" type="password" />
                </label>
                <p aria-live="polite" className={styles.error}>{error}</p>
                <button className={styles.primary} disabled={pending} type="submit">
                    {pending ? t('auth.login.pending') : t('auth.login.submit')}
                </button>
            </form>
            <nav aria-label={t('auth.help')} className={styles.links}>
                <Link to="/forgot-password">{t('auth.forgot.link')}</Link>
                {publicInfo?.EnablePublicUserRegistration && (
                    <Link to="/register">{t('auth.register.link')}</Link>
                )}
            </nav>
        </AuthFrame>
    );
}

export function RegisterPage({ t }: PageProps) {
    const { publicInfo, register } = useSession();
    const navigate = useNavigate();
    const [error, setError] = useState('');
    const [pending, setPending] = useState(false);
    const minimum = Math.max(1, publicInfo?.PublicUserRegistrationMinimumPasswordLength || 8);

    if (publicInfo && !publicInfo.EnablePublicUserRegistration) {
        return <AuthFrame kicker={t('auth.register.closed')} title={t('auth.register.title')}>
            <Link className={styles.primaryLink} to="/login">{t('auth.backToLogin')}</Link>
        </AuthFrame>;
    }

    async function submit(event: FormEvent<HTMLFormElement>) {
        event.preventDefault();
        const data = new FormData(event.currentTarget);
        const values = {
            confirmation: String(data.get('confirmation') ?? ''),
            password: String(data.get('password') ?? ''),
            username: String(data.get('username') ?? '')
        };
        const invalid = validateRegistration(values, minimum);
        if (invalid) {
            setError(t(`auth.register.validation.${invalid}`, { minimum }));
            return;
        }

        setPending(true);
        setError('');
        try {
            await register(values.username, values.password);
            navigate('/profiles', { replace: true });
        } catch (cause) {
            setError(registrationErrorMessage(cause, t));
        } finally {
            setPending(false);
        }
    }

    return (
        <AuthFrame kicker={t('auth.register.kicker')} title={t('auth.register.title')}>
            <form className={styles.form} onSubmit={submit}>
                <label>
                    {t('auth.username')}
                    <input autoComplete="username" name="username" required />
                </label>
                <label>
                    {t('auth.password')}
                    <input
                        aria-describedby="password-help"
                        autoComplete="new-password"
                        minLength={minimum}
                        name="password"
                        required
                        type="password"
                    />
                </label>
                <small id="password-help">{t('auth.register.minimum', { minimum })}</small>
                <label>
                    {t('auth.passwordConfirmation')}
                    <input autoComplete="new-password" name="confirmation" required type="password" />
                </label>
                <p aria-live="polite" className={styles.error}>{error}</p>
                <button className={styles.primary} disabled={pending} type="submit">
                    {pending ? t('auth.register.pending') : t('auth.register.submit')}
                </button>
            </form>
            <Link className={styles.secondaryLink} to="/login">{t('auth.backToLogin')}</Link>
        </AuthFrame>
    );
}

export function ForgotPasswordPage({ t }: PageProps) {
    const { forgotPassword } = useSession();
    const [error, setError] = useState('');
    const [message, setMessage] = useState('');
    const [pinFile, setPinFile] = useState('');

    async function submit(event: FormEvent<HTMLFormElement>) {
        event.preventDefault();
        const data = new FormData(event.currentTarget);
        setError('');
        setMessage('');
        setPinFile('');
        try {
            const result = await forgotPassword(String(data.get('username') ?? ''));
            const action = result.Action || 'ContactAdmin';
            setMessage(t(`auth.forgot.result.${action}`));
            if (result.PinFile) setPinFile(result.PinFile);
        } catch (cause) {
            setError(errorMessage(cause, t));
        }
    }

    return (
        <AuthFrame kicker={t('auth.forgot.kicker')} title={t('auth.forgot.title')}>
            <form className={styles.form} onSubmit={submit}>
                <label>
                    {t('auth.username')}
                    <input autoComplete="username" name="username" required />
                </label>
                <p aria-live="polite" className={styles.error}>{error}</p>
                <button className={styles.primary} type="submit">{t('auth.forgot.submit')}</button>
            </form>
            {message && <div aria-live="polite" className={styles.notice}>
                <p>{message}</p>
                {pinFile && <code>{pinFile}</code>}
            </div>}
            <Link className={styles.secondaryLink} to="/login">{t('auth.backToLogin')}</Link>
        </AuthFrame>
    );
}

export function SetupRequiredPage({ t }: PageProps) {
    return (
        <AuthFrame kicker={t('setup.kicker')} title={t('setup.title')}>
            <p className={styles.copy}>{t('setup.description')}</p>
            <p className={styles.notice}>{t('setup.legacyInstruction')}</p>
        </AuthFrame>
    );
}

export function ServerUnavailablePage({ t }: PageProps) {
    return (
        <AuthFrame kicker={t('errors.network')} title={t('errors.unavailable')}>
            <button className={styles.primary} onClick={() => window.location.reload()} type="button">
                {t('actions.retry')}
            </button>
        </AuthFrame>
    );
}
