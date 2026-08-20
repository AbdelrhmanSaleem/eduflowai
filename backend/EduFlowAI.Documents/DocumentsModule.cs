using Amazon.S3;
using EduFlowAI.Admission.Application.Interfaces.Services;
using EduFlowAI.Admission.Application.Services;
using EduFlowAI.Communication.Application.Interfaces.Emails;
using EduFlowAI.Documents.Application.DTOs;
using EduFlowAI.Documents.Application.Emails;
using EduFlowAI.Documents.Application.Interfaces;
using EduFlowAI.Documents.Application.Services;
using EduFlowAI.Documents.Application.Validators;
using EduFlowAI.Documents.Infrastructure.Auth;
using EduFlowAI.Documents.Infrastructure.Storage;
using EduFlowAI.Identity.Application.Interfaces;
using EduFlowAI.Identity.Application.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace EduFlowAI.Documents
{
    public static class DocumentsModule
    {
        public static IServiceCollection AddDocumentsModule(this IServiceCollection services)
        {
            // Bind DocumentStorage options from appsettings.json
            services.AddOptions<DocumentStorageOptions>()
                .BindConfiguration(DocumentStorageOptions.SectionName);

            //services.AddAWSService<IAmazonS3>();

            // Application + Infrastructure services registrations go here later
            services.AddScoped<IFileStorageService, FileStorageService>();
            services.AddScoped<IReplacementRequestService, ReplacementRequestService>();
            services.AddScoped<IDocumentSubmissionService, DocumentSubmissionService>();
            services.AddScoped<IDocumentHumanReviewService, DocumentHumanReviewService>();
            services.AddScoped<ICurrentUserService, HttpCurrentUserService>();
            services.AddScoped<IApplicantDocumentService, ApplicantDocumentService>();
            services.AddScoped<IValidator<UploadDocumentDto>, UploadDocumentDtoValidator>();
            services.AddScoped<IProgramRequirementReader, ProgramRequirementReader>();
            services.AddScoped<IApplicationAcessReader, ApplicationAcessReader>();

            services.AddScoped<IProfileService, ProfileService>();

            services.AddScoped<IDocumentVerificationQueryService, DocumentVerificationQueryService>();

            return services;
        }

        public static IServiceCollection AddDocumentsWorkerModule(this IServiceCollection services)
        {
            // Bind DocumentStorage options from appsettings.json
            services.AddOptions<DocumentStorageOptions>()
                .BindConfiguration(DocumentStorageOptions.SectionName);

            //services.AddAWSService<IAmazonS3>();

            services.AddScoped<IEmailTemplate, DocumentReplacementRequestEmailTemplate>();

            // Needed by AI's document verification handler (file reader + expected-field lookup).
            services.AddScoped<IFileStorageService, FileStorageService>();
            services.AddScoped<IProfileService, ProfileService>();

            return services;
        }

        // Called only by Api — Worker never calls this
        // public static IEndpointRouteBuilder MapDocumentsPresentation(this IEndpointRouteBuilder app) { ... }
    }
}

