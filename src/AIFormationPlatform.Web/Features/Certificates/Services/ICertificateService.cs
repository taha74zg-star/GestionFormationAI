namespace AIFormationPlatform.Web.Features.Certificates.Services;

public interface ICertificateService
{
    Task<bool> IsEligibleAsync(string userId, int formationId);
    Task<byte[]> GenerateCertificateAsync(string userId, int formationId);
}
