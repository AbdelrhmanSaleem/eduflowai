using EduFlowAI.Communication.Application.Interfaces;
using EduFlowAI.Communication.Application.Interfaces.Emails;
using EduFlowAI.Communication.Application.Services;
using EduFlowAI.Communication.Infrastructure.ExternalServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace EduFlowAI.Communication
{
    public static class CommunicationModule
    {
        public static IServiceCollection AddCommunicationModule(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IEmailDispatcher, EmailDispatcher>();
            services.AddScoped<IEmailContentBuilder, EmailContentBuilder>();
            services.AddScoped<IEmailSender, EmailSender>();

            services.AddSingleton(new SmtpSettings(
                Host: configuration["Smtp:Host"]!, Port: int.Parse(configuration["Smtp:Port"]!), 
                Password: configuration["Smtp:Password"]!, FromAddress: configuration["Smtp:FromAddress"]!, 
                FromName: configuration["Smtp:FromName"]!));

            return services;
        }

        // Called only by Api — Worker never calls this
        // public static IEndpointRouteBuilder MapCommunicationPresentation(this IEndpointRouteBuilder app) { ... }
    }
}
