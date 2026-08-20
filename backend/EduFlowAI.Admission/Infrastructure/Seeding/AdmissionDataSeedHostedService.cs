using Microsoft.Extensions.Hosting;

namespace EduFlowAI.Admission.Infrastructure.Seeding;

internal sealed class AdmissionDataSeedHostedService(
    IServiceProvider serviceProvider) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken) =>
        serviceProvider.SeedAdmissionDataAsync(cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken) =>
        Task.CompletedTask;
}
