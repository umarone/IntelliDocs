using AIChatAssistant.Configuration;
using AIChatAssistant.Interfaces;
using Microsoft.Extensions.Options;

namespace AIChatAssistant.Services.Providers
{
    public class ChatProviderFactory : IChatProviderFactory
    {
        private readonly IEnumerable<IChatProvider> _providers;
        private readonly AIOptions _options;
        public ChatProviderFactory(IEnumerable<IChatProvider> providers,
        IOptions<AIOptions> options)
        {
            _providers = providers;
            _options = options.Value;
        }
        public IChatProvider GetProvider()
        {
            var provider = _providers.FirstOrDefault(p => p.Name.Equals(_options.Provider, StringComparison.OrdinalIgnoreCase));
            if (provider == null)
            {
                throw new InvalidOperationException(
                    $"AI Provider '{_options.Provider}' is not registered.");
            }

            return provider;
        }
    }
}
