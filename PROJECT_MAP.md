# PROJECT MAP

## PROJECT OVERVIEW

Project: AI Formation Platform

Description: Plateforme Web .NET de gestion des formations avec Formateur IA interactif.

---

## TECH STACK

| Technologie | Version | Statut |
|-------------|---------|--------|
| .NET | 10.0 (LTS) | VERIFIED |
| ASP.NET Core | 10.0 | VERIFIED |
| C# | 14.0 | VERIFIED |
| Entity Framework Core | 10.0 | VERIFIED |
| Npgsql.EFCore.PostgreSQL | 10.0.2 | VERIFIED |
| Database | PostgreSQL 17.x | CONFIGURED |
| Authentication | ASP.NET Core Identity | IMPLEMENTED |
| Frontend | Razor Views, Bootstrap 5.3 | IMPLEMENTED |
| AI Provider | OpenAI (GPT-4o-mini) | IMPLEMENTED |
| Avatar Provider | Anam AI (JavaScript SDK) | PENDING |
| Hosting | Railway (Docker) | CONFIGURED |

---

## SYSTEM FLOW

### Administrateur
```
Connexion → Dashboard → Formations → Modules → Leçons → Quiz → Publication
```

### Étudiant
```
Inscription → Connexion → Catalogue → Formation → Leçon → Formateur IA → Quiz → Résultat → Certificat
```

### Formateur IA
```
Question → Validation → Leçon → Contenu → Prompt → OpenAI → Réponse → Affichage
```

---

## ARCHITECTURE

Style: Monolithique modulaire (Feature-based)

Structure:
```
src/AIFormationPlatform.Web/
├── Features/
│   ├── Authentication/ (Register, Login, Logout)
│   ├── Admin/ (Dashboard)
│   ├── Formations/ (CRUD Admin)
│   ├── Modules/ (CRUD Admin)
│   ├── Lessons/ (CRUD Admin)
│   ├── Enrollments/ (Student enrollment, Progress)
│   ├── Quizzes/ (Admin CRUD + Student take quiz)
│   ├── AITrainer/ (IA Service + Controller)
│   └── Certificates/ (PDF certificate generation)
├── Data/ (AppDbContext + Configurations)
├── Models/ (14 entities)
├── Shared/ (Home controller + Layout)
├── Views/ (Razor Views)
└── wwwroot/ (Static files)
```

---

## DATABASE

Entities: 14
- ApplicationUser, Formation, Module, Lesson
- Enrollment, LessonProgress
- Quiz, Question, AnswerChoice
- QuizAttempt, StudentAnswer
- AIConversation, AIMessage

Relationships:
- Formation 1→N Modules (Cascade)
- Module 1→N Lessons (Cascade)
- Formation 1→N Enrollments (Cascade)
- Formation 1→N Quizzes (Cascade)
- Quiz 1→N Questions (Cascade)
- Question 1→N AnswerChoices (Cascade)
- Quiz 1→N QuizAttempts (Cascade)
- QuizAttempt 1→N StudentAnswers (Cascade)
- Lesson 1→N LessonProgress (Cascade)
- Lesson 1→N AIConversations (Cascade)
- AIConversation 1→N AIMessages (Cascade)

Constraints:
- Enrollment(UserId, FormationId) UNIQUE
- LessonProgress(UserId, LessonId) UNIQUE

---

## AI INTEGRATION

Selected AI Provider: OpenAI
Selected Model: GPT-4o-mini
Context Strategy: System Prompt + contenu de la leçon
Fallback Strategy: Message d'erreur convivial
SDK: OpenAI .NET SDK 2.12.0
Avatar: Anam AI (JavaScript SDK) - PENDING

---

## SECURITY

Authentication: ASP.NET Core Identity (cookie)
Authorization: Role-based (Administrator, Student)
Roles: Administrator, Student
Secret Management: Variables d'environnement, User Secrets (dev)
CSRF: Anti-forgery tokens
Validation: Server-side (Data Annotations + ModelState)

---

## IMPLEMENTED FEATURES

| Feature | Status |
|---------|--------|
| Auth (Register/Login/Logout) | VERIFIED |
| Admin Dashboard | VERIFIED |
| Formations CRUD | VERIFIED |
| Modules CRUD | VERIFIED |
| Lessons CRUD | VERIFIED |
| Student Enrollment | VERIFIED |
| Progression Tracking | VERIFIED |
| Quiz (Create/Take/Result) | VERIFIED |
| AI Trainer (OpenAI) | IMPLEMENTED |
| AI Avatar (Anam AI) | PENDING |
| Completion Certificate (PDF) | VERIFIED |
| Tests | VERIFIED (14/14) |

---

## TESTS

- QuizScoreTests: Score calculation, unique constraints (app logic)
- EnrollmentTests: Enrollment creation, cascade delete
- AITrainerPromptTests: Prompt generation rules
- CertificateTests: Eligibility logic, PDF generation

Total: 14 tests, all passing

---

## DEPENDENCIES

| Package | Version |
|---------|---------|
| Npgsql.EntityFrameworkCore.PostgreSQL | 10.0.2 |
| Microsoft.AspNetCore.Identity.EntityFrameworkCore | 10.0.9 |
| Microsoft.EntityFrameworkCore.Design | 10.0.9 |
| OpenAI | 2.12.0 |
| QuestPDF | 2026.7.1 |

---

## DECISIONS

1. .NET 10 LTS chosen (supported until Nov 2028)
2. Monolithic architecture (no microservices)
3. No Repository Pattern (EF Core DbContext direct)
4. OpenAI as AI provider (mature .NET SDK)
5. Anam AI for avatar (real-time WebRTC)
6. PostgreSQL 17.x for database
7. Railway for hosting (Docker-based)
8. Feature-based folder organization
9. QuestPDF for PDF generation (fluent API, community license)

---

## ENVIRONMENT VARIABLES

| Variable | Description |
|----------|-------------|
| DATABASE_URL | PostgreSQL connection string |
| OPENAI_API_KEY | OpenAI API key |
| ANAM_API_KEY | Anam AI API key |
| ASPNETCORE_ENVIRONMENT | Production/Development |

---

## ORPHANS & PENDING

### PENDING

- Avatar Anam AI: Requires ANAM_API_KEY configuration
- Database migrations: Require PostgreSQL connection to generate
- Railway deployment: Requires GitHub push + Railway configuration

### BLOCKED

- Avatar Anam AI: API key not configured yet
- OpenAI integration: Requires OPENAI_API_KEY in production

---

## FUTURE IMPROVEMENTS

- Application mobile
- Système de paiement
- Diffusion en direct
- Messagerie entre étudiants
- Gamification avancée
- Système de recommandations
