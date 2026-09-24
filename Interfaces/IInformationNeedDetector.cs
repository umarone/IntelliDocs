using AIChatAssistant.Models.RAG;

namespace AIChatAssistant.Interfaces
{
    public interface IInformationNeedDetector
    {
        Task<InformationNeedDetectionResult> DetectAsync(
        string question,
        CancellationToken cancellationToken = default);
    }
}
