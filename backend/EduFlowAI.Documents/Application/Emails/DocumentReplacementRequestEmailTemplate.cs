using EduFlowAI.Communication.Application.Services;
using EduFlowAI.Communication.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace EduFlowAI.Documents.Application.Emails
{
    public sealed record DocumentReplacementRequestEmailData(string DocumentType, string Reason);

    public sealed class DocumentReplacementRequestEmailTemplate : EmailTemplate<DocumentReplacementRequestEmailData>
    {
        public override EmailType Type => EmailType.DocumentReplacementRequested;

        protected override string BuildSubjectCore(DocumentReplacementRequestEmailData? data)
            => "Action needed: please re-upload your document";

        protected override string BuildHtmlBodyCore(DocumentReplacementRequestEmailData? data) 
            => $@"
                <html lang='ar'>
                  <body style='margin: 0; padding: 0; background-color: #f2f2f2; font-family: 'Cairo', Arial, sans-serif;'>
                    <table role='presentation' cellspacing='0' cellpadding='0' border='0' width='100%' style='background-color: #f2f2f2; padding: 40px 0;'>
                      <tr>
                        <td align='center'>
                          <table role='presentation' cellspacing='0' cellpadding='0' border='0' width='600' style='background: #ffffff; border-radius: 10px; overflow: hidden; box-shadow: 0 4px 12px rgba(0,0,0,0.1);'>
                            <tr>
                              <td style='background-color: #1E3A8A; padding: 30px; text-align: center; color: white;'>
                                <img src='../../../EduFlowAI.Api/wwwroot/Logo/edu-logo-light.png' alt='EduFlowAI Logo' style='width: 85px; height: auto; margin-bottom: 10px;' />
                              </td>
                            </tr>
                            <tr>
                              <td style='padding: 30px; text-align: left; color: #333333;'>
                                <p style='font-size: 20px;'>Dear Applicant,</p>
                                <p style='font-size: 20px;'>We need you to re-upload your</p>
                                <p style='font-size: 28px; font-weight: bold; color: #1E3A8A; text-align: left; margin: 30px 0;'>{data!.DocumentType}</p>
                                <p style='font-size: 16px;'><strong>Reason:</strong></p>
                                <ul style='font-size: 15px; line-height: 1.8; padding: 0; margin: 10px 0; list-style: none;'>
                                  <li style='display: flex; align-items: flex-start; margin-bottom: 8px;'>
                                    <span>{data!.Reason}</span>
                                  </li>
                                </ul>
                                <p style='margin-top: 40px; font-size: 16px;'>Please log in to EduFlowAI to upload the corrected document.</p>
                                <p font-size: 16px;'>Best regards.<br></p>
                              </td>
                            </tr>
                            <tr>
                              <td style='background-color: #2563EB; padding: 15px; text-align: center; font-size: 13px; color: #ffffff;'>
                                &copy; {DateTime.Now.Year} EduFlowAI, All rigts reserved.
                              </td>
                            </tr>
                          </table>
                        </td>
                      </tr>
                    </table>
                  </body>
                </html>";
    }
}