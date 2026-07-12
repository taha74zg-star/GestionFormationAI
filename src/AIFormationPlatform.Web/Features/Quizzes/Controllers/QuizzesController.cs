using System.Security.Claims;
using AIFormationPlatform.Web.Data;
using AIFormationPlatform.Web.Features.Quizzes.ViewModels;
using AIFormationPlatform.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AIFormationPlatform.Web.Features.Quizzes.Controllers;

[Authorize]
public class QuizzesController : Controller
{
    private readonly AppDbContext _context;
    private readonly ILogger<QuizzesController> _logger;

    public QuizzesController(AppDbContext context, ILogger<QuizzesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Index(int formationId)
    {
        var formation = await _context.Formations.FindAsync(formationId);
        if (formation == null) return NotFound();

        var quizzes = await _context.Quizzes
            .Where(q => q.FormationId == formationId)
            .Include(q => q.Questions)
            .ToListAsync();

        ViewBag.FormationId = formationId;
        ViewBag.FormationTitle = formation.Title;

        return View(quizzes);
    }

    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Create(int formationId)
    {
        var formation = await _context.Formations.FindAsync(formationId);
        if (formation == null) return NotFound();

        ViewBag.FormationId = formationId;
        ViewBag.FormationTitle = formation.Title;

        return View(new QuizFormViewModel
        {
            FormationId = formationId,
            TimeLimitMinutes = 30
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> Create(QuizFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var formation = await _context.Formations.FindAsync(model.FormationId);
            ViewBag.FormationId = model.FormationId;
            ViewBag.FormationTitle = formation?.Title ?? "";
            return View(model);
        }

        var quiz = new Quiz
        {
            FormationId = model.FormationId,
            Title = model.Title,
            TimeLimitMinutes = model.TimeLimitMinutes
        };

        _context.Quizzes.Add(quiz);
        await _context.SaveChangesAsync();

        foreach (var q in model.Questions)
        {
            if (string.IsNullOrWhiteSpace(q.Text)) continue;

            var question = new Question
            {
                QuizId = quiz.Id,
                Text = q.Text,
                Points = q.Points
            };

            _context.Questions.Add(question);
            await _context.SaveChangesAsync();

            foreach (var ac in q.AnswerChoices)
            {
                if (string.IsNullOrWhiteSpace(ac.Text)) continue;

                _context.AnswerChoices.Add(new AnswerChoice
                {
                    QuestionId = question.Id,
                    Text = ac.Text,
                    IsCorrect = ac.IsCorrect
                });
            }
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Quiz créé: {Title} pour la formation {FormationId}", model.Title, model.FormationId);
        return RedirectToAction(nameof(Index), new { formationId = model.FormationId });
    }

    public async Task<IActionResult> Take(int id)
    {
        var quiz = await _context.Quizzes
            .Include(q => q.Questions)
                .ThenInclude(q => q.AnswerChoices)
            .FirstOrDefaultAsync(q => q.Id == id);

        if (quiz == null) return NotFound();

        var model = new QuizTakeViewModel
        {
            Id = quiz.Id,
            FormationId = quiz.FormationId,
            Title = quiz.Title,
            TimeLimitMinutes = quiz.TimeLimitMinutes,
            Questions = quiz.Questions.Select(q => new QuestionTakeViewModel
            {
                Id = q.Id,
                Text = q.Text,
                Points = q.Points,
                AnswerChoices = q.AnswerChoices.Select(ac => new AnswerChoiceTakeViewModel
                {
                    Id = ac.Id,
                    Text = ac.Text
                }).ToList()
            }).ToList()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(int quizId, Dictionary<int, int> answers)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var quiz = await _context.Quizzes
            .Include(q => q.Questions)
                .ThenInclude(q => q.AnswerChoices)
            .FirstOrDefaultAsync(q => q.Id == quizId);

        if (quiz == null) return NotFound();

        int score = 0;
        int totalPoints = quiz.Questions.Sum(q => q.Points);

        var attempt = new QuizAttempt
        {
            UserId = userId,
            QuizId = quizId,
            TotalPoints = totalPoints,
            AttemptedAt = DateTime.UtcNow
        };

        _context.QuizAttempts.Add(attempt);
        await _context.SaveChangesAsync();

        foreach (var question in quiz.Questions)
        {
            int selectedChoiceId = answers.ContainsKey(question.Id) ? answers[question.Id] : 0;

            var selectedChoice = await _context.AnswerChoices.FindAsync(selectedChoiceId);
            bool isCorrect = selectedChoice?.IsCorrect ?? false;

            if (isCorrect) score += question.Points;

            _context.StudentAnswers.Add(new StudentAnswer
            {
                QuizAttemptId = attempt.Id,
                QuestionId = question.Id,
                AnswerChoiceId = selectedChoiceId > 0 ? selectedChoiceId : 0
            });
        }

        attempt.Score = score;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Quiz soumis: User {UserId}, Quiz {QuizId}, Score {Score}/{TotalPoints}", userId, quizId, score, totalPoints);

        return RedirectToAction("Result", new { attemptId = attempt.Id });
    }

    public async Task<IActionResult> Result(int attemptId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var attempt = await _context.QuizAttempts
            .Include(qa => qa.Quiz)
            .Include(qa => qa.StudentAnswers)
                .ThenInclude(sa => sa.Question)
            .Include(qa => qa.StudentAnswers)
                .ThenInclude(sa => sa.AnswerChoice)
            .FirstOrDefaultAsync(qa => qa.Id == attemptId && qa.UserId == userId);

        if (attempt == null) return NotFound();

        var correctAnswers = await _context.AnswerChoices
            .Where(ac => ac.IsCorrect && ac.Question.QuizId == attempt.QuizId)
            .Select(ac => new { ac.QuestionId, ac.Text })
            .ToListAsync();

        var correctMap = correctAnswers.ToDictionary(c => c.QuestionId, c => c.Text);

        var viewModel = new QuizResultViewModel
        {
            QuizId = attempt.QuizId,
            QuizTitle = attempt.Quiz.Title,
            Score = attempt.Score,
            TotalPoints = attempt.TotalPoints,
            Percentage = attempt.TotalPoints > 0 ? Math.Round((double)attempt.Score / attempt.TotalPoints * 100, 1) : 0,
            AttemptedAt = attempt.AttemptedAt,
            Questions = attempt.StudentAnswers.Select(sa => new QuestionResultViewModel
            {
                Text = sa.Question.Text,
                Points = sa.Question.Points,
                SelectedAnswer = sa.AnswerChoice?.Text ?? "Pas de réponse",
                CorrectAnswer = correctMap.TryGetValue(sa.QuestionId, out var correct) ? correct : "N/A",
                IsCorrect = sa.AnswerChoice?.IsCorrect ?? false
            }).ToList()
        };

        return View(viewModel);
    }

    [Authorize(Roles = "Administrator")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, int formationId)
    {
        var quiz = await _context.Quizzes.FindAsync(id);
        if (quiz == null) return NotFound();

        _context.Quizzes.Remove(quiz);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Quiz supprimé: {Id}", id);
        return RedirectToAction(nameof(Index), new { formationId });
    }
}
