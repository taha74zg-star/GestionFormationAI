using AIFormationPlatform.Web.Features.AITrainer.Prompts;

namespace AIFormationPlatform.Tests;

public class AITrainerPromptTests
{
    [Fact]
    public void GetSystemPrompt_ShouldContainLessonContent()
    {
        var content = "Le C# est un langage orienté objet.";
        var prompt = AITrainerPrompts.GetSystemPrompt(content);

        Assert.Contains(content, prompt);
        Assert.Contains("Formateur IA", prompt);
    }

    [Fact]
    public void GetSystemPrompt_ShouldContainStrictRules()
    {
        var prompt = AITrainerPrompts.GetSystemPrompt("test");

        Assert.Contains("RÈGLES STRICTES", prompt);
        Assert.Contains("N'inventez", prompt);
    }

    [Fact]
    public void GetUserPrompt_ShouldContainQuestion()
    {
        var question = "Qu'est-ce que le polymorphisme ?";
        var prompt = AITrainerPrompts.GetUserPrompt(question);

        Assert.Contains(question, prompt);
    }
}
