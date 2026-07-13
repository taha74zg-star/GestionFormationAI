using AIFormationPlatform.Infrastructure.Identity;
using AIFormationPlatform.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AIFormationPlatform.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Formateur> Formateurs => Set<Formateur>();
    public DbSet<Categorie> Categories => Set<Categorie>();
    public DbSet<Formation> Formations => Set<Formation>();
    public DbSet<Module> Modules => Set<Module>();
    public DbSet<ContenuCours> ContenusCours => Set<ContenuCours>();
    public DbSet<Exercice> Exercices => Set<Exercice>();
    public DbSet<Quiz> Quiz => Set<Quiz>();
    public DbSet<QuestionQuiz> QuestionsQuiz => Set<QuestionQuiz>();
    public DbSet<ReponseQuiz> ReponsesQuiz => Set<ReponseQuiz>();
    public DbSet<Inscription> Inscriptions => Set<Inscription>();
    public DbSet<Progression> Progressions => Set<Progression>();
    public DbSet<TentativeQuiz> TentativesQuiz => Set<TentativeQuiz>();
    public DbSet<ReponseTentative> ReponsesTentative => Set<ReponseTentative>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(u => u.Prenom).HasMaxLength(100);
            entity.Property(u => u.Nom).HasMaxLength(100);
            entity.Property(u => u.PhotoUrl).HasMaxLength(500);
        });

        builder.Entity<Formateur>(entity =>
        {
            entity.Property(f => f.Nom).HasMaxLength(100);
            entity.Property(f => f.Prenom).HasMaxLength(100);
            entity.Property(f => f.Specialites).HasMaxLength(500);
            entity.Property(f => f.PhotoUrl).HasMaxLength(500);
            entity.HasIndex(f => f.UserId);
        });

        builder.Entity<Categorie>(entity =>
        {
            entity.Property(c => c.Nom).HasMaxLength(150);
            entity.HasOne(c => c.Parent)
                .WithMany(c => c.Enfants)
                .HasForeignKey(c => c.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Formation entity mapping updated to new schema (categorie stored as text)
        builder.Entity<Formation>(entity =>
        {
            entity.Property(f => f.Titre).HasMaxLength(200);
            entity.Property(f => f.Categorie).HasMaxLength(150);
            entity.Property(f => f.Niveau).HasMaxLength(50);
            entity.Property(f => f.ImageUrl).HasMaxLength(500);
            entity.Property(f => f.DateCreation).HasDefaultValueSql("GETUTCDATE()");
        });

        builder.Entity<Module>(entity =>
        {
            entity.Property(m => m.Titre).HasMaxLength(200);
            entity.HasIndex(m => new { m.FormationId, m.Ordre }).IsUnique();
            entity.HasOne(m => m.Formation)
                .WithMany(f => f.Modules)
                .HasForeignKey(m => m.FormationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ContenuCours>(entity =>
        {
            entity.HasOne(c => c.Module)
                .WithMany(m => m.Contenus)
                .HasForeignKey(c => c.ModuleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Exercice>(entity =>
        {
            entity.HasOne(e => e.Module)
                .WithOne(m => m.Exercice)
                .HasForeignKey<Exercice>(e => e.ModuleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Quiz>(entity =>
        {
            entity.Property(q => q.Titre).HasMaxLength(200);
            entity.HasOne(q => q.Module)
                .WithOne(m => m.Quiz)
                .HasForeignKey<Quiz>(q => q.ModuleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<QuestionQuiz>(entity =>
        {
            entity.HasOne(q => q.Quiz)
                .WithMany(q => q.Questions)
                .HasForeignKey(q => q.QuizId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ReponseQuiz>(entity =>
        {
            entity.Property(r => r.Texte).HasMaxLength(500);
            entity.HasOne(r => r.Question)
                .WithMany(q => q.Reponses)
                .HasForeignKey(r => r.QuestionQuizId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Inscription>(entity =>
        {
            entity.HasIndex(i => new { i.UserId, i.FormationId }).IsUnique();
            entity.HasOne(i => i.Formation)
                .WithMany(f => f.Inscriptions)
                .HasForeignKey(i => i.FormationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Progression>(entity =>
        {
            entity.HasIndex(p => new { p.UserId, p.ModuleId }).IsUnique();
            entity.HasOne(p => p.Module)
                .WithMany(m => m.Progressions)
                .HasForeignKey(p => p.ModuleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<TentativeQuiz>(entity =>
        {
            entity.HasOne(t => t.Quiz)
                .WithMany(q => q.Tentatives)
                .HasForeignKey(t => t.QuizId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ReponseTentative>(entity =>
        {
            entity.HasOne(r => r.Tentative)
                .WithMany(t => t.Reponses)
                .HasForeignKey(r => r.TentativeQuizId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(r => r.Question)
                .WithMany()
                .HasForeignKey(r => r.QuestionQuizId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(r => r.ReponseSelectionnee)
                .WithMany()
                .HasForeignKey(r => r.ReponseQuizId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
