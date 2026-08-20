using EduFlowAI.Shared.Messaging.Contracts.Communication.V1;
using EduFlowAI.Shared.Messaging.Contracts.Documents.V1;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Text;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.Postgresql;
using Wolverine.RabbitMQ;

namespace EduFlowAI.Shared.Messaging.Contracts.Configuration
{
    public static class MessagingConfiguration
    {
        public static void ConfigureCommonMessaging(this IHostApplicationBuilder builder, string hostName)
        {
            try
            {
                var databaseConnection = builder.Configuration.GetConnectionString("DefaultConnection");
                var rabbitConnection = builder.Configuration.GetConnectionString("RabbitMq");
                var useRabbitMq =builder.Configuration.GetValue<bool>("Messaging:Enabled");

                builder.UseWolverine(options =>
                {
                    options.ServiceName = hostName;

                    options.PersistMessagesWithPostgresql(databaseConnection!, schemaName: "messaging");
                    options.UseEntityFrameworkCoreTransactions();

                    options.Policies.UseDurableOutboxOnAllSendingEndpoints();
                    options.Policies.UseDurableInboxOnAllListeners();

                    options.PublishFaultEvents(
                        includeExceptionMessage: false,
                        includeStackTrace: false);

                    var rmq = options.UseRabbitMq(rabbitConnection!)
                        .EnableEnhancedDeadLettering();

                    ConfigureSendingRoutes(options);

                    if (useRabbitMq)
                    {
                        rmq.AutoProvision();
                    }
                    else
                    {
                        // stop wolverine from trying to connect to RabbitMQ and just stub out the transports for testing
                        options.StubAllExternalTransports();
                    }
                });
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to configure messaging RabbitMQ or DB connection is missing.", ex);
            }
        }

        private static void ConfigureSendingRoutes(WolverineOptions options)
        {
            options.PublishMessage<VerifyApplicantDocumentV1>()
                .ToRabbitQueue(MessagingQueueNames.DocumentVerification);

            options.PublishMessage<ApplicantDocumentVerificationCompletedV1>()
                .ToRabbitQueue(MessagingQueueNames.ApplyDocumentVerificationResult);

            options.PublishMessage<ApplicantDocumentVerificationFailedV1>()
                .ToRabbitQueue(MessagingQueueNames.ApplyDocumentVerificationFailure);

            options.PublishMessage<ApplicantDocumentAutoApprovedV1>()
                .ToRabbitQueue(MessagingQueueNames.DocumentStatusNotifications);

            options.PublishMessage<ApplicantDocumentNeedsHumanReviewV1>()
                .ToRabbitQueue(MessagingQueueNames.DocumentStatusNotifications);

            options.PublishMessage<DocumentReplacementFulfilledV1>()
                .ToRabbitQueue(MessagingQueueNames.DocumentReplacementFulfilled);

            options.PublishMessage<SendEmailV1>()
                .ToRabbitQueue(MessagingQueueNames.SendEmail);

            //options.PublishMessage<CreateNotificationV1>()
            //    .ToRabbitQueue(MessagingQueueNames.CreateNotification);


            options.PublishMessage<NotificationCreatedV1>()
                .ToRabbitQueue(MessagingQueueNames.RealtimeNotification);
        }

        public static void ConfigureWorkerListeners(WolverineOptions options)
        {
            options.ListenToRabbitQueue(MessagingQueueNames.DocumentVerification);
            options.ListenToRabbitQueue(MessagingQueueNames.ApplyDocumentVerificationResult);
            options.ListenToRabbitQueue(MessagingQueueNames.ApplyDocumentVerificationFailure);
            options.ListenToRabbitQueue(MessagingQueueNames.DocumentStatusNotifications);
            options.ListenToRabbitQueue(MessagingQueueNames.DocumentReplacementFulfilled);
            options.ListenToRabbitQueue(MessagingQueueNames.SendEmail);
            //options.ListenToRabbitQueue(MessagingQueueNames.CreateNotification);
        }

        public static void ConfigureApiListeners(WolverineOptions options)
        {
            options.ListenToRabbitQueue(MessagingQueueNames.RealtimeNotification);
        }
    }
}
