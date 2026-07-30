import { useRef } from 'react';

import { MediaCard } from './MediaCard';
import type { MediaRail as MediaRailValue } from '../types/media';
import styles from './MediaRail.module.css';

interface MediaRailProps {
    rail: MediaRailValue;
    variant?: 'landscape' | 'poster';
}

export function MediaRail({ rail, variant = 'poster' }: MediaRailProps) {
    const listRef = useRef<HTMLDivElement>(null);
    const scroll = (direction: number) => {
        listRef.current?.scrollBy({ left: direction * listRef.current.clientWidth * 0.85, behavior: 'smooth' });
    };

    if (!rail.items.length) {
        return null;
    }

    return (
        <section className={styles.rail} aria-labelledby={`rail-${rail.id}`}>
            <div className={styles.heading}>
                <h2 id={`rail-${rail.id}`}>{rail.title}</h2>
                <div className={styles.controls}>
                    <button aria-label='Précédent' onClick={() => scroll(-1)}>←</button>
                    <button aria-label='Suivant' onClick={() => scroll(1)}>→</button>
                </div>
            </div>
            <div className={`${styles.items} ${styles[variant]}`} ref={listRef}>
                {rail.items.map((item, index) => (
                    <MediaCard
                        item={item}
                        key={item.id}
                        {...(rail.numbered ? { number: index + 1 } : {})}
                        variant={variant}
                    />
                ))}
            </div>
        </section>
    );
}
