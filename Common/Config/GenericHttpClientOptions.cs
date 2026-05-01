namespace Common.Config
{
    public class GenericHttpClientOptions
    {
        public const string ClientName = "GenericHttpClient";

        public string BaseUrl { get; set; } = string.Empty;
        public int TimeoutSeconds { get; set; } = 30;
    }
}
