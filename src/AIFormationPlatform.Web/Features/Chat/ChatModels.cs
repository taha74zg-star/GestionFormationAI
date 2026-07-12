namespace AIFormationPlatform.Web.Features.Chat;

public record ChatRequest(string SessionId, string Message);
public record ChatReply(string Reply, string SessionId);
