# AI Formation Platform

Application web ASP.NET Core de gestion des formations avec module **formateur IA** (avatar conversationnel Anam.ai).

## Architecture

```
src/
├── AIFormationPlatform.Core/           # Entités, enums, interfaces (IAvatarService)
├── AIFormationPlatform.Infrastructure/ # EF Core, Identity, AnamAvatarService
└── AIFormationPlatform.Web/            # API Minimal + UI statique (demo avatar)
```

## Prérequis

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/) (optionnel, pour PostgreSQL local)
- Clés API : **OpenAI** (chat texte) et **Anam.ai** (avatar vocal)

## Démarrage rapide

Lancez PostgreSQL, puis configurez les variables requises :

```powershell
docker compose up -d postgres
cd src/AIFormationPlatform.Web
$env:PORT = "8080"
$env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5432;Database=AIFormationPlatform;Username=postgres;Password=YourStrongPassword"
$env:OPENAI_API_KEY = "sk-..."
$env:ANAM_API_KEY = "..."
dotnet run
```

Ouvrir http://localhost:8080

## Démarrage avec PostgreSQL

1. Lancer PostgreSQL :

```powershell
docker compose up -d postgres
```

2. Configurer la connexion (copier `.env.example` ou créer `appsettings.Development.json`) :

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=AIFormationPlatform;Username=postgres;Password=YourStrongPassword"
  }
}
```

3. Appliquer les migrations (automatique au démarrage, ou manuellement) :

```powershell
dotnet ef database update --project src/AIFormationPlatform.Infrastructure --startup-project src/AIFormationPlatform.Web
```

4. Lancer l'application :

```powershell
dotnet run --project src/AIFormationPlatform.Web
```

## Variables d'environnement

| Variable | Description |
|----------|-------------|
| `ConnectionStrings__DefaultConnection` | Chaîne PostgreSQL |
| `OPENAI_API_KEY` | Clé API OpenAI (chat) |
| `ANAM_API_KEY` | Clé API Anam.ai |
| `ANAM_AVATAR_ID` | ID de l'avatar Anam |
| `ANAM_VOICE_ID` | ID de la voix |
| `ANAM_AVATAR_MODEL` | Modèle avatar (ex. `cara-4`) |
| `ANAM_LLM_ID` | LLM Anam (ex. `CUSTOMER_CLIENT_V1`) |
| `PORT` | Port HTTP (Railway) |

Voir [.env.example](.env.example) pour la liste complète.

## API existante (rétrocompatible)

| Endpoint | Description |
|----------|-------------|
| `POST /api/chat` | Chat texte streaming (SSE) |
| `POST /api/session-token` | Token session avatar Anam |
| `POST /api/clear-session` | Effacer l'historique chat |

## Déploiement Railway

1. Créer un service **PostgreSQL** et renseigner `ConnectionStrings__DefaultConnection`
2. Ajouter les variables `OPENAI_API_KEY`, `ANAM_API_KEY`, etc.
3. Railway détecte le `Dockerfile` à la racine

```powershell
docker build -t ai-formation-platform .
# Exemple de run local (Railway utilisera sa propre image/variables)
docker run -p 8080:8080 \
  -e PORT=8080 \
  -e ConnectionStrings__DefaultConnection="Host=host.docker.internal;Port=5432;Database=AIFormationPlatform;Username=postgres;Password=YourStrongPassword" \
  -e ANAM_API_KEY=... \
  -e OPENAI_API_KEY=... \
  ai-formation-platform
```

## État d'avancement

- [x] **Étape 0** — Architecture Core/Infrastructure, EF Core, Identity (schéma), `IAvatarService`
- [ ] Étape 1 — Authentification (pages Razor)
- [ ] Étape 2 — Admin catégories + formateurs
- [ ] Étape 3 — CRUD formations
- [ ] …

## Licence

Projet privé — usage interne.
