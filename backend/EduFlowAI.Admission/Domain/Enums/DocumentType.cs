namespace EduFlowAI.Admission.Domain.Enums
{
    // Fixed vocabulary for the required documents in the verification pipeline
    public enum DocumentType
    {
        None = 0,
        NationalId = 1,
        BirthCertificate = 2,
        GraduationCertificate = 3,
        MilitaryCertificate = 4
    }
}