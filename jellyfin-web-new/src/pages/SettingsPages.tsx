import { useMutation, useQueryClient } from '@tanstack/react-query';
import { type FormEvent, type PropsWithChildren, useState } from 'react';
import { Link } from 'react-router-dom';

import {
    ApiError,
    profileQueryKeys,
    profilesApi,
    type PlaybackPreferences,
    type ProfileSettings
} from '../api';
import { useSession } from '../auth';
import { Button } from '../components/Button';
import { useProfile } from '../features/profiles';
import { useI18n } from '../i18n';
import { useOnlineStatus } from '../app/useOnlineStatus';
import styles from './SettingsPages.module.css';

function SettingsFrame({ children, title }: PropsWithChildren<{ title: string }>) {
    const { t } = useI18n();
    return (
        <div className={styles.page}>
            <aside className={styles.sidebar}>
                <h1>{title}</h1>
                <nav aria-label={t('settings')}>
                    <Link to='/settings/profile'>{t('profile')}</Link>
                    <Link to='/settings/playback'>{t('playback')}</Link>
                    <Link to='/account'>{t('account')}</Link>
                    <Link to='/history'>{t('history')}</Link>
                    <Link to='/profiles/manage'>{t('profiles.manage')}</Link>
                </nav>
            </aside>
            <section className={styles.content}>{children}</section>
        </div>
    );
}

function Toggle({ defaultChecked, label, name }: { defaultChecked: boolean; label: string; name: string }) {
    return (
        <label className={styles.toggle}>
            <span>{label}</span>
            <input defaultChecked={defaultChecked} name={name} type='checkbox' />
        </label>
    );
}

function useSaveProfile() {
    const { client, session } = useSession();
    const { activeProfile } = useProfile();
    const { locale } = useI18n();
    const queryClient = useQueryClient();
    const [ message, setMessage ] = useState('');
    const mutation = useMutation({
        mutationFn: (update: { PlaybackPreferences?: PlaybackPreferences; Settings?: ProfileSettings }) => {
            if (!activeProfile) throw new Error('No active profile');
            return profilesApi.update(client, activeProfile.Id, update);
        },
        onError: error => {
            setMessage(error instanceof ApiError && error.status === 409
                ? locale === 'fr'
                    ? 'Arrêtez la lecture avant de modifier ce profil.'
                    : 'Stop playback before changing this profile.'
                : locale === 'fr'
                    ? 'Impossible d’enregistrer les réglages.'
                    : 'The settings could not be saved.');
        },
        onSuccess: async () => {
            setMessage(locale === 'fr' ? 'Réglages enregistrés.' : 'Settings saved.');
            if (session) {
                await Promise.all([
                    queryClient.invalidateQueries({ queryKey: profileQueryKeys.active(session.userId) }),
                    queryClient.invalidateQueries({ queryKey: profileQueryKeys.profiles(session.userId) })
                ]);
            }
        }
    });

    return { message, mutation };
}

export function ProfileSettingsPage() {
    const { activeProfile } = useProfile();
    const { locale, t } = useI18n();
    const online = useOnlineStatus();
    const { message, mutation } = useSaveProfile();

    if (!activeProfile) {
        return <SettingsFrame title={t('profile')}><p>{t('profiles.choose')}</p></SettingsFrame>;
    }

    const submit = (event: FormEvent<HTMLFormElement>) => {
        event.preventDefault();
        const data = new FormData(event.currentTarget);
        mutation.mutate({
            Settings: {
                AutoplayDelaySeconds: Number(data.get('autoplayDelay')),
                AutoplayEnabled: data.get('autoplay') === 'on',
                SkipIntroEnabled: data.get('skipIntro') === 'on',
                SkipRecapEnabled: data.get('skipRecap') === 'on'
            }
        });
    };

    return (
        <SettingsFrame title={t('profile')}>
            <p className={styles.eyebrow}>{activeProfile.Name}</p>
            <h2>{locale === 'fr' ? 'Expérience de lecture' : 'Viewing experience'}</h2>
            <form className={styles.form} onSubmit={submit}>
                <Toggle defaultChecked={activeProfile.Settings.AutoplayEnabled} label={locale === 'fr' ? 'Lire automatiquement l’épisode suivant' : 'Autoplay next episode'} name='autoplay' />
                <label>
                    <span>{locale === 'fr' ? 'Délai avant l’épisode suivant' : 'Next episode delay'}</span>
                    <select defaultValue={activeProfile.Settings.AutoplayDelaySeconds} name='autoplayDelay'>
                        {[ 5, 8, 10, 15, 20 ].map(seconds => <option key={seconds} value={seconds}>{seconds} s</option>)}
                    </select>
                </label>
                <Toggle defaultChecked={activeProfile.Settings.SkipIntroEnabled} label={t('skipIntro')} name='skipIntro' />
                <Toggle defaultChecked={activeProfile.Settings.SkipRecapEnabled} label={t('skipRecap')} name='skipRecap' />
                <Button disabled={!online || mutation.isPending} tone='primary' type='submit'>{t('actions.save')}</Button>
                <p aria-live='polite' className={styles.message}>{message}</p>
            </form>
        </SettingsFrame>
    );
}

export function PlaybackSettingsPage() {
    const { activeProfile } = useProfile();
    const { locale, t } = useI18n();
    const online = useOnlineStatus();
    const { message, mutation } = useSaveProfile();

    if (!activeProfile) {
        return <SettingsFrame title={t('playback')}><p>{t('profiles.choose')}</p></SettingsFrame>;
    }

    const preferences = activeProfile.PlaybackPreferences;
    const submit = (event: FormEvent<HTMLFormElement>) => {
        event.preventDefault();
        const data = new FormData(event.currentTarget);
        const bitrate = Number(data.get('bitrate'));
        mutation.mutate({
            PlaybackPreferences: {
                AllowAudioTranscoding: data.get('audioTranscode') === 'on',
                AllowContainerRemuxing: data.get('remux') === 'on',
                AllowVideoTranscoding: data.get('videoTranscode') === 'on',
                AudioDescriptionEnabled: data.get('audioDescription') === 'on',
                ClosedCaptionsEnabled: data.get('closedCaptions') === 'on',
                MaxStreamingBitrate: bitrate > 0 ? bitrate : null,
                PreferDirectPlay: data.get('directPlay') === 'on',
                PreferHardwareTranscoding: preferences.PreferHardwareTranscoding,
                PreferredAudioLanguage: String(data.get('audioLanguage') || '') || null,
                PreferredSubtitleLanguage: String(data.get('subtitleLanguage') || '') || null,
                SkipCreditsEnabled: data.get('skipCredits') === 'on',
                SubtitlesEnabled: data.get('subtitles') === 'on'
            }
        });
    };

    return (
        <SettingsFrame title={t('playback')}>
            <h2>{locale === 'fr' ? 'Qualité et pistes' : 'Quality and tracks'}</h2>
            <form className={styles.form} onSubmit={submit}>
                <label>
                    <span>{locale === 'fr' ? 'Débit maximal' : 'Maximum bitrate'}</span>
                    <select defaultValue={preferences.MaxStreamingBitrate ?? 0} name='bitrate'>
                        <option value='0'>{locale === 'fr' ? 'Automatique' : 'Automatic'}</option>
                        <option value='4000000'>4 Mbps</option>
                        <option value='8000000'>8 Mbps</option>
                        <option value='20000000'>20 Mbps</option>
                        <option value='40000000'>40 Mbps</option>
                        <option value='80000000'>80 Mbps</option>
                    </select>
                </label>
                <label>
                    <span>{locale === 'fr' ? 'Langue audio préférée' : 'Preferred audio language'}</span>
                    <select defaultValue={preferences.PreferredAudioLanguage ?? ''} name='audioLanguage'>
                        <option value=''>{locale === 'fr' ? 'Automatique' : 'Automatic'}</option>
                        <option value='fra'>Français</option>
                        <option value='eng'>English</option>
                    </select>
                </label>
                <label>
                    <span>{locale === 'fr' ? 'Langue de sous-titres' : 'Subtitle language'}</span>
                    <select defaultValue={preferences.PreferredSubtitleLanguage ?? ''} name='subtitleLanguage'>
                        <option value=''>{locale === 'fr' ? 'Automatique' : 'Automatic'}</option>
                        <option value='fra'>Français</option>
                        <option value='eng'>English</option>
                    </select>
                </label>
                <Toggle defaultChecked={preferences.PreferDirectPlay} label='Direct Play' name='directPlay' />
                <Toggle defaultChecked={preferences.AllowContainerRemuxing} label='Remux' name='remux' />
                <Toggle defaultChecked={preferences.AllowVideoTranscoding} label={locale === 'fr' ? 'Transcodage vidéo' : 'Video transcoding'} name='videoTranscode' />
                <Toggle defaultChecked={preferences.AllowAudioTranscoding} label={locale === 'fr' ? 'Transcodage audio' : 'Audio transcoding'} name='audioTranscode' />
                <p className={styles.message}>
                    {locale === 'fr'
                        ? 'L’accélération matérielle se configure dans l’administration du serveur.'
                        : 'Hardware acceleration is configured in the server administration.'}
                </p>
                <Toggle defaultChecked={preferences.SubtitlesEnabled} label={t('subtitles')} name='subtitles' />
                <Toggle defaultChecked={preferences.AudioDescriptionEnabled} label={locale === 'fr' ? 'Audiodescription' : 'Audio description'} name='audioDescription' />
                <Toggle defaultChecked={preferences.ClosedCaptionsEnabled} label={locale === 'fr' ? 'Sous-titres sourds et malentendants' : 'Closed captions'} name='closedCaptions' />
                <Toggle defaultChecked={preferences.SkipCreditsEnabled} label={t('skipCredits')} name='skipCredits' />
                <Button disabled={!online || mutation.isPending} tone='primary' type='submit'>{t('actions.save')}</Button>
                <p aria-live='polite' className={styles.message}>{message}</p>
            </form>
        </SettingsFrame>
    );
}

export function AccountPage() {
    const { logout, publicInfo } = useSession();
    const { locale, setLocale, t } = useI18n();

    return (
        <SettingsFrame title={t('account')}>
            <p className={styles.eyebrow}>{publicInfo?.ServerName}</p>
            <h2>{locale === 'fr' ? 'Interface' : 'Interface'}</h2>
            <div className={styles.form}>
                <label>
                    <span>{t('language')}</span>
                    <select onChange={event => setLocale(event.target.value === 'fr' ? 'fr' : 'en')} value={locale}>
                        <option value='fr'>Français</option>
                        <option value='en'>English</option>
                    </select>
                </label>
                <Button onClick={() => void logout()} tone='danger'>{t('logout')}</Button>
            </div>
        </SettingsFrame>
    );
}
