import { Jellyfin, type Api } from '@jellyfin/sdk';
import { AuthenticationApi } from '@jellyfin/sdk/lib/generated-client/api/authentication-api';
import type {
    AuthenticationResult,
    ForgotPasswordResult,
    UserDto
} from '@jellyfin/sdk/lib/generated-client';

import { getDefaultApiBaseUrl } from './baseUrl';
import { toApiError } from './errors';

export const CLIENT_NAME = 'Jellyfin Web New';
export const CLIENT_VERSION = '0.1.0';

export interface PublicSystemInfo {
    EnablePublicUserRegistration: boolean;
    Id?: string | null;
    LocalAddress?: string | null;
    ProductName?: string | null;
    PublicUserRegistrationMinimumPasswordLength: number;
    ServerName?: string | null;
    StartupWizardCompleted?: boolean | null;
    Version?: string | null;
}

export interface RequestOptions {
    body?: unknown;
    query?: Record<string, boolean | number | string | null | undefined>;
    signal?: AbortSignal;
}

function authenticationApi(api: Api) {
    return new AuthenticationApi(api.configuration, undefined, api.axiosInstance);
}

function deviceName(): string {
    const platform = navigator.platform;
    return platform ? `Web (${platform})` : 'Web browser';
}

export class JellyfinClient {
    public readonly sdkApi: Api;

    public constructor(
        public readonly baseUrl: string,
        deviceId: string,
        accessToken = ''
    ) {
        const jellyfin = new Jellyfin({
            clientInfo: { name: CLIENT_NAME, version: CLIENT_VERSION },
            deviceInfo: { id: deviceId, name: deviceName() }
        });
        this.sdkApi = jellyfin.createApi(baseUrl, accessToken);
    }

    public setAccessToken(accessToken = ''): void {
        this.sdkApi.update({ accessToken });
    }

    public async getPublicSystemInfo(): Promise<PublicSystemInfo> {
        return this.request<PublicSystemInfo>('GET', 'System/Info/Public');
    }

    public async authenticate(username: string, password: string): Promise<AuthenticationResult> {
        try {
            const response = await authenticationApi(this.sdkApi).authenticateUserByName({
                authenticateUserByName: {
                    Pw: password,
                    Username: username.trim()
                }
            });
            return response.data;
        } catch (error) {
            throw toApiError(error);
        }
    }

    public async register(username: string, password: string): Promise<AuthenticationResult> {
        return this.request<AuthenticationResult>('POST', 'Users/Register', {
            body: { Name: username.trim(), Password: password }
        });
    }

    public async forgotPassword(username: string): Promise<ForgotPasswordResult> {
        try {
            const response = await authenticationApi(this.sdkApi).forgotPassword({
                forgotPasswordDto: { EnteredUsername: username.trim() }
            });
            return response.data;
        } catch (error) {
            throw toApiError(error);
        }
    }

    public getCurrentUser(): Promise<UserDto> {
        return this.request<UserDto>('GET', 'Users/Me');
    }

    public async reportSessionEnded(): Promise<void> {
        try {
            await this.sdkApi.logout();
        } catch {
            // Logging out locally must still succeed when the session is already expired.
        }
    }

    public async request<T>(
        method: 'DELETE' | 'GET' | 'PATCH' | 'POST' | 'PUT',
        path: string,
        options: RequestOptions = {}
    ): Promise<T> {
        try {
            const response = await this.sdkApi.axiosInstance.request<T>({
                ...(options.body === undefined ? {} : { data: options.body }),
                headers: {
                    Authorization: this.sdkApi.authorizationHeader,
                    ...(options.body === undefined ? {} : { 'Content-Type': 'application/json' })
                },
                method,
                ...(options.query ? { params: options.query } : {}),
                ...(options.signal ? { signal: options.signal } : {}),
                url: this.sdkApi.getUri(path)
            });
            return response.data;
        } catch (error) {
            throw toApiError(error);
        }
    }
}

export function createJellyfinClient(deviceId: string, accessToken = '', baseUrl = getDefaultApiBaseUrl()) {
    return new JellyfinClient(baseUrl, deviceId, accessToken);
}
