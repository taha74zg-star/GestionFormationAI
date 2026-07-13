namespace AIFormationPlatform.Core.Interfaces;

public interface IAvatarService
{
    bool IsConfigured { get; }

    Task<AvatarSessionResult> CreateSessionTokenAsync(
        AvatarPersonaConfig? personaOverride = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Démarre une session d'avatar pour un script donné. Retourne un token de session
    /// (pour usage côté client) ou, en cas d'indisponibilité, fournit un fallback texte.
    /// </summary>
    Task<AvatarStartResult> StartSessionAsync(string script, AvatarPersonaConfig? personaOverride = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Envoie une question audio/texte vers la session avatar et récupère la réponse.
    /// Peut retourner une URL audio ou une réponse texte selon la disponibilité.
    /// </summary>
    Task<AvatarInteractionResult> SendQuestionAsync(string sessionToken, string question, CancellationToken cancellationToken = default);
}

public record AvatarPersonaConfig(
    string Name,
    string AvatarId,
    string AvatarModel,
    string VoiceId,
    string LlmId,
    string SystemPrompt);

public record AvatarSessionResult(bool Success, string? SessionToken, string? ErrorMessage);

public record AvatarStartResult(bool Success, string? SessionToken, string? FallbackText, string? ErrorMessage);

public record AvatarInteractionResult(bool Success, string? AudioUrl, string? TextResponse, string? ErrorMessage);
