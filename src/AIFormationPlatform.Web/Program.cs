using AIFormationPlatform.Web.Data;
using AIFormationPlatform.Web.Features.AITrainer.Services;
using AIFormationPlatform.Web.Features.Certificates.Services;
using AIFormationPlatform.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var appPort = Environment.GetEnvironmentVariable("PORT") ?? "5000";
builder.WebHost.UseUrls($"http://*:{appPort}");

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
if (!string.IsNullOrEmpty(databaseUrl))
{
    var uri = new Uri(databaseUrl);
    var userInfo = uri.UserInfo.Split(':');
    var port = uri.Port > 0 ? uri.Port : 5432;
    connectionString = $"Host={uri.Host};Port={port};Database={uri.AbsolutePath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]}";
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 3;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(2);
});

builder.Services.AddScoped<ICertificateService, CertificateService>();
builder.Services.AddScoped<IAIProvider, OpenAIProvider>();
builder.Services.AddScoped<IAITrainerService, AITrainerService>();
builder.Services.AddHttpClient();
builder.Services.Configure<Microsoft.AspNetCore.Mvc.Razor.RazorViewEngineOptions>(options =>
{
    options.ViewLocationFormats.Clear();
    options.ViewLocationFormats.Add("/Views/{1}/{0}" + Microsoft.AspNetCore.Mvc.Razor.RazorViewEngine.ViewExtension);
    options.ViewLocationFormats.Add("/Features/{1}/Views/{1}/{0}" + Microsoft.AspNetCore.Mvc.Razor.RazorViewEngine.ViewExtension);
    options.ViewLocationFormats.Add("/Features/{1}/Views/{0}" + Microsoft.AspNetCore.Mvc.Razor.RazorViewEngine.ViewExtension);
    options.ViewLocationFormats.Add("/Views/Shared/{0}" + Microsoft.AspNetCore.Mvc.Razor.RazorViewEngine.ViewExtension);
});
builder.Services.AddControllersWithViews();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var db = services.GetRequiredService<AppDbContext>();
    try
    {
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Erreur lors de la migration de la base de données");
    }

    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

    if (!await roleManager.RoleExistsAsync("Administrator"))
        await roleManager.CreateAsync(new IdentityRole("Administrator"));

    if (!await roleManager.RoleExistsAsync("Student"))
        await roleManager.CreateAsync(new IdentityRole("Student"));

    var adminEmail = "admin@admin.com";
    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser == null)
    {
        adminUser = new ApplicationUser
        {
            UserName = "admin",
            Email = adminEmail,
            EmailConfirmed = true,
            FirstName = "Admin",
            LastName = "Admin"
        };
        await userManager.CreateAsync(adminUser, "admin");
        await userManager.AddToRoleAsync(adminUser, "Administrator");
    }

    var studentEmail = "student@student.com";
    var studentUser = await userManager.FindByEmailAsync(studentEmail);
    if (studentUser == null)
    {
        studentUser = new ApplicationUser
        {
            UserName = "student",
            Email = studentEmail,
            EmailConfirmed = true,
            FirstName = "Student",
            LastName = "Student"
        };
        await userManager.CreateAsync(studentUser, "student");
        await userManager.AddToRoleAsync(studentUser, "Student");
    }

    if (!db.Formations.Any())
    {
        var formations = new List<Formation>
        {
            new() { Title = "Introduction à Python", Description = "Apprenez les bases de Python : variables, boucles, fonctions et structures de données.", IsPublished = true },
            new() { Title = "Développement Web avec HTML/CSS", Description = "Créez vos premières pages web avec HTML5 et CSS3.", IsPublished = true },
            new() { Title = "JavaScript Fundamentals", Description = "Maîtrisez les concepts clés de JavaScript : DOM, événements, async/await.", IsPublished = true },
            new() { Title = "Bases de données SQL", Description = "Apprenez à créer et interroger des bases de données relationnelles avec SQL.", IsPublished = true },
            new() { Title = "Git et GitHub pour débutants", Description = "Gérez votre code avec Git et collaborez sur GitHub.", IsPublished = true },
            new() { Title = "Introduction à React", Description = "Créez des interfaces utilisateur modernes avec React et ses hooks.", IsPublished = true },
            new() { Title = "Cybersécurité de base", Description = "Comprenez les menaces informatiques et protégez vos applications.", IsPublished = true },
            new() { Title = "Machine Learning avec Python", Description = "Découvrez les bases du machine learning avec scikit-learn et TensorFlow.", IsPublished = true },
            new() { Title = "Docker et Conteneurisation", Description = "Apprenez à conteneuriser vos applications avec Docker.", IsPublished = true },
            new() { Title = "Communication Professionnelle", Description = "Développez vos compétences en communication écrite et orale en entreprise.", IsPublished = true }
        };
        db.Formations.AddRange(formations);
        await db.SaveChangesAsync();

        var allFormations = await db.Formations.ToListAsync();
        foreach (var f in allFormations)
        {
            var mod1 = new Module { FormationId = f.Id, Title = "Module 1 - Fondamentaux", Description = "Les bases essentielles", SortOrder = 1 };
            var mod2 = new Module { FormationId = f.Id, Title = "Module 2 - Pratique", Description = "Mise en pratique", SortOrder = 2 };
            var mod3 = new Module { FormationId = f.Id, Title = "Module 3 - Avancé", Description = "Concepts avancés", SortOrder = 3 };
            db.Modules.AddRange(mod1, mod2, mod3);
            await db.SaveChangesAsync();

            db.Lessons.AddRange(
                new Lesson { ModuleId = mod1.Id, Title = "Introduction et installation", Content = GetLessonContent(f.Title, "introduction"), SortOrder = 1 },
                new Lesson { ModuleId = mod1.Id, Title = "Les variables et types", Content = GetLessonContent(f.Title, "variables"), SortOrder = 2 },
                new Lesson { ModuleId = mod1.Id, Title = "Les conditions", Content = GetLessonContent(f.Title, "conditions"), SortOrder = 3 },
                new Lesson { ModuleId = mod2.Id, Title = "Premier exercice pratique", Content = GetLessonContent(f.Title, "exercice1"), SortOrder = 1 },
                new Lesson { ModuleId = mod2.Id, Title = "Exercices intermédiaires", Content = GetLessonContent(f.Title, "exercice2"), SortOrder = 2 },
                new Lesson { ModuleId = mod3.Id, Title = "Projets avancés", Content = GetLessonContent(f.Title, "avance"), SortOrder = 1 },
                new Lesson { ModuleId = mod3.Id, Title = "Récapitulatif et conclusion", Content = GetLessonContent(f.Title, "conclusion"), SortOrder = 2 }
            );
            await db.SaveChangesAsync();
        }
    }
}

app.Run();

static string GetLessonContent(string formationTitle, string topic)
{
    var contents = new Dictionary<string, Dictionary<string, string>>
    {
        ["Introduction à Python"] = new()
        {
            ["introduction"] = "Python est un langage de programmation interprété, créé en 1991 par Guido van Rossum. Il est reconnu pour sa syntaxe claire et lisible.\n\nInstallation :\n1. Téléchargez Python depuis python.org\n2. Cochez 'Add Python to PATH'\n3. Vérifiez l'installation avec : python --version\n\nPour commencer à coder, utilisez un éditeur comme VS Code, PyCharm ou même le terminal.",
            ["variables"] = "En Python, les variables se déclarent sans specifying le type :\n\nnom = 'Alice'  # Chaîne de caractères\nage = 25       # Entier\ntaille = 1.68  # Décimale\nest_etudiant = True  # Booléen\n\nprint(f'Je m\\'appelle {nom}, j\\'ai {age} ans')\n\nLes types principaux sont : str, int, float, bool, list, dict, tuple.",
            ["conditions"] = "Les conditions en Python utilisent if/elif/else :\n\nage = 18\nif age >= 18:\n    print('Majeur')\nelif age >= 16:\n    print('Presque majeur')\nelse:\n    print('Mineur')\n\nOpérateurs : ==, !=, <, >, <=, >=, and, or, not",
            ["exercice1"] = "Exercice : Créez un programme qui demande le nom et l'âge de l'utilisateur, puis affiche un message personnalisé.\n\nnom = input('Votre nom : ')\nage = int(input('Votre âge : '))\nif age >= 18:\n    print(f'Bonjour {nom}, vous êtes majeur.')\nelse:\n    print(f'Bonjour {nom}, vous êtes mineur.')",
            ["exercice2"] = "Exercice intermédiaire : Créez une liste de notes, calculez la moyenne et affichez la mention.\n\nnotes = [15, 12, 18, 14, 16]\nmoyenne = sum(notes) / len(notes)\nprint(f'Moyenne : {moyenne:.2f}')\nif moyenne >= 16: print('Très bien')\nelif moyenne >= 14: print('Bien')\nelif moyenne >= 12: print('Assez bien')\nelse: print('À améliorer')",
            ["avance"] = "Concepts avancés en Python :\n\n1. List comprehension : squares = [x**2 for x in range(10)]\n2. Fonctions lambda : double = lambda x: x * 2\n3. Decorateurs : @property, @staticmethod\n4. Gestion d'erreurs : try/except/finally\n5. Fichiers : with open('fichier.txt', 'r') as f:\n6. Modules : import math, from datetime import datetime",
            ["conclusion"] = "Récapitulatif du cours Python :\n\n✅ Variables et types de données\n✅ Conditions (if/elif/else)\n✅ Boucles (for, while)\n✅ Fonctions\n✅ Listes et dictionnaires\n✅ Gestion d'erreurs\n✅ Manipulation de fichiers\n\nProchaines étapes : Django, Flask, automatisation, data science."
        },
        ["Développement Web avec HTML/CSS"] = new()
        {
            ["introduction"] = "HTML (HyperText Markup Language) est le langage de structure du web. CSS (Cascading Style Sheets) contrôle l'apparence.\n\nStructure de base :\n<!DOCTYPE html>\n<html>\n<head>\n    <title>Ma page</title>\n    <link rel='stylesheet' href='style.css'>\n</head>\n<body>\n    <h1>Bonjour le monde !</h1>\n</body>\n</html>",
            ["variables"] = "En CSS, les propriétés definissent le style :\n\n/* Variables CSS */\n:root {\n  --primary-color: #007bff;\n  --font-size: 16px;\n}\n\n/* Sélecteurs */\n#mon-id { }  /* Par ID */\n.mon-class { }  /* Par classe */\ndiv p { }  /* Par hiérarchie */",
            ["conditions"] = "Les media queries permettent le responsive design :\n\n/* Mobile */\n@media (max-width: 768px) {\n  .container { flex-direction: column; }\n}\n\n/* Desktop */\n@media (min-width: 769px) {\n  .container { flex-direction: row; }\n}\n\nFlexbox et CSS Grid sont essentiels pour les mises en page modernes.",
            ["exercice1"] = "Exercice : Créez une page de profil avec une photo, un nom, une bio et un bouton.\n\nUtilisez une carte (card) avec : border-radius, box-shadow, padding, margin.\nCentrez le tout avec Flexbox : display:flex; justify-content:center; align-items:center.",
            ["exercice2"] = "Exercice intermédiaire : Créez un site one-page avec navigation fixe, sections alternées et footer.\n\nSections : Accueil, À propos, Services, Contact.\nUtilisez : position:sticky, scroll-behavior:smooth, nth-child.",
            ["avance"] = "Techniques avancées CSS :\n\n1. CSS Grid : display:grid; grid-template-columns: repeat(3, 1fr);\n2. Animations : @keyframes, transition, transform\n3. Variables CSS et calc()\n4. Pseudo-éléments ::before, ::after\n5. Blending modes et gradients\n6. CSS Custom Properties pour les thèmes",
            ["conclusion"] = "Récapitulatif HTML/CSS :\n\n✅ Structure HTML sémantique\n✅ Sélecteurs CSS\n✅ Flexbox et Grid\n✅ Responsive design\n✅ Animations CSS\n\nProchaines étapes : JavaScript, frameworks (Bootstrap, Tailwind)."
        },
        ["JavaScript Fundamentals"] = new()
        {
            ["introduction"] = "JavaScript est le langage de programmation du web. Il rend les pages web interactives.\n\n<script>\n  console.log('Hello World!');\n  document.title = 'Ma page JS';\n</script>\n\nJS peut modifier le HTML (DOM), réagir aux événements, et communiquer avec des serveurs (fetch API).",
            ["variables"] = "Variables en JavaScript :\n\nlet age = 25;        // Modifiable, portée de bloc\nconst nom = 'Alice'; // Non modifiable\nvar ancien = 'old';  // Éviter (portée floue)\n\nTypes : string, number, boolean, null, undefined, object, symbol\ntypeof operator pour vérifier le type.",
            ["conditions"] = "Conditions et opérateurs en JS :\n\nif (age >= 18) {\n  console.log('Majeur');\n} else {\n  console.log('Mineur');\n}\n\n// Ternaire\nconst statut = age >= 18 ? 'Majeur' : 'Mineur';\n\n// Switch\nswitch(role) {\n  case 'admin': break;\n  case 'user': break;\n}",
            ["exercice1"] = "Exercice : Créez un compteur avec boutons + et -.\n\nlet count = 0;\nfunction increment() { count++; updateDisplay(); }\nfunction decrement() { count--; updateDisplay(); }\nfunction updateDisplay() {\n  document.getElementById('count').textContent = count;\n}",
            ["exercice2"] = "Exercice intermédiaire : Créez une TODO list avec ajout, suppression et marquage.\n\nUtilisez : addEventListener, createElement, classList.toggle.\nStocker les tâches dans un tableau et manipuler le DOM.",
            ["avance"] = "Concepts avancés JavaScript :\n\n1. Promises et async/await\n2. Fetch API pour les requêtes HTTP\n3. DOM manipulation avancée\n4. Closures et portée\n5. ES6+ : destructuring, spread, modules\n6. Gestion d'événements (event delegation)",
            ["conclusion"] = "Récapitulatif JavaScript :\n\n✅ Variables (let, const)\n✅ Conditions et boucles\n✅ Fonctions\n✅ DOM manipulation\n✅ Événements\n✅ API Fetch\n\nProchaines étapes : React, Vue.js, Node.js."
        },
        ["Bases de données SQL"] = new()
        {
            ["introduction"] = "SQL (Structured Query Language) sert à gérer des bases de données relationnelles.\n\nConcepts clés :\n- Table : collection de données structurées\n- Ligne (tuple) : un enregistrement\n- Colonne (attribut) : un champ\n- Clé primaire : identifiant unique\n- Clé étrangère : lien entre tables\n\nSGBD populaires : PostgreSQL, MySQL, SQLite.",
            ["variables"] = "Types de données SQL principaux :\n\n- VARCHAR(n) : texte de longueur variable\n- INTEGER : nombres entiers\n- DECIMAL(p,s) : nombres décimaux\n- DATE : date\n- BOOLEAN : vrai/faux\n- TEXT : texte long\n- SERIAL : auto-incrémenté\n\nCREATE TABLE utilisateurs (\n  id SERIAL PRIMARY KEY,\n  nom VARCHAR(100) NOT NULL,\n  email VARCHAR(255) UNIQUE\n);",
            ["conditions"] = "Requêtes SQL de base :\n\n-- Sélection\nSELECT * FROM utilisateurs WHERE age > 18;\n\n-- Tri\nSELECT * FROM utilisateurs ORDER BY nom ASC;\n\n-- Agrégation\nSELECT COUNT(*), AVG(age) FROM utilisateurs;\n\n-- Jointure\nSELECT u.nom, c.titre\nFROM utilisateurs u\nINNER JOIN commandes c ON u.id = c.user_id;",
            ["exercice1"] = "Exercice : Créez une base de données pour gérer des étudiants.\n\n1. Table etudiants (id, nom, prenom, email, date_naissance)\n2. Table matieres (id, nom, coefficient)\n3. Table notes (id, etudiant_id, matiere_id, note)\n4. Insérez 5 étudiants et 3 matières",
            ["exercice2"] = "Exercice intermédiaire : Requêtes JOIN et agrégation.\n\n1. Calculez la moyenne de chaque étudiant\n2. Trouvez l'étudiant avec la meilleure moyenne\n3. Classez les étudiants par moyenne décroissante\n4. Comptez le nombre de notes par matière",
            ["avance"] = "Concepts SQL avancés :\n\n1. Index pour optimiser les performances\n2. Transactions (BEGIN, COMMIT, ROLLBACK)\n3. Vues (CREATE VIEW)\n4. Procédures stockées\n5. Normalisation (1NF, 2NF, 3NF)\n6. Under et EXISTS",
            ["conclusion"] = "Récapitulatif SQL :\n\n✅ CREATE, INSERT, SELECT, UPDATE, DELETE\n✅ Jointures (INNER, LEFT, RIGHT)\n✅ GROUP BY et HAVING\n✅ Index et optimisation\n✅ Conception de schéma\n\nProchaines étapes : ORM (Entity Framework), PostgreSQL avancé."
        },
        ["Git et GitHub pour débutants"] = new()
        {
            ["introduction"] = "Git est un système de contrôle de version distribué, créé en 2005 par Linus Torvalds.\n\nPourquoi Git ?\n- Suivre les modifications du code\n- Collaborer avec d'autres développeurs\n- Revenir à des versions précédentes\n- Brancher et merger sans conflits\n\nInstallation : git-scm.com",
            ["variables"] = "Commandes Git essentielles :\n\ngit init                    # Initialiser un dépôt\ngit add .                   # Ajouter les fichiers\ngit commit -m 'message'     # Sauvegarder\ngit status                  # Voir l'état\ngit log --oneline           # Historique\ngit diff                    # Voir les changements\ngit branch                  # Lister les branches",
            ["conditions"] = "Branches et merges :\n\ngit checkout -b feature     # Créer et basculer\ngit checkout main           # Retour à main\ngit merge feature           # Fusionner\ngit branch -d feature       # Supprimer\n\nRésolution de conflits :\n1. Ouvrez le fichier avec les conflits\n2. Choisissez les changements à garder\ngit add . && git commit",
            ["exercice1"] = "Exercice : Créez un dépôt Git et faites 5 commits.\n\n1. git init mon-projet\ncd mon-projet\n2. Créez un fichier README.md\n3. git add . && git commit -m 'init'\n4. Ajoutez du code, commit 4 fois\n5. Consultez l'historique avec git log",
            ["exercice2"] = "Exercice intermédiaire : Créez une branche, faites des modifications, et mergez.\n\ngit checkout -b feature-login\ngit add . && git commit -m 'feat: login'\ngit checkout main\ngit merge feature-login\n\nPoussez sur GitHub :\ngit remote add origin https://github.com/user/repo.git\ngit push -u origin main",
            ["avance"] = "Git avancé :\n\n1. git rebase : réécrire l'historique\n2. git stash : stocker temporairement\n3. git cherry-pick : récupérer un commit\n4. .gitignore : ignorer des fichiers\n5. GitHub Actions : CI/CD automatique\n6. Pull Requests et Code Review",
            ["conclusion"] = "Récapitulatif Git :\n\n✅ Init, add, commit, push\n✅ Branches et merges\n✅ Résolution de conflits\n✅ GitHub (remote, PR)\n✅ Bonnes pratiques (messages conventionnels)\n\nProchaines étapes : GitHub Actions, GitLab CI."
        },
        ["Introduction à React"] = new()
        {
            ["introduction"] = "React est une bibliothèque JavaScript pour créer des interfaces utilisateur, développée par Meta (Facebook).\n\nPourquoi React ?\n- Composants réutilisables\n- Virtual DOM performant\n- Écosystème riche\n- JSX : mélange HTML et JS\n\nnpx create-react-app mon-app\ncd mon-app && npm start",
            ["variables"] = "JSX et composants React :\n\nfunction App() {\n  const nom = 'React';\n  return (\n    <div>\n      <h1>Bonjour {nom} !</h1>\n      <p>Mon premier composant</p>\n    </div>\n  );\n}\n\nexport default App;\n\nJSX : syntaxe qui permet d'écrire du HTML dans JavaScript.",
            ["conditions"] = "Rendu conditionnel en React :\n\nfunction Greeting({ isLoggedIn }) {\n  return (\n    <div>\n      {isLoggedIn ? <h1>Bienvenue !</h1> : <h1>Connectez-vous</h1>}\n      {isLoggedIn && <p>Vous êtes connecté</p>}\n    </div>\n  );\n}\n\nListes avec .map() :\n{items.map(item => <li key={item.id}>{item.name}</li>)}",
            ["exercice1"] = "Exercice : Créez un composant Counter avec useState.\n\nimport { useState } from 'react';\n\nfunction Counter() {\n  const [count, setCount] = useState(0);\n  return (\n    <div>\n      <p>Compteur: {count}</p>\n      <button onClick={() => setCount(count + 1)}>+1</button>\n    </div>\n  );\n}",
            ["exercice2"] = "Exercice intermédiaire : Créez une app de todos avec ajout et suppression.\n\nUtilisez useState pour gérer la liste.\nCréez un formulaire contrôlé.\nImplémentez la suppression par index.",
            ["avance"] = "Concepts React avancés :\n\n1. useEffect pour les effets de bord\n2. useContext pour le partage d'état\n3. useReducer pour la logique complexe\n4. Custom Hooks\n5. React Router pour la navigation\n6. Performance : memo, useMemo, useCallback",
            ["conclusion"] = "Récapitulatif React :\n\n✅ Composants fonctionnels\n✅ JSX\n✅ useState et useEffect\n✅ Props et state\n✅ Rendu conditionnel et listes\n\nProchaines étapes : Redux, Next.js, TypeScript avec React."
        },
        ["Cybersécurité de base"] = new()
        {
            ["introduction"] = "La cybersécurité protège les systèmes et données contre les attaques.\n\nTypes de menaces :\n- Malware (virus, ransomware)\n- Phishing (hameçonnage)\n- Attaques DDoS\n- Injection SQL\n- XSS (Cross-Site Scripting)\n\nPrincipes CIA : Confidentialité, Intégrité, Disponibilité.",
            ["variables"] = "Principes de sécurité fondamentaux :\n\n1. Moindre privilège : accès minimum nécessaire\n2. Défense en profondeur : multiples couches\n3. Sécurité par conception (Security by Design)\n4. Mise à jour régulière des systèmes\n5. Authentification forte (MFA)\n6. Chiffrement des données sensibles",
            ["conditions"] = "Sécurité des mots de passe :\n\n✅ Minimum 12 caractères\n✅ Mélange majuscules/minuscules/chiffres/symboles\n✅ Pas de mots du dictionnaire\n✅ Gestionnaire de mots de passe\n✅ Authentification à deux facteurs\n\nHashage : bcrypt, Argon2 (jamais MD5/SHA1 pour les mots de passe)",
            ["exercice1"] = "Exercice : Identifiez les vulnérabilités dans ce code :\n\n// SQL Injection\nquery = 'SELECT * FROM users WHERE name=' + input\n\n// XSS\ninnerHTML = userInput\n\n// Mot de passe faible\npassword = '123456'\n\nCorrigez chaque problème.",
            ["exercice2"] = "Exercice intermédiaire : Configurez la sécurité d'une application web.\n\n1. Activez HTTPS\n2. Configurez les en-têtes de sécurité\n3. Implémentez la validation des entrées\n4. Configurez CORS\n5. Ajoutez le rate limiting",
            ["avance"] = "Sécurité avancée :\n\n1. Pentest et audit de sécurité\n2. OWASP Top 10\n3. Sécurité des API (OAuth2, JWT)\n4. Sécurité cloud (AWS, Azure)\n5. Response aux incidents\n6. Conformité (RGPD, PCI-DSS)",
            ["conclusion"] = "Récapitulatif Cybersécurité :\n\n✅ Menaces courantes\n✅ Bonnes pratiques\n✅ Sécurité des mots de passe\n✅ Sécurité des applications web\n✅ OWASP Top 10\n\nProchaines étapes : Certifications (CompTIA Security+, CEH)."
        },
        ["Machine Learning avec Python"] = new()
        {
            ["introduction"] = "Le Machine Learning permet aux ordinateurs d'apprendre à partir de données.\n\nTypes :\n- Supervisé : classification, régression\n- Non supervisé : clustering\n- Par renforcement : apprentissage par essai\n\nLibrairies : scikit-learn, TensorFlow, PyTorch, Pandas, NumPy.",
            ["variables"] = "Préparation des données :\n\nimport pandas as pd\nimport numpy as np\n\ndf = pd.read_csv('data.csv')\ndf.head()  # Premières lignes\ndf.describe()  # Statistiques\ndf.isnull().sum()  # Valeurs manquantes\n\nNettoyage :\n- Gérer les valeurs manquantes\n- Encoder les variables catégorielles\n- Normaliser les données",
            ["conditions"] = "Division des données :\n\nfrom sklearn.model_selection import train_test_split\n\nX_train, X_test, y_train, y_test = train_test_split(\n    X, y, test_size=0.2, random_state=42\n)\n\nPourquoi ? Évaluer le modèle sur des données jamais vues.\n- Train set : entraînement (80%)\n- Test set : évaluation (20%)",
            ["exercice1"] = "Exercice : Régression linéaire simple.\n\nfrom sklearn.linear_model import LinearRegression\n\nmodel = LinearRegression()\nmodel.fit(X_train, y_train)\npredictions = model.predict(X_test)\n\nMesurez l'erreur avec RMSE et R² score.",
            ["exercice2"] = "Exercice intermédiaire : Classification avec Random Forest.\n\nfrom sklearn.ensemble import RandomForestClassifier\nfrom sklearn.metrics import accuracy_score\n\nmodel = RandomForestClassifier(n_estimators=100)\nmodel.fit(X_train, y_train)\ny_pred = model.predict(X_test)\nprint(f'Accuracy: {accuracy_score(y_test, y_pred):.2f}')",
            ["avance"] = "ML avancé :\n\n1. Deep Learning (TensorFlow, PyTorch)\n2. NLP (traitement du langage)\n3. Computer Vision\n4. Hyperparameter tuning\n5. Cross-validation\n6. Pipeline de production (MLOps)",
            ["conclusion"] = "Récapitulatif Machine Learning :\n\n✅ Préparation des données\n✅ Modèles supervisés et non supervisés\n✅ Évaluation des performances\n✅ scikit-learn et Python\n\nProchaines étapes : Deep Learning, NLP, MLOps."
        },
        ["Docker et Conteneurisation"] = new()
        {
            ["introduction"] = "Docker permet de conteneuriser les applications pour une portabilité maximale.\n\nAvantages :\n- 'It works on my machine' résolu\n- Isolation des environnements\n- Déploiement rapide\n- Scalabilité\n\nInstallation : docker.com/get-docker",
            ["variables"] = "Concepts Docker clés :\n\n- Image : blueprint de l'application\n- Container : instance d'une image\n- Dockerfile : instructions pour construire l'image\n- Docker Hub : registre d'images\n- Volume : persistance des données\n- Network : communication entre containers\n\nImages de base : ubuntu, alpine, node, python",
            ["conditions"] = "Dockerfile et Docker Compose :\n\n# Dockerfile\nFROM node:18-alpine\nWORKDIR /app\nCOPY package*.json ./\nRUN npm install\nCOPY . .\nEXPOSE 3000\nCMD ['npm', 'start']\n\n# docker-compose.yml\nservices:\n  app:\n    build: .\n    ports: ['3000:3000']\n  db:\n    image: postgres:16",
            ["exercice1"] = "Exercice : Conteneurisez une application Node.js.\n\n1. Créez un Dockerfile\n2. Construisez l'image : docker build -t mon-app .\n3. Lancez le container : docker run -p 3000:3000 mon-app\n4. Vérifiez : docker ps, docker logs",
            ["exercice2"] = "Exercice intermédiaire : Docker Compose avec une app + base de données.\n\n1. Créez docker-compose.yml\n2. Ajoutez un service PostgreSQL\n3. Connectez l'app à la base\n4. Utilisez des volumes pour la persistence\n5. docker-compose up -d",
            ["avance"] = "Docker avancé :\n\n1. Multi-stage builds\n2. Docker networks personnalisés\n3. Health checks\n4. Docker secrets\n5. Kubernetes (orchestration)\n6. CI/CD avec Docker",
            ["conclusion"] = "Récapitulatif Docker :\n\n✅ Images et containers\n✅ Dockerfile\n✅ Docker Compose\n✅ Volumes et networks\n✅ Bonnes pratiques\n\nProchaines étapes : Kubernetes, Docker Swarm."
        },
        ["Communication Professionnelle"] = new()
        {
            ["introduction"] = "La communication professionnelle est essentielle en entreprise.\n\nTypes de communication :\n- Écrite : emails, rapports, comptes-rendus\n- Orale : réunions, présentations\n- Non-verbale : langage corporel\n\nObjectif : transmettre un message clair et efficace.",
            ["variables"] = "Règles de communication écrite :\n\n1. Objet clair et concis\n2. Structure : intro, développement, conclusion\n3. Ton professionnel\n4. Relecture systématique\n5. Pièces jointes nommées clairement\n6. CC et CCI appropriés\n\nEmail type : Objet, Salutation, Corps, Formule de politesse.",
            ["conditions"] = "Communication orale en réunion :\n\nAVANT : Préparez un ordre du jour, définez les objectifs\nPENDANT : Soyez clair, écoutez, gérez le temps\nAPRÈS : Compte-rendu avec actions et responsables\n\nTechniques :\n- STAR (Situation, Task, Action, Result)\n- Pyramid Principle (conclusion d'abord)\n- 5W2H (What, Why, When, Where, Who, How, How much)",
            ["exercice1"] = "Exercice : Rédigez un email professionnel pour :\n\n1. Demander un report de deadline\n2. Présenter une nouvelle idée à votre manager\n3. Annoncer un retard à un client\n\nAppliquez la structure : Objet, Salutation, Corps, Politesse.",
            ["exercice2"] = "Exercice intermédiaire : Préparez une présentation de 10 minutes.\n\n1. Définissez le message clé\n2. Structurez en 3 parties\n3. Créez des slides efficaces\n4. Entraînez-vous à l'oral\n5. Gérez les questions",
            ["avance"] = "Communication avancée :\n\n1. Négociation ( méthode Harvard)\n2. Gestion de conflits\n3. Communication interculturelle\n4. Storytelling en entreprise\n5. Présentation à un public large\n6. Communication digitale (LinkedIn, Slack)",
            ["conclusion"] = "Récapitulatif Communication :\n\n✅ Email professionnel\n✅ Présentation orale\n✅ Réunions efficaces\n✅ Écoute active\n✅ Feedback constructif\n\nProchaines étapes : Formation continue, coaching."
        }
    };

    if (contents.TryGetValue(formationTitle, out var topics) && topics.TryGetValue(topic, out var content))
        return content;

    return $"Contenu de la leçon '{topic}' pour la formation '{formationTitle}'. " +
           $"Ce module couvre les aspects fondamentaux de {topic} dans le contexte de {formationTitle}. " +
           $"Les étudiants apprendront les concepts théoriques et pratiques nécessaires. " +
           $"Des exercices pratiques seront proposés pour consolider les acquis.";
}
