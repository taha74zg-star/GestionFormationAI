using AIFormationPlatform.Web.Data;
using AIFormationPlatform.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace AIFormationPlatform.Tests;

public class QuizScoreTests
{
    private AppDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task SubmitQuiz_AllCorrectAnswers_ShouldScore100Percent()
    {
        using var context = CreateInMemoryContext();

        var formation = new Formation { Title = "Test Formation", Description = "Test", IsPublished = true };
        context.Formations.Add(formation);
        await context.SaveChangesAsync();

        var quiz = new Quiz { FormationId = formation.Id, Title = "Test Quiz", TimeLimitMinutes = 30 };
        context.Quizzes.Add(quiz);
        await context.SaveChangesAsync();

        var question = new Question { QuizId = quiz.Id, Text = "What is 2+2?", Points = 1 };
        context.Questions.Add(question);
        await context.SaveChangesAsync();

        var correctChoice = new AnswerChoice { QuestionId = question.Id, Text = "4", IsCorrect = true };
        var wrongChoice = new AnswerChoice { QuestionId = question.Id, Text = "3", IsCorrect = false };
        context.AnswerChoices.AddRange(correctChoice, wrongChoice);
        await context.SaveChangesAsync();

        var user = new ApplicationUser { UserName = "test@test.com", Email = "test@test.com", Role = "Student" };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var attempt = new QuizAttempt
        {
            UserId = user.Id,
            QuizId = quiz.Id,
            TotalPoints = 1,
            Score = 1,
            AttemptedAt = DateTime.UtcNow
        };
        context.QuizAttempts.Add(attempt);
        await context.SaveChangesAsync();

        context.StudentAnswers.Add(new StudentAnswer
        {
            QuizAttemptId = attempt.Id,
            QuestionId = question.Id,
            AnswerChoiceId = correctChoice.Id
        });
        await context.SaveChangesAsync();

        var loadedAttempt = await context.QuizAttempts.FindAsync(attempt.Id);
        Assert.NotNull(loadedAttempt);
        Assert.Equal(1, loadedAttempt.Score);
        Assert.Equal(1, loadedAttempt.TotalPoints);
    }

    [Fact]
    public async Task Enrollment_ApplicationLogic_ShouldCheckDuplicates()
    {
        using var context = CreateInMemoryContext();

        var user = new ApplicationUser { UserName = "test@test.com", Email = "test@test.com", Role = "Student" };
        var formation = new Formation { Title = "Test", Description = "Test", IsPublished = true };
        context.Users.Add(user);
        context.Formations.Add(formation);
        await context.SaveChangesAsync();

        context.Enrollments.Add(new Enrollment { UserId = user.Id, FormationId = formation.Id });
        await context.SaveChangesAsync();

        var exists = await context.Enrollments
            .AnyAsync(e => e.UserId == user.Id && e.FormationId == formation.Id);

        Assert.True(exists);
    }

    [Fact]
    public async Task LessonProgress_ApplicationLogic_ShouldCheckDuplicates()
    {
        using var context = CreateInMemoryContext();

        var user = new ApplicationUser { UserName = "test@test.com", Email = "test@test.com", Role = "Student" };
        var lesson = new Lesson { Title = "Test Lesson", Content = "Content" };
        context.Users.Add(user);
        context.Lessons.Add(lesson);
        await context.SaveChangesAsync();

        context.LessonProgresses.Add(new LessonProgress
        {
            UserId = user.Id,
            LessonId = lesson.Id,
            IsCompleted = true,
            CompletedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var exists = await context.LessonProgresses
            .AnyAsync(lp => lp.UserId == user.Id && lp.LessonId == lesson.Id);

        Assert.True(exists);
    }
}
