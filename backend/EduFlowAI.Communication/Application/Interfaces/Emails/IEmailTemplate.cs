using EduFlowAI.Communication.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace EduFlowAI.Communication.Application.Interfaces.Emails
{
    public interface IEmailTemplate
    {
        EmailType Type { get; }
        object DeserializeData(string json);
        string BuildSubject(object? data);
        string BuildHtmlBody(object? data);
    }


}
