using AIFormationPlatform.Web.Data;
using AIFormationPlatform.Web.Features.Certificates.Services;
using AIFormationPlatform.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace AIFormationPlatform.Tests;

public class CertificateTests
{
    private AppDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task IsEligible_AllLessonsCompleted_ShouldReturnTrue()
    {
        using var context = CreateInMemoryContext();
        var service = new CertificateService(context);

        var user = new ApplicationUser { UserName = "test@test.com", Email = "test@test.com", FirstName = "Jean", LastName = "Dupont", Role = "Student" };
        context.Users.Add(user);

        var formation = new Formation { Title = "Cours Test", Description = "Description", IsPublished = true };
        context.Formations.Add(formation);
        await context.SaveChangesAsync();

        var module = new Module { FormationId = formation.Id, Title = "Module 1", SortOrder = 1 };
        context.Modules.Add(module);
        await context.SaveChangesAsync();

        var lesson1 = new Lesson { ModuleId = module.Id, Title = "Leçon 1", Content = "Contenu 1", SortOrder = 1 };
        var lesson2 = new Lesson { ModuleId = module.Id, Title = "Leçon 2", Content = "Contenu 2", SortOrder = 2 };
        context.Lessons.AddRange(lesson1, lesson2);
        await context.SaveChangesAsync();

        context.Enrollments.Add(new Enrollment { UserId = user.Id, FormationId = formation.Id });
        await context.SaveChangesAsync();

        context.LessonProgresses.AddRange(
            new LessonProgress { UserId = user.Id, LessonId = lesson1.Id, IsCompleted = true, CompletedAt = DateTime.UtcNow },
            new LessonProgress { UserId = user.Id, LessonId = lesson2.Id, IsCompleted = true, CompletedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var result = await service.IsEligibleAsync(user.Id, formation.Id);
        Assert.True(result);
    }

    [Fact]
    public async Task IsEligible_PartialCompletion_ShouldReturnFalse()
    {
        using var context = CreateInMemoryContext();
        var service = new CertificateService(context);

        var user = new ApplicationUser { UserName = "test@test.com", Email = "test@test.com", FirstName = "Jean", LastName = "Dupont", Role = "Student" };
        context.Users.Add(user);

        var formation = new Formation { Title = "Cours Test", Description = "Description", IsPublished = true };
        context.Formations.Add(formation);
        await context.SaveChangesAsync();

        var module = new Module { FormationId = formation.Id, Title = "Module 1", SortOrder = 1 };
        context.Modules.Add(module);
        await context.SaveChangesAsync();

        var lesson1 = new Lesson { ModuleId = module.Id, Title = "Leçon 1", Content = "Contenu 1", SortOrder = 1 };
        var lesson2 = new Lesson { ModuleId = module.Id, Title = "Leçon 2", Content = "Contenu 2", SortOrder = 2 };
        context.Lessons.AddRange(lesson1, lesson2);
        await context.SaveChangesAsync();

        context.Enrollments.Add(new Enrollment { UserId = user.Id, FormationId = formation.Id });
        await context.SaveChangesAsync();

        context.LessonProgresses.Add(
            new LessonProgress { UserId = user.Id, LessonId = lesson1.Id, IsCompleted = true, CompletedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var result = await service.IsEligibleAsync(user.Id, formation.Id);
        Assert.False(result);
    }

    [Fact]
    public async Task IsEligible_NotEnrolled_ShouldReturnFalse()
    {
        using var context = CreateInMemoryContext();
        var service = new CertificateService(context);

        var user = new ApplicationUser { UserName = "test@test.com", Email = "test@test.com", FirstName = "Jean", LastName = "Dupont", Role = "Student" };
        context.Users.Add(user);

        var formation = new Formation { Title = "Cours Test", Description = "Description", IsPublished = true };
        context.Formations.Add(formation);
        await context.SaveChangesAsync();

        var result = await service.IsEligibleAsync(user.Id, formation.Id);
        Assert.False(result);
    }

    [Fact]
    public async Task IsEligible_NoLessonsInFormation_ShouldReturnFalse()
    {
        using var context = CreateInMemoryContext();
        var service = new CertificateService(context);

        var user = new ApplicationUser { UserName = "test@test.com", Email = "test@test.com", FirstName = "Jean", LastName = "Dupont", Role = "Student" };
        context.Users.Add(user);

        var formation = new Formation { Title = "Cours Test", Description = "Description", IsPublished = true };
        context.Formations.Add(formation);
        await context.SaveChangesAsync();

        context.Enrollments.Add(new Enrollment { UserId = user.Id, FormationId = formation.Id });
        await context.SaveChangesAsync();

        var result = await service.IsEligibleAsync(user.Id, formation.Id);
        Assert.False(result);
    }

    [Fact]
    public async Task GenerateCertificate_EligibleUser_ShouldReturnPdfBytes()
    {
        using var context = CreateInMemoryContext();
        var service = new CertificateService(context);

        var user = new ApplicationUser { UserName = "test@test.com", Email = "test@test.com", FirstName = "Jean", LastName = "Dupont", Role = "Student" };
        context.Users.Add(user);

        var formation = new Formation { Title = "Cours Test", Description = "Description", IsPublished = true };
        context.Formations.Add(formation);
        await context.SaveChangesAsync();

        var module = new Module { FormationId = formation.Id, Title = "Module 1", SortOrder = 1 };
        context.Modules.Add(module);
        await context.SaveChangesAsync();

        var lesson = new Lesson { ModuleId = module.Id, Title = "Leçon 1", Content = "Contenu 1", SortOrder = 1 };
        context.Lessons.Add(lesson);
        await context.SaveChangesAsync();

        context.Enrollments.Add(new Enrollment { UserId = user.Id, FormationId = formation.Id });
        await context.SaveChangesAsync();

        context.LessonProgresses.Add(
            new LessonProgress { UserId = user.Id, LessonId = lesson.Id, IsCompleted = true, CompletedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var pdfBytes = await service.GenerateCertificateAsync(user.Id, formation.Id);

        Assert.NotNull(pdfBytes);
        Assert.NotEmpty(pdfBytes);
        Assert.True(pdfBytes.Length > 100);
    }

    [Fact]
    public async Task GenerateCertificate_NotEligible_ShouldThrowInvalidOperationException()
    {
        using var context = CreateInMemoryContext();
        var service = new CertificateService(context);

        var user = new ApplicationUser { UserName = "test@test.com", Email = "test@test.com", FirstName = "Jean", LastName = "Dupont", Role = "Student" };
        context.Users.Add(user);

        var formation = new Formation { Title = "Cours Test", Description = "Description", IsPublished = true };
        context.Formations.Add(formation);
        await context.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GenerateCertificateAsync(user.Id, formation.Id));
    }
}
