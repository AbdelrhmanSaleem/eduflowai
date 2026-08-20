namespace EduFlowAI.AI.Infrastructure.Options;

public class IntentClassificationOptions
{
    public const string SectionName = "IntentClassification";

    // Below this the router asks the user to clarify instead of guessing.
    public decimal MinConfidence { get; set; } = 0.6m;
}
