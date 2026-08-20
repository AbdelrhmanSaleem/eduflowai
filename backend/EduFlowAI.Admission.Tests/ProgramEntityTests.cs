using AdmissionProgram = EduFlowAI.Admission.Domain.Entities.Program;

namespace EduFlowAI.Admission.Tests;

public sealed class ProgramEntityTests
{
    [Fact]
    public void New_program_uses_an_empty_description_instead_of_null()
    {
        var program = new AdmissionProgram();

        Assert.Equal(string.Empty, program.Description);
    }
}
