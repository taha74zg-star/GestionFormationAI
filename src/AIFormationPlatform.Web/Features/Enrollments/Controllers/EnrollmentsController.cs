using System.Security.Claims;
using AIFormationPlatform.Web.Data;
using AIFormationPlatform.Web.Features.Enrollments.ViewModels;
using AIFormationPlatform.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AIFormationPlatform.Web.Features.Enrollments.Controllers;

[Authorize]
public class EnrollmentsController : Controller
{
    private readonly AppDbContext _context;
    private readonly ILogger<EnrollmentsController> _logger;

    public EnrollmentsController(AppDbContext context, ILogger<EnrollmentsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IActionResult> MyFormations()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var enrollments = await _context.Enrollments
            .Where(e => e.UserId == userId)
            .Include(e => e.Formation)
            .OrderByDescending(e => e.EnrolledAt)
            .ToListAsync();

        var viewModels = new List<EnrollmentViewModel>();

        foreach (var enrollment in enrollments)
        {
            var formation = await _context.Formations
                .Include(f => f.Modules)
                    .ThenInclude(m => m.Lessons)
                .FirstAsync(f => f.Id == enrollment.FormationId);

            var totalLessons = formation.Modules.SelectMany(m => m.Lessons).Count();
            var completedLessons = await _context.LessonProgresses
                .Where(lp => lp.UserId == userId && lp.Lesson.Module.FormationId == enrollment.FormationId && lp.IsCompleted)
                .CountAsync();

            viewModels.Add(new EnrollmentViewModel
            {
                Id = enrollment.Id,
                FormationId = enrollment.FormationId,
                FormationTitle = formation.Title,
                FormationDescription = formation.Description,
                EnrolledAt = enrollment.EnrolledAt,
                ProgressPercentage = totalLessons > 0 ? Math.Round((double)completedLessons / totalLessons * 100, 1) : 0,
                CompletedLessons = completedLessons,
                TotalLessons = totalLessons
            });
        }

        return View(viewModels);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enroll(int formationId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var exists = await _context.Enrollments
            .AnyAsync(e => e.UserId == userId && e.FormationId == formationId);

        if (exists)
        {
            _logger.LogWarning("Tentative d'inscription dupliquée: User {UserId}, Formation {FormationId}", userId, formationId);
            return RedirectToAction("MyFormations");
        }

        var formation = await _context.Formations.FindAsync(formationId);
        if (formation == null) return NotFound();

        var enrollment = new Enrollment
        {
            UserId = userId,
            FormationId = formationId,
            EnrolledAt = DateTime.UtcNow
        };

        _context.Enrollments.Add(enrollment);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Inscription créée: User {UserId}, Formation {FormationId}", userId, formationId);
        return RedirectToAction("MyFormations");
    }

    public async Task<IActionResult> Progress(int formationId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var enrollment = await _context.Enrollments
            .FirstOrDefaultAsync(e => e.UserId == userId && e.FormationId == formationId);

        if (enrollment == null) return RedirectToAction("MyFormations");

        var formation = await _context.Formations
            .Include(f => f.Modules.OrderBy(m => m.SortOrder))
                .ThenInclude(m => m.Lessons.OrderBy(l => l.SortOrder))
            .FirstAsync(f => f.Id == formationId);

        var totalLessons = formation.Modules.SelectMany(m => m.Lessons).Count();
        var completedLessons = await _context.LessonProgresses
            .Where(lp => lp.UserId == userId && lp.Lesson.Module.FormationId == formationId && lp.IsCompleted)
            .CountAsync();

        var completedLessonIds = await _context.LessonProgresses
            .Where(lp => lp.UserId == userId && lp.IsCompleted)
            .Select(lp => lp.LessonId)
            .ToListAsync();

        var viewModel = new ProgressViewModel
        {
            FormationId = formationId,
            FormationTitle = formation.Title,
            ProgressPercentage = totalLessons > 0 ? Math.Round((double)completedLessons / totalLessons * 100, 1) : 0,
            CompletedLessons = completedLessons,
            TotalLessons = totalLessons,
            Modules = formation.Modules.Select(m => new ModuleProgressViewModel
            {
                Id = m.Id,
                Title = m.Title,
                Lessons = m.Lessons.Select(l => new LessonProgressItemViewModel
                {
                    Id = l.Id,
                    Title = l.Title,
                    IsCompleted = completedLessonIds.Contains(l.Id)
                }).ToList()
            }).ToList()
        };

        return View(viewModel);
    }
}
