import {
    createContext,
    type PropsWithChildren,
    useCallback,
    useContext,
    useMemo,
    useState
} from 'react';

const messages = {
    en: {
        'actions.delete': 'Delete',
        'actions.done': 'Done',
        'actions.loading': 'Loading…',
        'actions.retry': 'Try again',
        'actions.save': 'Save',
        'auth.backToLogin': 'Back to sign in',
        'auth.forgot.kicker': 'Account recovery',
        'auth.forgot.link': 'Forgot password?',
        'auth.forgot.submit': 'Request help',
        'auth.forgot.title': 'Recover your account',
        'auth.forgot.result.ContactAdmin': 'Contact the server administrator to reset your password.',
        'auth.forgot.result.InNetworkRequired': 'Connect from the server’s local network and try again.',
        'auth.forgot.result.PinCode': 'Use the PIN file shown below to finish the reset.',
        'auth.help': 'Need help?',
        'auth.login.pending': 'Signing in…',
        'auth.login.submit': 'Sign in',
        'auth.login.title': 'Welcome back',
        'auth.password': 'Password',
        'auth.passwordConfirmation': 'Confirm password',
        'auth.register.closed': 'Public registration is currently closed.',
        'auth.register.kicker': 'Join the library',
        'auth.register.link': 'Create an account',
        'auth.register.minimum': 'Use at least {count} characters.',
        'auth.register.pending': 'Creating account…',
        'auth.register.submit': 'Create account',
        'auth.register.title': 'Create your account',
        'auth.register.validation.confirmation': 'The passwords do not match.',
        'auth.register.validation.password': 'Use at least {minimum} characters.',
        'auth.register.validation.username': 'Enter a username.',
        'auth.server': 'Your private cinema',
        'auth.username': 'Username',
        'errors.network': 'The server cannot be reached.',
        'errors.badRequest': 'Check the information and try again.',
        'errors.conflict': 'This name is already in use.',
        'errors.forbidden': 'This action is not allowed.',
        'errors.invalidCredentials': 'Incorrect username or password.',
        'errors.notFound': 'The requested resource does not exist.',
        'errors.rateLimited': 'Too many attempts. Try again in {seconds} seconds.',
        'errors.unavailable': 'This service is temporarily unavailable.',
        'errors.unknown': 'Something went wrong. Please try again.',
        'auth.register.errors.conflict': 'This username is already in use.',
        'auth.register.errors.disabled': 'Public registration is disabled.',
        'auth.register.errors.invalid': 'The registration information is invalid.',
        'auth.register.errors.rateLimited': 'Too many registrations. Try again in {seconds} seconds.',
        'profiles.add': 'Add a profile',
        'profiles.avatar': 'Avatar',
        'profiles.avatars.amber': 'Amber',
        'profiles.avatars.forest': 'Forest',
        'profiles.avatars.ocean': 'Ocean',
        'profiles.avatars.snow': 'Snow',
        'profiles.avatars.stone': 'Stone',
        'profiles.choose': 'Who’s watching?',
        'profiles.create': 'Create profile',
        'profiles.deleteConfirm': 'Delete {name}?',
        'profiles.kicker': 'One account, your own space',
        'profiles.limit': 'The limit of {count} profiles has been reached.',
        'profiles.manage': 'Manage profiles',
        'profiles.manageKicker': 'Personal spaces',
        'profiles.name': 'Profile name',
        'profiles.playbackConflict': 'Stop playback before changing or deleting this profile.',
        'setup.description': 'This viewer app starts once the server setup has been completed.',
        'setup.kicker': 'Server setup',
        'setup.legacyInstruction': 'Launch Jellyfin with the legacy web client, finish its setup wizard, then restart with this web directory.',
        'setup.title': 'Finish configuring Jellyfin',
        account: 'Account',
        add: 'Add',
        back: 'Back',
        browseMovies: 'Movies',
        browseSeries: 'Series',
        cancel: 'Cancel',
        close: 'Close',
        continueWatching: 'Continue watching',
        delete: 'Delete',
        details: 'More info',
        edit: 'Edit',
        error: 'Something went wrong',
        errorDescription: 'The requested content could not be loaded.',
        feedbackDown: 'Not for me',
        feedbackUp: 'I like this',
        forgotPassword: 'Forgot password?',
        fullScreen: 'Full screen',
        genres: 'Genres',
        history: 'History',
        home: 'Home',
        language: 'Language',
        loading: 'Loading',
        login: 'Sign in',
        logout: 'Sign out',
        myList: 'My list',
        newAndPopular: 'New & popular',
        nextEpisode: 'Next episode',
        noResults: 'Nothing here yet',
        offline: 'You are offline. Playback and changes are unavailable.',
        password: 'Password',
        pause: 'Pause',
        pictureInPicture: 'Picture in picture',
        play: 'Play',
        playback: 'Playback',
        profile: 'Profile',
        profiles: 'Profiles',
        quality: 'Quality',
        register: 'Create account',
        remove: 'Remove',
        resume: 'Resume',
        retry: 'Try again',
        search: 'Search',
        searchHint: 'Titles, people and genres',
        seasons: 'Seasons',
        settings: 'Settings',
        skip: 'Skip',
        skipCredits: 'Skip credits',
        skipIntro: 'Skip intro',
        skipRecap: 'Skip recap',
        startupIncomplete: 'Finish setting up Jellyfin in the legacy administration panel, then come back here.',
        subtitles: 'Subtitles',
        top10: 'Top 10',
        trending: 'Trending',
        username: 'Username',
        volume: 'Volume',
        whoIsWatching: 'Who’s watching?'
    },
    fr: {
        'actions.delete': 'Supprimer',
        'actions.done': 'Terminé',
        'actions.loading': 'Chargement…',
        'actions.retry': 'Réessayer',
        'actions.save': 'Enregistrer',
        'auth.backToLogin': 'Retour à la connexion',
        'auth.forgot.kicker': 'Récupération du compte',
        'auth.forgot.link': 'Mot de passe oublié ?',
        'auth.forgot.submit': 'Demander de l’aide',
        'auth.forgot.title': 'Récupérer votre compte',
        'auth.forgot.result.ContactAdmin': 'Contactez l’administrateur du serveur pour réinitialiser votre mot de passe.',
        'auth.forgot.result.InNetworkRequired': 'Connectez-vous depuis le réseau local du serveur puis réessayez.',
        'auth.forgot.result.PinCode': 'Utilisez le fichier PIN affiché ci-dessous pour terminer la réinitialisation.',
        'auth.help': 'Besoin d’aide ?',
        'auth.login.pending': 'Connexion…',
        'auth.login.submit': 'Se connecter',
        'auth.login.title': 'Bon retour',
        'auth.password': 'Mot de passe',
        'auth.passwordConfirmation': 'Confirmer le mot de passe',
        'auth.register.closed': 'Les inscriptions publiques sont actuellement fermées.',
        'auth.register.kicker': 'Rejoindre la médiathèque',
        'auth.register.link': 'Créer un compte',
        'auth.register.minimum': 'Utilisez au moins {count} caractères.',
        'auth.register.pending': 'Création du compte…',
        'auth.register.submit': 'Créer le compte',
        'auth.register.title': 'Créer votre compte',
        'auth.register.validation.confirmation': 'Les mots de passe ne correspondent pas.',
        'auth.register.validation.password': 'Utilisez au moins {minimum} caractères.',
        'auth.register.validation.username': 'Saisissez un nom d’utilisateur.',
        'auth.server': 'Votre cinéma privé',
        'auth.username': 'Nom d’utilisateur',
        'errors.network': 'Le serveur est injoignable.',
        'errors.badRequest': 'Vérifiez les informations puis réessayez.',
        'errors.conflict': 'Ce nom est déjà utilisé.',
        'errors.forbidden': 'Cette action n’est pas autorisée.',
        'errors.invalidCredentials': 'Nom d’utilisateur ou mot de passe incorrect.',
        'errors.notFound': 'La ressource demandée n’existe pas.',
        'errors.rateLimited': 'Trop de tentatives. Réessayez dans {seconds} secondes.',
        'errors.unavailable': 'Ce service est temporairement indisponible.',
        'errors.unknown': 'Une erreur est survenue. Veuillez réessayer.',
        'auth.register.errors.conflict': 'Ce nom d’utilisateur est déjà utilisé.',
        'auth.register.errors.disabled': 'Les inscriptions publiques sont désactivées.',
        'auth.register.errors.invalid': 'Les informations d’inscription sont invalides.',
        'auth.register.errors.rateLimited': 'Trop d’inscriptions. Réessayez dans {seconds} secondes.',
        'profiles.add': 'Ajouter un profil',
        'profiles.avatar': 'Avatar',
        'profiles.avatars.amber': 'Ambre',
        'profiles.avatars.forest': 'Forêt',
        'profiles.avatars.ocean': 'Océan',
        'profiles.avatars.snow': 'Neige',
        'profiles.avatars.stone': 'Pierre',
        'profiles.choose': 'Qui regarde ?',
        'profiles.create': 'Créer le profil',
        'profiles.deleteConfirm': 'Supprimer {name} ?',
        'profiles.kicker': 'Un compte, votre espace',
        'profiles.limit': 'La limite de {count} profils est atteinte.',
        'profiles.manage': 'Gérer les profils',
        'profiles.manageKicker': 'Espaces personnels',
        'profiles.name': 'Nom du profil',
        'profiles.playbackConflict': 'Arrêtez la lecture avant de modifier ou supprimer ce profil.',
        'setup.description': 'Cette application spectateur démarre une fois le serveur configuré.',
        'setup.kicker': 'Configuration du serveur',
        'setup.legacyInstruction': 'Lancez Jellyfin avec le client web historique, terminez son assistant, puis redémarrez avec ce dossier web.',
        'setup.title': 'Terminez la configuration de Jellyfin',
        account: 'Compte',
        add: 'Ajouter',
        back: 'Retour',
        browseMovies: 'Films',
        browseSeries: 'Séries',
        cancel: 'Annuler',
        close: 'Fermer',
        continueWatching: 'Continuer à regarder',
        delete: 'Supprimer',
        details: 'Plus d’infos',
        edit: 'Modifier',
        error: 'Une erreur est survenue',
        errorDescription: 'Le contenu demandé n’a pas pu être chargé.',
        feedbackDown: 'Pas pour moi',
        feedbackUp: 'J’aime',
        forgotPassword: 'Mot de passe oublié ?',
        fullScreen: 'Plein écran',
        genres: 'Genres',
        history: 'Historique',
        home: 'Accueil',
        language: 'Langue',
        loading: 'Chargement',
        login: 'Se connecter',
        logout: 'Se déconnecter',
        myList: 'Ma liste',
        newAndPopular: 'Nouveautés',
        nextEpisode: 'Épisode suivant',
        noResults: 'Rien à afficher pour le moment',
        offline: 'Vous êtes hors connexion. La lecture et les modifications sont indisponibles.',
        password: 'Mot de passe',
        pause: 'Pause',
        pictureInPicture: 'Image dans l’image',
        play: 'Lecture',
        playback: 'Lecture',
        profile: 'Profil',
        profiles: 'Profils',
        quality: 'Qualité',
        register: 'Créer un compte',
        remove: 'Retirer',
        resume: 'Reprendre',
        retry: 'Réessayer',
        search: 'Recherche',
        searchHint: 'Titres, personnes et genres',
        seasons: 'Saisons',
        settings: 'Réglages',
        skip: 'Passer',
        skipCredits: 'Passer le générique',
        skipIntro: 'Passer l’introduction',
        skipRecap: 'Passer le résumé',
        startupIncomplete: 'Terminez la configuration de Jellyfin dans le panel d’administration historique, puis revenez ici.',
        subtitles: 'Sous-titres',
        top10: 'Top 10',
        trending: 'Tendances',
        username: 'Nom d’utilisateur',
        volume: 'Volume',
        whoIsWatching: 'Qui regarde ?'
    }
} as const;

export type Locale = keyof typeof messages;
export type MessageKey = string;

interface I18nValue {
    locale: Locale;
    setLocale: (locale: Locale) => void;
    t: (key: MessageKey, values?: Record<string, number | string>) => string;
}

const STORAGE_KEY = 'jellyfin-web-new:locale';
const I18nContext = createContext<I18nValue | undefined>(undefined);

function translate(locale: Locale, key: string, values: Record<string, number | string> = {}): string {
    const dictionaries: Record<Locale, Record<string, string>> = messages;
    const template = dictionaries[locale][key] ?? dictionaries.en[key] ?? key;
    return Object.entries(values).reduce(
        (value, [name, replacement]) => value.replaceAll(`{${name}}`, String(replacement)),
        template
    );
}

function initialLocale(): Locale {
    const stored = localStorage.getItem(STORAGE_KEY);
    if (stored === 'en' || stored === 'fr') {
        return stored;
    }

    return navigator.language.toLowerCase().startsWith('fr') ? 'fr' : 'en';
}

export function I18nProvider({ children }: PropsWithChildren) {
    const [ locale, setLocaleState ] = useState<Locale>(initialLocale);
    const setLocale = useCallback((nextLocale: Locale) => {
        localStorage.setItem(STORAGE_KEY, nextLocale);
        document.documentElement.lang = nextLocale;
        setLocaleState(nextLocale);
    }, []);
    const value = useMemo<I18nValue>(() => ({
        locale,
        setLocale,
        t: (key, values) => translate(locale, key, values)
    }), [ locale, setLocale ]);

    return <I18nContext.Provider value={value}>{children}</I18nContext.Provider>;
}

export function useI18n(): I18nValue {
    const value = useContext(I18nContext);
    if (!value) {
        throw new Error('useI18n must be used inside I18nProvider');
    }

    return value;
}
