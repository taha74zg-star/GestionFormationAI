using AIFormationPlatform.Web.Data;
using AIFormationPlatform.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace AIFormationPlatform.Tests;

public class EnrollmentTests
{
    private AppDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task Enrollment_ShouldBeCreatedSuccessfully()
    {
        using var context = CreateInMemoryContext();

        var user = new ApplicationUser { UserName = "student@test.com", Email = "student@test.com", Role = "Student" };
        var formation = new Formation { Title = "Test Formation", Description = "Test", IsPublished = true };
        context.Users.Add(user);
        context.Formations.Add(formation);
        await context.SaveChangesAsync();

        var enrollment = new Enrollment
        {
            UserId = user.Id,
            FormationId = formation.Id,
            EnrolledAt = DateTime.UtcNow
        };
        context.Enrollments.Add(enrollment);
        await context.SaveChangesAsync();

        var saved = await context.Enrollments.FirstOrDefaultAsync(e => e.UserId == user.Id && e.FormationId == formation.Id);
        Assert.NotNull(saved);
        Assert.Equal(user.Id, saved.UserId);
        Assert.Equal(formation.Id, saved.FormationId);
    }

    [Fact]
    public async Task Formation_CascadeDelete_ShouldRemoveEnrollments()
    {
        using var context = CreateInMemoryContext();

        var user = new ApplicationUser { UserName = "test@test.com", Email = "test@test.com", Role = "Student" };
        var formation = new Formation { Title = "Test", Description = "Test", IsPublished = true };
        context.Users.Add(user);
        context.Formations.Add(formation);
        await context.SaveChangesAsync();

        context.Enrollments.Add(new Enrollment { UserId = user.Id, FormationId = formation.Id });
        await context.SaveChangesAsync();

        context.Formations.Remove(formation);
        await context.SaveChangesAsync();

        var enrollments = await context.Enrollments.Where(e => e.FormationId == formation.Id).ToListAsync();
        Assert.Empty(enrollments);
    }
}
