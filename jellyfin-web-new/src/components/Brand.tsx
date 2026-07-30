import styles from './Brand.module.css';

export function Brand() {
    return (
        <span className={styles.brand} aria-label='Jellyfin'>
            <svg aria-hidden='true' viewBox='0 0 28 28'>
                <path d='M14 2 26 24H2L14 2Zm0 7.2L7.9 21h12.2L14 9.2Z' fill='currentColor' fillRule='evenodd' />
            </svg>
            <span>Jellyfin</span>
        </span>
    );
}
