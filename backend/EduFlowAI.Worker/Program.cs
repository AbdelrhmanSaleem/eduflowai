using EduFlowAI.Admission;
using EduFlowAI.Admission.Application.Messages;
using EduFlowAI.AI;
using EduFlowAI.AI.Application.DocumentVerification.Messaging;
using EduFlowAI.Communication;
using EduFlowAI.Documents;
using EduFlowAI.Documents.Application.Messaging;
using EduFlowAI.Identity;
using EduFlowAI.Identity.Application.Interfaces;
using EduFlowAI.Identity.Application.Services;
using EduFlowAI.Identity.Domain.Entities;
using EduFlowAI.Persistence;
using EduFlowAI.Persistence.Extensions;
using EduFlowAI.Shared.Messaging.Contracts.Configuration;
using iText.StyledXmlParser.Jsoup.Nodes;
using JasperFx.CodeGeneration.Model;
using JasperFx.Resources;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Wolverine;
using Wolverine.RabbitMQ;

namespace EduFlowAI.Worker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);

            builder.Services.AddSerilog((services, loggerConfiguration) =>
            {
                loggerConfiguration
                    .ReadFrom.Configuration(builder.Configuration)
                    .ReadFrom.Services(services)
                    .Enrich.FromLogContext()
                    .Enrich.WithProperty("Application", "EduFlowAI.Worker");
            });

            builder.Services.AddPersistence(builder.Configuration);

            builder.Services.AddAdmissionWorkerModule();
            builder.Services.AddDocumentsWorkerModule();
            builder.Services.AddAIWorkerModule();
            builder.Services.AddCommunicationModule(builder.Configuration);

            //builder.Services
            //    .AddIdentityCore<AppUser>()
            //    .AddEntityFrameworkStores<EduFlowAIDbContext>();

            builder.Services.AddScoped<IUserContactInfoReader, UserContactInfoReader>();

            builder.ConfigureCommonMessaging("EduFlowAI.Worker");
            builder.Services.ConfigureWolverine(options =>
            {
                options.ServiceLocationPolicy = ServiceLocationPolicy.AllowedButWarn;

                options.Discovery.IncludeAssembly(
                    typeof(VerifyApplicantDocumentV1Handler).Assembly);
                options.Discovery.IncludeAssembly(
                    typeof(ApplicantDocumentVerificationCompletedV1Handler).Assembly);

                MessagingConfiguration.ConfigureWorkerListeners(options);

                options.ListenToRabbitQueue(AdmissionQueueNames.AdmissionStatusEmails);
            });

            // messaging dev only
            if (builder.Environment.IsDevelopment())
            {
                builder.Services.AddResourceSetupOnStartup();
            }


            var host = builder.Build();
            host.Run();
        }
    }
}
