using EduFlowAI.Admission.Application.DTOs;
using EduFlowAI.Shared.Kernel.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace EduFlowAI.Admission.Application.Interfaces.Services
{
    public interface ICycleQueryService
    {
        /// <summary>
        /// Retrieves a list of currently active admission cycles.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<Result<IEnumerable<ActiveAdmissionCycleDto>>> GetActiveCyclesAsync(CancellationToken cancellationToken = default);
    }
}
