namespace AIFormationPlatform.Core.Options;

public class AnamOptions
{
    public const string SectionName = "Anam";

    public string ApiKey { get; set; } = string.Empty;
    public string AvatarId { get; set; } = "ccf00c0e-7302-455b-ace2-057e0cf58127";
    public string AvatarModel { get; set; } = "cara-4";
    public string VoiceId { get; set; } = "8f80e347-4fc0-11f1-84b0-52bacf74fa75";
    public string LlmId { get; set; } = "CUSTOMER_CLIENT_V1";
    public string PersonaName { get; set; } = "Assistant IA";
    public string DefaultSystemPrompt { get; set; } =
        "Tu es un assistant IA intelligent et amical. Réponds toujours dans la langue de l'utilisateur.";
    public string ApiBaseUrl { get; set; } = "https://api.anam.ai";
}
