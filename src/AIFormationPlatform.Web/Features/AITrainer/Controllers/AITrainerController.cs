using System.Security.Claims;
using AIFormationPlatform.Web.Features.AITrainer.Services;
using AIFormationPlatform.Web.Features.AITrainer.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIFormationPlatform.Web.Features.AITrainer.Controllers;

[Authorize]
public class AITrainerController : Controller
{
    private readonly IAITrainerService _aiTrainerService;
    private readonly ILogger<AITrainerController> _logger;

    public AITrainerController(IAITrainerService aiTrainerService, ILogger<AITrainerController> logger)
    {
        _aiTrainerService = aiTrainerService;
        _logger = logger;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ask([FromBody] ChatRequestViewModel request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var response = await _aiTrainerService.AskQuestionAsync(
            userId,
            request.LessonId,
            request.Question,
            request.ConversationId);

        if (!response.IsSuccess)
        {
            return Json(new { success = false, message = response.ErrorMessage });
        }

        return Json(new
        {
            success = true,
            response = response.Response,
            conversationId = response.ConversationId
        });
    }
}
