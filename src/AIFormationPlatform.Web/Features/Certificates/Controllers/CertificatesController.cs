using System.Security.Claims;
using AIFormationPlatform.Web.Features.Certificates.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIFormationPlatform.Web.Features.Certificates.Controllers;

[Authorize]
public class CertificatesController : Controller
{
    private readonly ICertificateService _certificateService;
    private readonly ILogger<CertificatesController> _logger;

    public CertificatesController(ICertificateService certificateService, ILogger<CertificatesController> logger)
    {
        _certificateService = certificateService;
        _logger = logger;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Download(int formationId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var eligible = await _certificateService.IsEligibleAsync(userId, formationId);
        if (!eligible)
        {
            _logger.LogWarning("Tentative de téléchargement de certificat non éligible: User {UserId}, Formation {FormationId}", userId, formationId);
            return Forbid();
        }

        try
        {
            var pdfBytes = await _certificateService.GenerateCertificateAsync(userId, formationId);
            return File(pdfBytes, "application/pdf", "certificat.pdf");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Erreur lors de la génération du certificat: User {UserId}, Formation {FormationId}", userId, formationId);
            return RedirectToAction("Progress", "Enrollments", new { formationId });
        }
    }
}
