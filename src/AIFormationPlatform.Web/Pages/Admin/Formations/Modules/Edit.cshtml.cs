using AIFormationPlatform.Core.Entities;
using AIFormationPlatform.Core.Enums;
using AIFormationPlatform.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AIFormationPlatform.Web.Pages.Admin.Formations.Modules;

[Authorize(Roles = "Admin")]
public class EditModel(ApplicationDbContext context) : PageModel
{
    [BindProperty]
    public ModuleFormData Form { get; set; } = new();
    public string FormationTitre { get; private set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync(int formationId, int id)
    {
        var module = await GetModuleAsync(formationId, id);
        if (module is null) return NotFound();
        FormationTitre = module.Formation.Titre;
        Form = ToForm(module);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int formationId, int id)
    {
        var module = await GetModuleAsync(formationId, id);
        if (module is null) return NotFound();
        FormationTitre = module.Formation.Titre;
        ValidateForm();
        if (!ModelState.IsValid) return Page();

        module.Titre = Form.Titre;
        module.Description = Form.Description;
        module.Ordre = Form.Ordre;
        module.TypeModalite = Form.TypeModalite;
        module.ContenuTexte = Form.TypeModalite == TypeModalite.Cours ? Form.ContenuTexte : null;
        if (module.Exercice is not null) context.Exercices.Remove(module.Exercice);
        if (module.Quiz is not null) context.Quiz.Remove(module.Quiz);
        module.Exercice = null;
        module.Quiz = null;
        if (Form.TypeModalite == TypeModalite.Exercice)
            module.Exercice = new Exercice { Enonce = Form.EnonceExercice!, Consignes = Form.ConsignesExercice };
        if (Form.TypeModalite == TypeModalite.Quiz)
            module.Quiz = BuildQuiz();
        await context.SaveChangesAsync();
        return RedirectToPage("Index", new { formationId });
    }

    private async Task<AIFormationPlatform.Core.Entities.Module?> GetModuleAsync(int formationId, int id) =>
        await context.Modules.Include(m => m.Formation).Include(m => m.Exercice).Include(m => m.Quiz).ThenInclude(q => q!.Questions).ThenInclude(q => q.Reponses)
            .SingleOrDefaultAsync(m => m.Id == id && m.FormationId == formationId);

    private static ModuleFormData ToForm(AIFormationPlatform.Core.Entities.Module module) => new()
    {
        Titre = module.Titre, Description = module.Description, Ordre = module.Ordre, TypeModalite = module.TypeModalite,
        ContenuTexte = module.ContenuTexte, EnonceExercice = module.Exercice?.Enonce, ConsignesExercice = module.Exercice?.Consignes,
        TitreQuiz = module.Quiz?.Titre,
        Questions = module.Quiz?.Questions.OrderBy(q => q.Ordre).Select(q => new QuestionFormData
        {
            Enonce = q.Enonce, Points = q.Points,
            Reponses = q.Reponses.Select(r => new ReponseFormData { Texte = r.Texte, EstCorrecte = r.EstCorrecte }).ToList()
        }).ToList() ?? [new(), new(), new()]
    };

    private void ValidateForm()
    {
        if (string.IsNullOrWhiteSpace(Form.Titre)) ModelState.AddModelError("Form.Titre", "Le titre est requis.");
        if (Form.TypeModalite == TypeModalite.Cours && string.IsNullOrWhiteSpace(Form.ContenuTexte)) ModelState.AddModelError("Form.ContenuTexte", "Le contenu du cours est requis.");
        if (Form.TypeModalite == TypeModalite.Exercice && string.IsNullOrWhiteSpace(Form.EnonceExercice)) ModelState.AddModelError("Form.EnonceExercice", "L'énoncé de l'exercice est requis.");
        if (Form.TypeModalite == TypeModalite.Quiz && !Form.Questions.Any(q => !string.IsNullOrWhiteSpace(q.Enonce))) ModelState.AddModelError("Form.Questions", "Le quiz doit comporter au moins une question.");
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
