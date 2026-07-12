using System.Security.Claims;
using AIFormationPlatform.Web.Data;
using AIFormationPlatform.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AIFormationPlatform.Web.Features.Enrollments.Controllers;

[Authorize]
public class ProgressController : Controller
{
    private readonly AppDbContext _context;

    public ProgressController(AppDbContext context) => _context = context;

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkLessonComplete(int lessonId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var exists = await _context.LessonProgresses
            .AnyAsync(lp => lp.UserId == userId && lp.LessonId == lessonId);

        if (!exists)
        {
            var progress = new LessonProgress
            {
                UserId = userId,
                LessonId = lessonId,
                IsCompleted = true,
                CompletedAt = DateTime.UtcNow
            };

            _context.LessonProgresses.Add(progress);
            await _context.SaveChangesAsync();
        }

        var lesson = await _context.Lessons
            .Include(l => l.Module)
            .FirstOrDefaultAsync(l => l.Id == lessonId);

        if (lesson != null)
        {
            return RedirectToAction("Progress", "Enrollments", new { formationId = lesson.Module.FormationId });
        }

        return RedirectToAction("MyFormations", "Enrollments");
    }
}
