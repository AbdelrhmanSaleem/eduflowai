using System;
using System.Collections.Generic;
using System.Text;

namespace EduFlowAI.Shared.Kernel.Common.Pagination
{
    public class QueryParameters
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        public string? Search { get; set; }
        public string? Status { get; set; }
        public string? Type { get; set; }
    }
}
