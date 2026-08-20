using System;
using System.Collections.Generic;
using System.Text;

namespace EduFlowAI.Admission.Domain.Entities
{
    public class ApplicationPreference
    {
        public Guid Id { get; set; }
        public short Rank { get; set; }

        public Guid ApplicationId { get; set; }
        public Guid TrackBranchOfferingId { get; set; }

        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }

        public Application Application { get; set; } = null!;
        public TrackBranchOffering TrackBranchOffering { get; set; } = null!;
    }
}
