namespace EduFlowAI.Admission.Application.Interfaces.Services
{
    public interface IAllocationService
    {
        /// <summary>
        /// Runs the allocation engine for a specific admission cycle.
        /// Distributes eligible applicants to tracks based on their scores, preferences, and available capacity.
        /// </summary>
        /// <param name="cycleId">The unique identifier of the admission cycle.</param>
        /// <returns>A tuple indicating success or failure along with a message.</returns>
        Task<(bool IsSuccess, string ErrorMessage)> RunAllocationAsync(
            Guid cycleId,
            CancellationToken cancellationToken = default);
    }
}
