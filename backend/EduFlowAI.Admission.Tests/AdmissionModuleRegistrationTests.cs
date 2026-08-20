using EduFlowAI.Admission;
using EduFlowAI.Admission.Application.Features.Branches;
using EduFlowAI.Admission.Application.Features.Cycles;
using EduFlowAI.Admission.Application.Features.Dashboard;
using EduFlowAI.Admission.Application.Features.Offerings;
using EduFlowAI.Admission.Application.Features.Programs;
using EduFlowAI.Admission.Application.Features.Requirements;
using EduFlowAI.Admission.Application.Features.Tracks;
using EduFlowAI.Admission.Application.Interfaces.Services;
using EduFlowAI.Admission.Application.Services;
using EduFlowAI.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EduFlowAI.Admission.Tests;

public sealed class AdmissionModuleRegistrationTests
{
    public static TheoryData<Type, Type> AdmissionFeatureRegistrations =>
        new()
        {
            { typeof(IProgramConfigurationService), typeof(ProgramConfigurationService) },
            { typeof(IProgramRequirementService), typeof(ProgramRequirementService) },
            { typeof(ITrackService), typeof(TrackService) },
            { typeof(IBranchService), typeof(BranchService) },
            { typeof(IAdmissionCycleService), typeof(AdmissionCycleService) },
            { typeof(IOfferingService), typeof(OfferingService) },
            { typeof(IAdmissionDashboardService), typeof(AdmissionDashboardService) }
        };

    [Theory]
    [MemberData(nameof(AdmissionFeatureRegistrations))]
    public void AddAdmissionModule_registers_each_configuration_feature_exactly_once(
        Type serviceType,
        Type implementationType)
    {
        var services = new ServiceCollection();

        services.AddAdmissionModule();

        var registration = Assert.Single(
            services,
            descriptor => descriptor.ServiceType == serviceType);

        Assert.Equal(ServiceLifetime.Scoped, registration.Lifetime);
        Assert.Equal(implementationType, registration.ImplementationType);
    }

    [Fact]
    public void AddAdmissionModule_registers_requirement_resolution_and_time()
    {
        var services = new ServiceCollection();

        services.AddAdmissionModule();

        var requirementRegistration = Assert.Single(
            services,
            descriptor =>
                descriptor.ServiceType == typeof(IProgramRequirementReader));

        Assert.Equal(
            typeof(ProgramRequirementReader),
            requirementRegistration.ImplementationType);

        Assert.Single(
            services,
            descriptor => descriptor.ServiceType == typeof(TimeProvider));
    }

    [Fact]
    public void Admission_and_AI_modules_keep_the_real_offered_track_reader()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        services.AddAdmissionModule();
        services.AddAIModule(configuration);

        var registration = Assert.Single(
            services,
            descriptor =>
                descriptor.ServiceType == typeof(IOfferedTrackReader));

        Assert.Equal(ServiceLifetime.Scoped, registration.Lifetime);
        Assert.Equal(
            typeof(OfferedTrackReader),
            registration.ImplementationType);
    }
}
