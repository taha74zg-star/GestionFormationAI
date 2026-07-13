using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AIFormationPlatform.Core.Interfaces;
using AIFormationPlatform.Core.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AIFormationPlatform.Infrastructure.Services;

public class AnamAvatarService : IAvatarService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AnamOptions _options;
    private readonly ILogger<AnamAvatarService> _logger;

    public AnamAvatarService(
        IHttpClientFactory httpClientFactory,
        IOptions<AnamOptions> options,
        ILogger<AnamAvatarService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    // Implémentations par défaut pour la compatibilité avec la nouvelle interface
    public async Task<AvatarStartResult> StartSessionAsync(string script, AvatarPersonaConfig? personaOverride = null, CancellationToken cancellationToken = default)
    {
        var session = await CreateSessionTokenAsync(personaOverride, cancellationToken);
        if (!session.Success)
            return new AvatarStartResult(false, null, script, session.ErrorMessage);

        return new AvatarStartResult(true, session.SessionToken, null, null);
    }

    public async Task<AvatarInteractionResult> SendQuestionAsync(string sessionToken, string question, CancellationToken cancellationToken = default)
    {
        // Basic service does not support interaction endpoint; return fallback text
        return new AvatarInteractionResult(false, null, question, "Interaction non supportée par ce client de base.");
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.ApiKey);

    public async Task<AvatarSessionResult> CreateSessionTokenAsync(
        AvatarPersonaConfig? personaOverride = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
            return new AvatarSessionResult(false, null, "Clé API Anam non configurée.");

        var persona = MergePersona(personaOverride);

        try
        {
            var client = _httpClientFactory.CreateClient(nameof(AnamAvatarService));
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
