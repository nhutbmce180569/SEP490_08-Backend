namespace AIAPI.Services.IntelligentChat.Providers
{
    public interface IChatProviderFactory
    {
        IChatProvider GetProvider(string? providerName = null);
    }
}
