namespace AIFormationPlatform.Web.Features.Chat;

public record ChatRequest(string SessionId, string Message, int? ModuleId = null);
public record ChatReply(string Reply, string SessionId);
