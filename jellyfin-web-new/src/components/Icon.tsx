import type { SVGProps } from 'react';

export type IconName =
    | 'account'
    | 'add'
    | 'back'
    | 'check'
    | 'close'
    | 'home'
    | 'info'
    | 'list'
    | 'movie'
    | 'pause'
    | 'play'
    | 'search'
    | 'series'
    | 'settings'
    | 'volume';

const paths: Record<IconName, string> = {
    account: 'M12 12a4 4 0 1 0 0-8 4 4 0 0 0 0 8Zm7 8a7 7 0 0 0-14 0',
    add: 'M12 5v14M5 12h14',
    back: 'm15 18-6-6 6-6',
    check: 'm5 12 4 4L19 6',
    close: 'M6 6l12 12M18 6 6 18',
    home: 'm3 11 9-8 9 8v9a1 1 0 0 1-1 1h-5v-7H9v7H4a1 1 0 0 1-1-1v-9Z',
    info: 'M12 16v-4m0-4h.01M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z',
    list: 'M8 6h13M8 12h13M8 18h13M3 6h.01M3 12h.01M3 18h.01',
    movie: 'M4 5h16v14H4zM8 5v14M16 5v14M4 9h4m8 0h4M4 15h4m8 0h4',
    pause: 'M9 5v14m6-14v14',
    play: 'm8 5 11 7-11 7V5Z',
    search: 'm21 21-4.35-4.35m2.35-5.15a7.5 7.5 0 1 1-15 0 7.5 7.5 0 0 1 15 0Z',
    series: 'M4 6h16v12H4zM8 2h8M8 22h8',
    settings: 'M12 15.5a3.5 3.5 0 1 0 0-7 3.5 3.5 0 0 0 0 7Zm8-3.5 2-1-2-3-2 .5-1.5-1.5.5-2-3-2-1 2-2 .5L4 8l-2 3 2 1v2l-2 1 2 3 2-.5L7.5 19 7 21l3 2 1-2h2l1 2 3-2-.5-2 1.5-1.5 2 .5 2-3-2-1v-2Z',
    volume: 'M11 5 6 9H3v6h3l5 4V5Zm4 4a4 4 0 0 1 0 6m2.5-8.5a8 8 0 0 1 0 11'
};

interface IconProps extends SVGProps<SVGSVGElement> {
    name: IconName;
}

export function Icon({ name, ...props }: IconProps) {
    return (
        <svg
            aria-hidden='true'
            fill='none'
            viewBox='0 0 24 24'
            stroke='currentColor'
            strokeLinecap='round'
            strokeLinejoin='round'
            strokeWidth='1.75'
            {...props}
        >
            <path d={paths[name]} />
        </svg>
    );
}
