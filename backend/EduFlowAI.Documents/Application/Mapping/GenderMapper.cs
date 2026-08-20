using System;
using System.Collections.Generic;
using System.Text;
using EduFlowAI.Admission.Domain.Enums;

namespace EduFlowAI.Documents.Application.Mapping
{

    public static class GenderMapper
    {
        public static Gender? ToGender(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            return Enum.TryParse<Gender>(
                value,
                ignoreCase: true,
                out var gender)
                    ? gender
                    : null;
        }
    }
}
