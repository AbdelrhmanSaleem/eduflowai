using EduFlowAI.Communication.Application.Interfaces.Emails;
using EduFlowAI.Communication.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace EduFlowAI.Communication.Application.Services
{
    public abstract class EmailTemplate<TData> : IEmailTemplate
    {
        public abstract EmailType Type { get; }

        public object DeserializeData(string json) => JsonSerializer.Deserialize<TData>(json)!;

        public string BuildSubject(object? data) => BuildSubjectCore((TData)data!);

        public string BuildHtmlBody(object? data) => BuildHtmlBodyCore((TData)data!);

        protected abstract string BuildSubjectCore(TData? data);

        protected abstract string BuildHtmlBodyCore(TData? data);
    }
}
