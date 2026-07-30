export type ApiErrorCode =
    | 'bad_request'
    | 'unauthorized'
    | 'forbidden'
    | 'not_found'
    | 'conflict'
    | 'rate_limited'
    | 'unavailable'
    | 'network'
    | 'unknown';

export class ApiError extends Error {
    public constructor(
        public readonly code: ApiErrorCode,
        public readonly status?: number,
        public readonly retryAfterSeconds?: number,
        public readonly detail?: string
    ) {
        super(detail || code);
        this.name = 'ApiError';
    }
}

interface HttpFailure {
    message?: string;
    response?: {
        data?: unknown;
        headers?: Record<string, unknown>;
        status?: number;
    };
}

const codes: Partial<Record<number, ApiErrorCode>> = {
    400: 'bad_request',
    401: 'unauthorized',
    403: 'forbidden',
    404: 'not_found',
    409: 'conflict',
    429: 'rate_limited',
    503: 'unavailable'
};

function detailOf(data: unknown): string | undefined {
    if (typeof data === 'string') return data;
    if (!data || typeof data !== 'object') return undefined;

    const problem = data as { Detail?: unknown; detail?: unknown; Message?: unknown; message?: unknown };
    const detail = problem.Detail ?? problem.detail ?? problem.Message ?? problem.message;
    return typeof detail === 'string' ? detail : undefined;
}

export function parseRetryAfter(value: unknown, now = Date.now()): number | undefined {
    if (Array.isArray(value)) value = value[0];
    if (typeof value === 'number' && Number.isFinite(value)) return Math.max(0, Math.ceil(value));
    if (typeof value !== 'string' || !value.trim()) return undefined;

    const seconds = Number(value);
    if (Number.isFinite(seconds)) return Math.max(0, Math.ceil(seconds));

    const date = Date.parse(value);
    return Number.isNaN(date) ? undefined : Math.max(0, Math.ceil((date - now) / 1000));
}

export function toApiError(error: unknown): ApiError {
    if (error instanceof ApiError) return error;

    const failure = error as HttpFailure;
    const status = failure?.response?.status;
    if (!status) {
        return new ApiError('network', undefined, undefined, failure?.message);
    }

    const headers = failure.response?.headers;
    const retryAfter = parseRetryAfter(headers?.['retry-after'] ?? headers?.RetryAfter);
    return new ApiError(
        codes[status] ?? 'unknown',
        status,
        retryAfter,
        detailOf(failure.response?.data) ?? failure.message
    );
}
