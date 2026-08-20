using System;
using System.Collections.Generic;
using System.Text;

namespace EduFlowAI.Admission.Application.DTOs
{
    public record ApplicationRequestDto(Guid CycleId, List<PreferenceDto> Preferences);
}
