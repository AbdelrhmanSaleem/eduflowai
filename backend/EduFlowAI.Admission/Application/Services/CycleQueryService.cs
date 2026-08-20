using EduFlowAI.Admission.Application.DTOs;
using EduFlowAI.Admission.Application.Interfaces.Repositories;
using EduFlowAI.Admission.Application.Interfaces.Services;
using EduFlowAI.Admission.Domain.Entities;
using EduFlowAI.Shared.Kernel.Common;
using Microsoft.EntityFrameworkCore;

namespace EduFlowAI.Admission.Application.Services
{
    public class CycleQueryService : ICycleQueryService
    {
        private readonly IGenericRepository<AdmissionCycle> _cycleRepository;

        public CycleQueryService(IGenericRepository<AdmissionCycle> cycleRepository)
        {
            _cycleRepository = cycleRepository;            
        }
        public async Task<Result<IEnumerable<ActiveAdmissionCycleDto>>> GetActiveCyclesAsync(CancellationToken cancellationToken = default)
        {
            var activeCycles = await _cycleRepository.GetAllAsync(
                predicate: c => c.Status == Domain.Enums.CycleStatus.Active && c.DeadlineUtc > DateTimeOffset.UtcNow,
                include: query => query.Include(c => c.Program)
            );

            if (activeCycles == null || !activeCycles.Any())
            {
                return Result<IEnumerable<ActiveAdmissionCycleDto>>.Success(
                    new List<ActiveAdmissionCycleDto>(),
                    statusCode: 200,
                    message: "No active cycles found at the moment."
                );
            }

            var cyclesDto = activeCycles.Select(c => new ActiveAdmissionCycleDto(
                CycleId: c.Id,
                ProgramName: c.Program.Name,
                ProgramCode: c.Program.Code,
                ProgramDescription: c.Program.Description,
                CycleLabel: c.Label,
                StartDate: c.StartDate,
                DeadlineUtc: c.DeadlineUtc
            )).ToList();

            return Result<IEnumerable<ActiveAdmissionCycleDto>>.Success(
                cyclesDto,
                statusCode: 200,
                message: "Active cycles with program details retrieved successfully."
            );
        }
    }
}
