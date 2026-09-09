using AIChatAssistant.Interfaces;
using System.Text;

namespace AIChatAssistant.Services.AI.Prompt
{
    public class PromptFormatter : IPromptFormatter
    {
        private const int SeparatorLength = 40;
        public string Build(
            string instruction,
            string title,
            IEnumerable<string> lines)
        {
            var builder = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(instruction))
            {
                builder.AppendLine(instruction);
                builder.AppendLine();
            }

            builder.AppendLine(title);
            builder.AppendLine(new string('-', SeparatorLength));

            foreach (var line in lines)
            {
                builder.AppendLine(line);
            }

            return builder.ToString();
        }
    }
}
