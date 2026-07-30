# jellyfin-web-new

Client spectateur Jellyfin dédié aux films et séries. Il ne remplace pas le client historique et n’est jamais sélectionné automatiquement par le serveur.

## Prérequis

- Node.js 24
- npm 11
- .NET SDK compatible avec le dépôt Jellyfin
- backend Jellyfin démarrable depuis le dossier parent

## Développement

```powershell
npm ci
npm run dev
```

Vite relaie `/jellyfin-api` vers `http://127.0.0.1:8096`. Pour une autre instance :

```powershell
$env:JELLYFIN_DEV_SERVER = 'http://127.0.0.1:8097'
npm run dev
```

## Build et lancement Jellyfin

```powershell
npm ci
npm run build
npm run jellyfin
```

Le dernier script résout `dist` en chemin absolu et lance :

```powershell
dotnet run --project ../Jellyfin.Server -- --webdir <chemin-absolu-vers-dist>
```

Pour revenir au client historique, redémarrer Jellyfin avec `--webdir` dirigé vers le `dist` de `jellyfin-web`. Aucun chemin web par défaut du serveur n’est modifié par ce projet.

## Vérifications

```powershell
npm run typecheck
npm run lint
npm test
npm run test:e2e
npm run build
```

Les tests Playwright utilisent des réponses API déterministes. Une validation média réelle reste nécessaire pour les codecs fournis par le navigateur hôte.

### Avis npm connu

React Router 7.18.2 reste signalé par `npm audit` pour
[GHSA-qwww-vcr4-c8h2](https://github.com/advisories/GHSA-qwww-vcr4-c8h2), limité au mode
RSC/Server Actions que cette SPA statique n’active pas. Les versions antérieures réintroduisent
des vulnérabilités applicables au routage client ; conserver 7.18.2 jusqu’à la publication du correctif amont.
