using EduFlowAI.Admission;
using EduFlowAI.Admission.Application.Messages;
using EduFlowAI.Admission.Infrastructure.Seeding;
using EduFlowAI.AI;
using EduFlowAI.Api.Middlewares;
using EduFlowAI.Communication;
using EduFlowAI.Communication.Infrastructure.Hubs;
using EduFlowAI.Documents;
using EduFlowAI.Identity;
using EduFlowAI.Persistence;
using EduFlowAI.Persistence.Extensions;
using EduFlowAI.Shared.Messaging.Contracts.Configuration;
using JasperFx.Resources;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi;
using Serilog;
using System.Threading.RateLimiting;
using Wolverine;
using Wolverine.RabbitMQ;

namespace EduFlowAI.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddSerilog((services, loggerConfiguration) =>
        {
            loggerConfiguration
                .ReadFrom.Configuration(builder.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", "EduFlowAI.Api");
        });

        builder.Services.AddControllers()
            .AddApplicationPart(typeof(IdentityModule).Assembly)
            .AddApplicationPart(typeof(AdmissionModule).Assembly)
            .AddApplicationPart(typeof(DocumentsModule).Assembly)
            .AddApplicationPart(typeof(AIModule).Assembly)
            .AddApplicationPart(typeof(CommunicationModule).Assembly);

        builder.Services.AddProblemDetails();
        builder.Services.AddExceptionHandler<ExceptionHandler>();
        builder.Services.AddOpenApi();
        builder.Services.AddEndpointsApiExplorer();

        var configuredOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins")
            .Get<string[]>();

        var allowedOrigins = configuredOrigins is { Length: > 0 }
            ? configuredOrigins
            : ["http://localhost:4200"];

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("Frontend", policy =>
            {
                policy.WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode =
                StatusCodes.Status429TooManyRequests;
            options.AddPolicy("Authentication", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    httpContext.Connection.RemoteIpAddress?.ToString()
                        ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        AutoReplenishment = true
                    }));
        });

        builder.Services.AddPersistence(builder.Configuration);

        var identityBuilder = builder.Services.AddIdentityModule(
            builder.Configuration);
        identityBuilder.AddEntityFrameworkStores<EduFlowAIDbContext>();
        builder.Services.AddAdmissionModule();
        builder.Services.AddDocumentsModule();
        builder.Services.AddAIModule(builder.Configuration);
        builder.Services.AddCommunicationModule(builder.Configuration);

        builder.ConfigureCommonMessaging("EduFlowAI.Api");
        builder.Services.ConfigureWolverine(options =>
        {
            MessagingConfiguration.ConfigureApiListeners(options);

            options.PublishMessage<SendAdmissionEmailCommand>()
                .ToRabbitQueue(AdmissionQueueNames.AdmissionStatusEmails);
        });

        if (builder.Environment.IsDevelopment())
        {
            builder.Services.AddResourceSetupOnStartup();
        }

        builder.Services.AddAdmissionDataSeeding();

        builder.Services.AddSwaggerGen(options =>
        {
            options.CustomSchemaIds(type =>
                type.FullName?.Replace("+", ".") ?? type.Name);

            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "EduFlowAI API",
                Version = "v1"
            });
            options.AddSecurityDefinition(
                "Bearer",
                new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Description =
                        "Enter the JWT access token without the Bearer prefix.",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = JwtBearerDefaults.AuthenticationScheme,
                    BearerFormat = "JWT"
                });
            options.AddSecurityRequirement(
                document => new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference(
                        "Bearer",
                        document)] = []
                });
        });

        builder.Services.AddSignalR();

        var app = builder.Build();

        app.UseExceptionHandler();

        app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
        });

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        else if (app.Environment.IsProduction())
        {
            app.MapOpenApi();
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseCors("Frontend");
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();


        app.MapHub<NotificationHub>("/hubs/notifications");

        app.MapControllers();
        app.Run();
    }
}
