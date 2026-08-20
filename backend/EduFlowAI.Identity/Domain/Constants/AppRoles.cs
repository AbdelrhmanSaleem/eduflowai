namespace EduFlowAI.Identity.Domain.Constants;

public static class AppRoles
{
    public const string Applicant = "Applicant";
    public const string OperationsManager = "OperationsManager";
    public const string SuperAdmin = "SuperAdmin";

    public static readonly string[] All =
    [
        Applicant,
        OperationsManager,
        SuperAdmin
    ];
}