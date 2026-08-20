using System;
using System.Collections.Generic;
using System.Text;

namespace EduFlowAI.Admission.Domain.Enums
{
    public enum EligibilityStatus
    {
        /// <summary>
        /// Default state when the application is just submitted
        /// </summary>
        Pending = 0,

        /// <summary>
        /// Preference meets all rules for the track
        /// </summary>
        Eligible = 1,

        /// <summary>
        /// Preference failed one or more rules
        /// </summary>
        Rejected = 2
    }
}
