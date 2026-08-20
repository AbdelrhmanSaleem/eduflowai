using FluentValidation;
using EduFlowAI.Admission.Domain.Enums;

using EduFlowAI.Documents.Application.DTOs;

namespace EduFlowAI.Documents.Application.Validators;

public class UploadDocumentDtoValidator : AbstractValidator<UploadDocumentDto>
{
    public UploadDocumentDtoValidator()
    {
        RuleFor(x => x.File)
            .NotNull().WithMessage("The file cannot be null.")
            .Must(file => file.Length > 0).WithMessage("The file cannot be empty.");

        RuleFor(x => x.DocumentType)
            .IsInEnum().WithMessage("The specified document type is invalid.")
            .NotEqual(DocumentType.None).WithMessage("A valid document type must be provided.");
    }
}