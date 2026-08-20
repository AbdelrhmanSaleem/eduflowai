using EduFlowAI.Communication.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace EduFlowAI.Communication.Application.Interfaces.Emails
{
    public interface IEmailContentBuilder
    {
        (string Subject, string HtmlBody) Build(EmailType type, string? dataJson);
    }


    public sealed class EmailContentBuilder : IEmailContentBuilder
    {
        private readonly IReadOnlyDictionary<EmailType, IEmailTemplate> _templates;

        public EmailContentBuilder(IEnumerable<IEmailTemplate> templates)
            => _templates = templates.ToDictionary(t => t.Type);

        public (string Subject, string HtmlBody) Build(EmailType type, string? dataJson)
        {
            if (!_templates.TryGetValue(type, out var template))
                throw new InvalidOperationException($"No email template registered for '{type}'.");

            var data = !string.IsNullOrEmpty(dataJson) ? template.DeserializeData(dataJson!) : null;
            return (template.BuildSubject(data), template.BuildHtmlBody(data));
        }
    }
}
