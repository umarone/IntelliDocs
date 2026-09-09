namespace AIChatAssistant.Interfaces
{
    public interface IPromptFormatter
    {
        string Build(
        string instruction,
        string title,
        IEnumerable<string> lines);
    }
}
