# Sécurité du client

- Le client fonctionne uniquement en même origine avec Jellyfin.
- La CSP interdit les scripts externes et inline, les objets et les soumissions hors origine.
- Le service worker ne met jamais en cache l’authentification, les médias, les sessions, l’historique ou les routes CustomNetflix.
- La déconnexion efface la session, TanStack Query, les snapshots IndexedDB, les caches PWA et l’état du lecteur.
- Les rapports terminaux utilisent un en-tête d’authentification avec `keepalive`; le jeton est retiré de leur URL.

## Audit npm

L’override EJS maintient la chaîne de build Workbox sur une version corrigée. `npm audit` conserve uniquement l’avis `GHSA-qwww-vcr4-c8h2` de React Router : il vise le mode serveur RSC et ses actions. Ce projet est une SPA statique `createHashRouter`, sans RSC, SSR, action ni loader serveur ; le chemin vulnérable n’est donc pas présent. Un downgrade réintroduirait plusieurs avis SPA corrigés. Mettre React Router à jour dès qu’une version postérieure corrigée est publiée.
