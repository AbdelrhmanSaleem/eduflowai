namespace EduFlowAI.Admission.Application.Interfaces.Services
{
    public interface IFinalRankingService
    {
        /// <summary>
        /// Executes the ranking algorithm.
        /// </summary>
        /// <param name="admittedCapacity"></param>
        /// <param name="waitlistCapacity"></param>
        /// <returns></returns>
        Task<(bool IsSuccess, string? ErrorMessage, int AdmittedCount, int WaitlistedCount)> ExecuteRankingAsync(int admittedCapacity, int waitlistCapacity);
    }
}
