namespace Common.Config
{
    public class HackerNewsOptions
    {
        public int MaxStoriesPerRequest { get; set; } = 50;
        public int MaxConcurrentStoryRequests { get; set; } = 10;
        public int BestStoriesCacheSeconds { get; set; } = 60;
        public int StoryDetailsCacheSeconds { get; set; } = 300;
    }
}
