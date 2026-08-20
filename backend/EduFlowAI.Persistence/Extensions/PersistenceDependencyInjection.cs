using EduFlowAI.Admission.Application.DbContextAbstraction;
using EduFlowAI.AI.Application.DbContextAbstraction;
using EduFlowAI.Communication.Application.DbContextAbstraction;
using EduFlowAI.Documents.Application.DbContextAbstraction;
using EduFlowAI.Documents.Application.Interfaces;
using EduFlowAI.Identity.Application.DbContextAbstraction;
using EduFlowAI.Persistence.Messaging;
using EduFlowAI.Shared.Kernel.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using Wolverine.EntityFrameworkCore;

namespace EduFlowAI.Persistence.Extensions
{
    public static class PersistenceDependencyInjection
    {
        public static IServiceCollection AddPersistence( this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContextWithWolverineIntegration<EduFlowAIDbContext>(options =>
            {
                options.UseNpgsql(
                    configuration.GetConnectionString("DefaultConnection"),
                    npgsqlOptions =>
                    {
                        // Enable vector operations for Npgsql
                        npgsqlOptions.UseVector();
                    });
            });

            services.AddScoped<IIdentityDbContext>(sp =>
                sp.GetRequiredService<EduFlowAIDbContext>());

            services.AddScoped<IAdmissionDbContext>(sp =>
                sp.GetRequiredService<EduFlowAIDbContext>());

            services.AddScoped<IDocumentDbContext>(sp =>
                sp.GetRequiredService<EduFlowAIDbContext>());

            services.AddScoped<IAIDbContext>(sp =>
                sp.GetRequiredService<EduFlowAIDbContext>());

            services.AddScoped<ICommunicationDbContext>(sp =>
                sp.GetRequiredService<EduFlowAIDbContext>());

            // ensure (builder.ConfigureCommonMessaging) in program.cs is un-commented to use this outbox
            services.AddScoped<IDocumentVerificationOutbox, DocumentVerificationOutbox>();

            services.AddScoped<IOutboxPublisher, OutboxPublisher>();

            return services;
        }
    }
}
