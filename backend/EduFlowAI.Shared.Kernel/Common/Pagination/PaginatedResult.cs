using System;
using System.Collections.Generic;
using System.Text;

namespace EduFlowAI.Shared.Kernel.Common.Pagination
{
    public class PaginatedResult<T>
    {
        public IEnumerable<T> Data { get; set; } = new List<T>();
        public object? Filters { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
    }
}
