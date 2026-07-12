using AIFormationPlatform.Web.Data;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AIFormationPlatform.Web.Features.Certificates.Services;

public class CertificateService : ICertificateService
{
    private readonly AppDbContext _context;

    public CertificateService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<bool> IsEligibleAsync(string userId, int formationId)
    {
        var enrollment = await _context.Enrollments
            .FirstOrDefaultAsync(e => e.UserId == userId && e.FormationId == formationId);

        if (enrollment == null) return false;

        var formation = await _context.Formations
            .Include(f => f.Modules)
                .ThenInclude(m => m.Lessons)
            .FirstOrDefaultAsync(f => f.Id == formationId);

        if (formation == null) return false;

        var totalLessons = formation.Modules.SelectMany(m => m.Lessons).Count();
        if (totalLessons == 0) return false;

        var completedLessons = await _context.LessonProgresses
            .Where(lp => lp.UserId == userId
                && lp.Lesson.Module.FormationId == formationId
                && lp.IsCompleted)
            .CountAsync();

        return completedLessons == totalLessons;
    }

    public async Task<byte[]> GenerateCertificateAsync(string userId, int formationId)
    {
        var eligible = await IsEligibleAsync(userId, formationId);
        if (!eligible)
            throw new InvalidOperationException("L'étudiant n'est pas éligible au certificat.");

        var user = await _context.Users.FindAsync(userId)
            ?? throw new InvalidOperationException("Utilisateur introuvable.");

        var formation = await _context.Formations.FindAsync(formationId)
            ?? throw new InvalidOperationException("Formation introuvable.");

        var completionDate = await _context.LessonProgresses
            .Where(lp => lp.UserId == userId
                && lp.Lesson.Module.FormationId == formationId
                && lp.IsCompleted)
            .MaxAsync(lp => lp.CompletedAt) ?? DateTime.UtcNow;

        var certificateId = Guid.NewGuid().ToString("N")[..12].ToUpper();

        QuestPDF.Settings.License = LicenseType.Community;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(60);

                page.Header().Element(header =>
                {
                    header.AlignCenter().Text("AI Formation Platform")
                        .FontSize(14)
                        .Bold()
                        .FontColor(Color.FromHex("#2c3e50"));
                });

                page.Content().Element(content =>
                {
                    content.PaddingVertical(40).Column(column =>
                    {
                        column.Item().AlignCenter().Text("Certificat de Réussite")
                            .FontSize(32)
                            .Bold()
                            .FontColor(Color.FromHex("#2c3e50"));

                        column.Item().PaddingTop(20).AlignCenter().Text("Ce certificat est délivré à")
                            .FontSize(14)
                            .FontColor(Color.FromHex("#7f8c8d"));

                        column.Item().PaddingTop(10).AlignCenter().Text($"{user.FirstName} {user.LastName}")
                            .FontSize(24)
                            .Bold()
                            .FontColor(Color.FromHex("#2980b9"));

                        column.Item().PaddingTop(20).AlignCenter().Text("pour avoir terminé avec succès la formation")
                            .FontSize(14)
                            .FontColor(Color.FromHex("#7f8c8d"));

                        column.Item().PaddingTop(10).AlignCenter().Text(formation.Title)
                            .FontSize(20)
                            .Bold()
                            .FontColor(Color.FromHex("#2c3e50"));

                        column.Item().PaddingTop(30).AlignCenter().Text($"Date de complétion : {completionDate:dd/MM/yyyy}")
                            .FontSize(12)
                            .FontColor(Color.FromHex("#7f8c8d"));

                        column.Item().PaddingTop(5).AlignCenter().Text($"Référence : {certificateId}")
                            .FontSize(10)
                            .FontColor(Color.FromHex("#95a5a6"));
                    });
                });

                page.Footer().Element(footer =>
                {
                    footer.AlignCenter().Text("AI Formation Plateforme — Certificat généré automatiquement")
                        .FontSize(8)
                        .FontColor(Color.FromHex("#bdc3c7"));
                });
            });
        }).GeneratePdf();

        return document;
    }
}
