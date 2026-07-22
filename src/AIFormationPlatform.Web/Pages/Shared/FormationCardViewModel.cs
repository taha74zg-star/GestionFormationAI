namespace AIFormationPlatform.Web.Pages.Shared;

public sealed record FormationCardViewModel(
    string Titre,
    string Categorie,
    string Niveau,
    string? ImageUrl,
    string? Formateur,
    int Progression,
    string Href);
