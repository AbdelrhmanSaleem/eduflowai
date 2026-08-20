using System;
using System.Collections.Generic;
using System.Text;

namespace EduFlowAI.Admission.Application.DTOs
{
    /// <summary>
    /// DTO for updating ONLY the preferences (Separation of Concerns).
    /// </summary>
    /// <param name="Preferences"></param>
    public record UpdateApplicationPreferencesDto(
        List<PreferenceDto> Preferences
    );
}
