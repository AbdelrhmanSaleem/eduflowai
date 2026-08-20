namespace EduFlowAI.Identity.Infrastructure.Options;

public sealed class BootstrapSuperAdminOptions
{
    public const string SectionName = "BootstrapSuperAdmin";

    public bool Enabled { get; init; }

    public string Email { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;
}
