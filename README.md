# AIFormationPlatform

Plateforme de formation en ligne construite avec ASP.NET Core. Elle propose des espaces Admin, Formateur et Apprenant, ainsi qu'un avatar IA Anam.ai pour accompagner les apprenants.

## Liens de test

| Environnement | Lien |
|---|---|
| Application locale | `http://localhost:8080` |
| Connexion locale | `http://localhost:8080/Account/Login` |
| Administration | `http://localhost:8080/Admin/Dashboard` |
| Comptes | `http://localhost:8080/Admin/Comptes` |
| Deploiement Railway | A renseigner apres le deploiement Railway |

> Apres le deploiement, remplacez la derniere ligne par l'URL publique Railway et ajoutez `/Account/Login` pour le lien de connexion.

## Comptes de demonstration

Les comptes suivants sont crees au demarrage de l'application si la base de donnees est vide :

| Role | E-mail | Mot de passe |
|---|---|---|
| Administrateur | `admin@aiformation.local` | `ChangeMe!2026` |
| Formateur IA | `formateur.ia@aiformation.local` | `ChangeMe!2026` |

Changez ces mots de passe avant une mise en production.

Depuis le compte Administrateur, la page **Comptes** permet de creer des comptes et de lancer rapidement une session de test Formateur IA.

## Tester rapidement la plateforme

1. Demarrez l'application avec `dotnet run --project src/AIFormationPlatform.Web`.
2. Ouvrez `http://localhost:8080/Account/Login`.
3. Connectez-vous avec le compte Administrateur.
4. Ouvrez la page **Formations** pour voir les formations de demonstration : **Introduction a .NET** et **Fondamentaux des reseaux**.
5. Ouvrez **Comptes**, puis cliquez sur **Demarrer le Formateur IA** pour tester l'espace formateur.
6. Pour tester Anam, ouvrez un module depuis l'espace Apprenant. L'avatar repond en francais par defaut et peut changer de langue si vous le lui demandez.

## Prerequis et configuration

- .NET 10 SDK
- PostgreSQL
- Une cle API Anam (`ANAM_API_KEY`) pour l'avatar
- Une cle Gemini (`GEMINI_API_KEY`) pour le chat texte optionnel

Exemple PowerShell pour le developpement local :

```powershell
$env:PORT = "8080"
$env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5432;Database=AIFormationPlatform;Username=postgres;Password=VotreMotDePasse"
$env:ANAM_API_KEY = "votre-cle-anam"
$env:GEMINI_API_KEY = "votre-cle-gemini"
dotnet run --project src/AIFormationPlatform.Web
```

Les migrations sont appliquees automatiquement au demarrage.

## Architecture

```text
src/
|- AIFormationPlatform.Core/           Entites, enums et interfaces
|- AIFormationPlatform.Infrastructure/ EF Core, Identity, PostgreSQL et Anam
`- AIFormationPlatform.Web/            Razor Pages, API minimale et interface web
```

## Deploiement Railway

Configurez au minimum les variables suivantes dans Railway :

| Variable | Description |
|---|---|
| `DATABASE_URL` | URL PostgreSQL fournie par Railway |
| `PORT` | Port HTTP fourni par Railway |
| `ANAM_API_KEY` | Cle API Anam.ai |
| `GEMINI_API_KEY` | Cle API Google Gemini, si le chat texte est utilise |

Le projet contient un `Dockerfile` et un fichier `railway.json` pour le deploiement.
