namespace SearchSharp.api.search
{
    /// <summary>
    /// Simple class for search engine results
    /// </summary>
    public class SearchResult
    {
        public string Title { get; set; }
        public string Url { get; set; }

        public SearchResult(string title, string url)
        {
            Title = title;
            Url = url;
        }
    }
}