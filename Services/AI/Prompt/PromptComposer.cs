using AIChatAssistant.Interfaces;
using AIChatAssistant.Models.Chat;

namespace AIChatAssistant.Services.AI.Prompt
{
    public class PromptComposer : IPromptComposer
    {
        private readonly IEnumerable<IPromptSection> _sections;

        public PromptComposer(
            IEnumerable<IPromptSection> sections)
        {
            _sections = sections;
        }

        public List<OllamaChatMessage> Compose(
            ChatRequest request,
            PromptContext context)
        {
            var messages = new List<OllamaChatMessage>();

            foreach (var section in _sections.OrderBy(s => s.Order))
            {
                messages.AddRange(section.Build(request, context));
            }

            messages.AddRange(context.ConversationMessages.Select(
            m => new OllamaChatMessage
            {
                Role = m.Role,
                Content = m.Content
            }));
            return messages;
        }
    }
}
