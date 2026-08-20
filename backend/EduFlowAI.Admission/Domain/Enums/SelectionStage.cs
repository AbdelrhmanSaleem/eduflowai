using System.ComponentModel.DataAnnotations;

namespace EduFlowAI.Admission.Domain.Enums
{
    public enum SelectionStage
    {
        [Display(Name = "None", Description = "No stage selected.")]
        None = 0,

        [Display(Name = "English & IQ Exam", Description = "Standardized assessment of reading, writing, comprehension, and logical reasoning.")]
        EnglishExamAndIq = 1,

        [Display(Name = "Technical Fundamentals Exam", Description = "Track-specific technical assessment. Evaluates core programming concepts.")]
        ProgrammingExam = 2,

        [Display(Name = "Technical Interview", Description = "One-on-one technical discussion with subject matter experts.")]
        TechnicalInterview = 3,

        [Display(Name = "Personal Interview", Description = "Final stage review with the admission committee to evaluate soft skills.")]
        SoftSkillsInterview = 4
    }
}
