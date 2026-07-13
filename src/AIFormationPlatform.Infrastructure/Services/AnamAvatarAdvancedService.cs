using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AIFormationPlatform.Core.Interfaces;
using AIFormationPlatform.Core.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AIFormationPlatform.Infrastructure.Services;

/// <summary>
/// Service d'intégration avancé pour Anam (start session, interactions audio/text).
/// Utilise AnamOptions pour la configuration (API key, avatar id, voice id).
/// Fournit un fallback texte si le service est indisponible.
/// </summary>
public class AnamAvatarAdvancedService : IAvatarService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AnamOptions _options;
    private readonly ILogger<AnamAvatarAdvancedService> _logger;

    public AnamAvatarAdvancedService(
        IHttpClientFactory httpClientFactory,
        IOptions<AnamOptions> options,
        ILogger<AnamAvatarAdvancedService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.ApiKey);

    public async Task<AvatarSessionResult> CreateSessionTokenAsync(AvatarPersonaConfig? personaOverride = null, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
            return new AvatarSessionResult(false, null, "Clé API Anam non configurée.");

        var persona = MergePersona(personaOverride);
        try
        {
            var client = _httpClientFactory.CreateClient(nameof(AnamAvatarAdvancedService));
            var payload = new
            {
                personaConfig = new
                {
                    name = persona.Name,
                    avatarId = persona.AvatarId,
                    avatarModel = persona.AvatarModel,
                    voiceId = persona.VoiceId,
                    llmId = persona.LlmId,
                    systemPrompt = persona.SystemPrompt
                }
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"{_options.ApiBaseUrl.TrimEnd('/')}/v1/auth/session-token")
            {
                Content = content
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

            var response = await client.SendAsync(request, cancellationToken);
            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Anam session-token failed: {Status} {Body}", response.StatusCode, responseJson);
                return new AvatarSessionResult(false, null, "Impossible de créer la session avatar.");
            }

            var tokenData = JsonSerializer.Deserialize<JsonElement>(responseJson);
            var sessionToken = tokenData.GetProperty("sessionToken").GetString();

            return string.IsNullOrEmpty(sessionToken)
                ? new AvatarSessionResult(false, null, "Token de session avatar invalide.")
                : new AvatarSessionResult(true, sessionToken, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur de connexion au service avatar Anam");
            return new AvatarSessionResult(false, null, "Erreur de connexion au service avatar.");
        }
    }

    public async Task<AvatarStartResult> StartSessionAsync(string script, AvatarPersonaConfig? personaOverride = null, CancellationToken cancellationToken = default)
    {
        // Essaye de créer un token de session et de fournir une URL/session ready
        var session = await CreateSessionTokenAsync(personaOverride, cancellationToken);
        if (!session.Success)
        {
            // fallback texte : retourne le script en tant que FallbackText
            return new AvatarStartResult(false, null, script, session.ErrorMessage);
        }

        // Ici on pourrait appeler l'API pour initialiser le script sur la session si l'API le nécessite.
        // Pour la plupart des intégrations, le client frontend enverra le script avec le token.
        return new AvatarStartResult(true, session.SessionToken, null, null);
    }

    public async Task<AvatarInteractionResult> SendQuestionAsync(string sessionToken, string question, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
            return new AvatarInteractionResult(false, null, question, "Service Anam non configuré.");

        try
        {
            var client = _httpClientFactory.CreateClient(nameof(AnamAvatarAdvancedService));
            var payload = new
            {
                sessionToken,
                input = new
                {
                    text = question
                }
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.ApiBaseUrl.TrimEnd('/')}/v1/session/interact")
            {
                Content = content
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

            var response = await client.SendAsync(request, cancellationToken);
            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Anam interact failed: {Status} {Body}", response.StatusCode, responseJson);
                return new AvatarInteractionResult(false, null, question, "Erreur lors de l'interaction avec l'avatar.");
            }

            var doc = JsonSerializer.Deserialize<JsonElement>(responseJson);
            // On cherche une URL audio dans la réponse (ex: output.audio.url) sinon text
            string? audioUrl = null;
            string? textResp = null;
            if (doc.TryGetProperty("output", out var output))
            {
                if (output.TryGetProperty("audio", out var audio) && audio.ValueKind == JsonValueKind.Object)
                {
                    if (audio.TryGetProperty("url", out var urlProp))
                        audioUrl = urlProp.GetString();
                }

                if (output.TryGetProperty("text", out var textProp))
                    textResp = textProp.GetString();
            }

            return new AvatarInteractionResult(true, audioUrl, textResp, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur interaction Anam");
            return new AvatarInteractionResult(false, null, question, "Erreur de communication avec le service avatar.");
        }
    }

    private AvatarPersonaConfig BuildDefaultPersona() => new(
        _options.PersonaName,
        _options.AvatarId,
        _options.AvatarModel,
        _options.VoiceId,
        _options.LlmId,
        _options.DefaultSystemPrompt);

    private AvatarPersonaConfig MergePersona(AvatarPersonaConfig? overrideConfig)
    {
        var defaults = BuildDefaultPersona();
        if (overrideConfig is null)
            return defaults;

        return new AvatarPersonaConfig(
            string.IsNullOrWhiteSpace(overrideConfig.Name) ? defaults.Name : overrideConfig.Name,
            string.IsNullOrWhiteSpace(overrideConfig.AvatarId) ? defaults.AvatarId : overrideConfig.AvatarId,
            string.IsNullOrWhiteSpace(overrideConfig.AvatarModel) ? defaults.AvatarModel : overrideConfig.AvatarModel,
            string.IsNullOrWhiteSpace(overrideConfig.VoiceId) ? defaults.VoiceId : overrideConfig.VoiceId,
            string.IsNullOrWhiteSpace(overrideConfig.LlmId) ? defaults.LlmId : overrideConfig.LlmId,
            string.IsNullOrWhiteSpace(overrideConfig.SystemPrompt) ? defaults.SystemPrompt : overrideConfig.SystemPrompt);
    }
}
