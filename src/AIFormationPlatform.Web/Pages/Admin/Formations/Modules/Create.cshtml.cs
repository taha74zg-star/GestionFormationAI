using AIFormationPlatform.Core.Entities;
using AIFormationPlatform.Core.Enums;
using AIFormationPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AIFormationPlatform.Web.Pages.Admin.Formations.Modules;

[Authorize(Roles = "Admin")]
public class CreateModel(ApplicationDbContext context) : PageModel
{
    [BindProperty]
    public ModuleFormData Form { get; set; } = new();
    public string FormationTitre { get; private set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync(int formationId)
    {
        var formation = await context.Formations.FindAsync(formationId);
        if (formation is null) return NotFound();
        FormationTitre = formation.Titre;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int formationId)
    {
        var formation = await context.Formations.FindAsync(formationId);
        if (formation is null) return NotFound();
        FormationTitre = formation.Titre;
        ValidateForm();
        if (!ModelState.IsValid) return Page();

        var module = new AIFormationPlatform.Core.Entities.Module
        {
            FormationId = formationId, Titre = Form.Titre, Description = Form.Description, Ordre = Form.Ordre,
            TypeModalite = Form.TypeModalite, ContenuTexte = Form.TypeModalite == TypeModalite.Cours ? Form.ContenuTexte : null
        };
        AddModalite(module);
        context.Modules.Add(module);
        await context.SaveChangesAsync();
        return RedirectToPage("Index", new { formationId });
    }

    private void ValidateForm()
    {
        if (string.IsNullOrWhiteSpace(Form.Titre)) ModelState.AddModelError("Form.Titre", "Le titre est requis.");
        if (Form.TypeModalite == TypeModalite.Cours && string.IsNullOrWhiteSpace(Form.ContenuTexte)) ModelState.AddModelError("Form.ContenuTexte", "Le contenu du cours est requis.");
        if (Form.TypeModalite == TypeModalite.Exercice && string.IsNullOrWhiteSpace(Form.EnonceExercice)) ModelState.AddModelError("Form.EnonceExercice", "L'énoncé de l'exercice est requis.");
        if (Form.TypeModalite == TypeModalite.Quiz && !Form.Questions.Any(q => !string.IsNullOrWhiteSpace(q.Enonce))) ModelState.AddModelError("Form.Questions", "Le quiz doit comporter au moins une question.");
    }

    private void AddModalite(AIFormationPlatform.Core.Entities.Module module)
    {
        if (Form.TypeModalite == TypeModalite.Exercice)
            module.Exercice = new Exercice { Enonce = Form.EnonceExercice!, Consignes = Form.ConsignesExercice };
        if (Form.TypeModalite == TypeModalite.Quiz)
            module.Quiz = BuildQuiz();
    }

    private Quiz BuildQuiz() => new()
    {
        Titre = string.IsNullOrWhiteSpace(Form.TitreQuiz) ? Form.Titre : Form.TitreQuiz,
        Questions = Form.Questions.Where(q => !string.IsNullOrWhiteSpace(q.Enonce)).Select((q, index) => new QuestionQuiz
        {
            Enonce = q.Enonce!, Type = QuestionType.Qcm, Points = Math.Max(1, q.Points), Ordre = index + 1,
            Reponses = q.Reponses.Where(r => !string.IsNullOrWhiteSpace(r.Texte)).Select(r => new ReponseQuiz { Texte = r.Texte!, EstCorrecte = r.EstCorrecte }).ToList()
        }).ToList()
    };
}
