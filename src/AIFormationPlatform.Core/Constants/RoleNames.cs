namespace AIFormationPlatform.Core.Constants;

public static class RoleNames
{
    public const string Admin = "Admin";
    public const string Formateur = "Formateur";
    public const string Apprenant = "Apprenant";

    public static readonly IReadOnlyList<string> All = [Admin, Formateur, Apprenant];
}
