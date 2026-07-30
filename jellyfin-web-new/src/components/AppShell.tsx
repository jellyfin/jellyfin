import { NavLink, Outlet } from 'react-router-dom';

import { Brand } from './Brand';
import { Icon, type IconName } from './Icon';
import { useProfile } from '../features/profiles';
import { useI18n, type MessageKey } from '../i18n';
import styles from './AppShell.module.css';

interface NavigationItem {
    icon: IconName;
    label: MessageKey;
    to: string;
}

const primaryNavigation: NavigationItem[] = [
    { icon: 'home', label: 'home', to: '/home' },
    { icon: 'movie', label: 'browseMovies', to: '/browse/movies' },
    { icon: 'series', label: 'browseSeries', to: '/browse/series' },
    { icon: 'add', label: 'newAndPopular', to: '/new-and-popular' },
    { icon: 'list', label: 'myList', to: '/my-list' }
];

const mobileNavigation = primaryNavigation
    .filter(({ to }) => to !== '/new-and-popular')
    .concat({ icon: 'search', label: 'search', to: '/search' });

function NavigationLink({ icon, label, to }: NavigationItem) {
    const { t } = useI18n();

    return (
        <NavLink
            className={({ isActive }) => isActive ? styles.activeLink : styles.link}
            end={to === '/home'}
            to={to}
        >
            <Icon name={icon} />
            <span>{t(label)}</span>
        </NavLink>
    );
}

export function AppShell() {
    const { t } = useI18n();
    const { activeProfile } = useProfile();

    return (
        <div className={styles.shell}>
            <header className={styles.header}>
                <NavLink className={styles.brandLink ?? ''} to='/home'><Brand /></NavLink>
                <nav className={styles.desktopNav} aria-label='Navigation principale'>
                    {primaryNavigation.map(item => <NavigationLink {...item} key={item.to} />)}
                </nav>
                <div className={styles.actions}>
                    <NavLink className={styles.iconLink ?? ''} to='/search' aria-label={t('search')}>
                        <Icon name='search' />
                    </NavLink>
                    <NavLink
                        className={styles.profileLink ?? ''}
                        to='/settings/profile'
                        aria-label={`${t('profile')}: ${activeProfile?.Name ?? ''}`}
                    >
                        <span aria-hidden='true'>{activeProfile?.Name.trim().slice(0, 1).toLocaleUpperCase() ?? 'P'}</span>
                    </NavLink>
                </div>
            </header>
            <main className={styles.main}>
                <Outlet />
            </main>
            <nav className={styles.mobileNav} aria-label='Navigation mobile'>
                {mobileNavigation.map(item => <NavigationLink {...item} key={item.to} />)}
            </nav>
        </div>
    );
}
