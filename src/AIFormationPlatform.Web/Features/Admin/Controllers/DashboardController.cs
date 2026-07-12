using AIFormationPlatform.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AIFormationPlatform.Web.Features.Admin.Controllers;

[Authorize(Roles = "Administrator")]
public class DashboardController : Controller
{
    private readonly AppDbContext _context;

    public DashboardController(AppDbContext context) => _context = context;

    public async Task<IActionResult> Index()
    {
        var viewModel = new ViewModels.DashboardViewModel
        {
            TotalFormations = await _context.Formations.CountAsync(),
            TotalStudents = await _context.Users.CountAsync(u => u.Role == "Student"),
            TotalEnrollments = await _context.Enrollments.CountAsync(),
            PublishedFormations = await _context.Formations.CountAsync(f => f.IsPublished)
        };

        return View(viewModel);
    }
}
