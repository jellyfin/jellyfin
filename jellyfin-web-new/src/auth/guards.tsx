import type { ReactNode } from 'react';
import { Navigate, useLocation } from 'react-router-dom';

import { useSession } from './SessionProvider';

interface RequireSessionProps {
    children: ReactNode;
    loading?: ReactNode;
    setupRequired?: ReactNode;
    unavailable?: ReactNode;
}

export function RequireSession({
    children,
    loading = null,
    setupRequired = null,
    unavailable = null
}: RequireSessionProps) {
    const location = useLocation();
    const { status } = useSession();

    if (status === 'loading') return loading;
    if (status === 'setup-required') return setupRequired;
    if (status === 'unavailable') return unavailable;
    if (status !== 'authenticated') {
        return <Navigate replace state={{ from: location }} to="/login" />;
    }

    return children;
}

export function AnonymousOnly({ children }: { children: ReactNode }) {
    const { status } = useSession();
    return status === 'authenticated'
        ? <Navigate replace to="/profiles" />
        : children;
}
