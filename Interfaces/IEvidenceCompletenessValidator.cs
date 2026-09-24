using AIChatAssistant.Models.RAG;

namespace AIChatAssistant.Interfaces
{
    public interface IEvidenceCompletenessValidator
    {
        Task<EvidenceCompletenessResult> ValidateAsync(
        string informationNeed,
        IReadOnlyList<EvidenceUnit> selectedEvidence,
        CancellationToken cancellationToken = default);
    }
}
