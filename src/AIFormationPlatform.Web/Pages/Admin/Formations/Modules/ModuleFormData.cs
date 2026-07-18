using AIFormationPlatform.Core.Enums;

namespace AIFormationPlatform.Web.Pages.Admin.Formations.Modules;

public class ModuleFormData
{
    public string Titre { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Ordre { get; set; }
    public TypeModalite TypeModalite { get; set; } = TypeModalite.Cours;
    public string? ContenuTexte { get; set; }
    public string? EnonceExercice { get; set; }
    public string? ConsignesExercice { get; set; }
    public string? TitreQuiz { get; set; }
    public List<QuestionFormData> Questions { get; set; } = CreateQuestions();

    private static List<QuestionFormData> CreateQuestions() => [new(), new(), new()];
}

public class QuestionFormData
{
    public string? Enonce { get; set; }
    public int Points { get; set; } = 1;
    public List<ReponseFormData> Reponses { get; set; } = [new(), new(), new(), new()];
}

public class ReponseFormData
{
    public string? Texte { get; set; }
    public bool EstCorrecte { get; set; }
}
