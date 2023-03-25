using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace SearchSharp.api.search
{

    public class WebSearch
    {
        private readonly HttpClient _client = GetClient();

        private static HttpClient GetClient()
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/111.0.0.0 Safari/537.36");
            return client;
        }
        
        public static WebSearch New()
        {
            return new WebSearch();
        }
        
        public enum SearchFreshness { Any, PastDay, PastWeek, PastMonth, PastYear}
        
        public async Task<List<SearchResult>> FromGoogle(string query, int results = 10, int page = 1, 
            SearchFreshness freshness = SearchFreshness.Any, string siteFilter = null)
        {
            var gurl =
                $"https://www.google.com/search?q={Uri.UnescapeDataString(query)}&num={results}&start={(page - 1) * results}";
            gurl += string.IsNullOrEmpty(siteFilter) ? "" : $"&as_sitesearch={Uri.EscapeDataString(siteFilter)}";

            if (freshness != SearchFreshness.Any)
            {
                gurl += "&tbs=qdr:";
                switch (freshness)
                {
                    case SearchFreshness.PastDay:
                        gurl += "d";
                        break;
                    case SearchFreshness.PastWeek:
                        gurl += "w";
                        break;
                    case SearchFreshness.PastMonth:
                        gurl += "m";
                        break;
                    case SearchFreshness.PastYear:
                        gurl += "y";
                        break;
                }
            }
            
            var html = await _client.GetStringAsync(gurl);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var resultNodes = doc.DocumentNode.SelectNodes("//*[@class='yuRUbf']");
            if (resultNodes == null) throw new Exception("Failed to scrape Google search results");
            return (from resultNode in resultNodes let titleNode = resultNode.SelectSingleNode(".//h3[@class='LC20lb MBeuO DKV0Md']") let urlNode = resultNode.SelectSingleNode(".//a[@href]") where titleNode != null && urlNode != null let title = titleNode.InnerText let url = urlNode.Attributes["href"].Value select new SearchResult(title, url)).ToList();
        }
        
        public async Task<List<SearchResult>> FromBing(string query, int results = 10, int page = 1, SearchFreshness freshness = SearchFreshness.Any)
        {
            var burl =
                $"https://www.bing.com/search?q={Uri.UnescapeDataString(query)}&count={results}&offset={(page - 1) * results}";

            if (freshness != SearchFreshness.Any)
            {
                burl += "&freshness=";
                switch (freshness)
                {
                    case SearchFreshness.PastDay:
                        burl += "Day";
                        break;
                    case SearchFreshness.PastWeek:
                        burl += "Week";
                        break;
                    case SearchFreshness.PastMonth:
                        burl += "Month";
                        break;
                    case SearchFreshness.PastYear:
                        burl += "Year";
                        break;
                }
            }

            var html = await _client.GetStringAsync(burl);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var resultNodes = doc.DocumentNode.SelectNodes("//li[@class='b_algo']");
            if (resultNodes == null) throw new Exception("Failed to scrape Bing search results");
            return (from resultNode in resultNodes let titleNode = resultNode.SelectSingleNode(".//h2/a") let urlNode = resultNode.SelectSingleNode(".//h2/a[@href]") where titleNode != null && urlNode != null let title = titleNode.InnerText let url = urlNode.Attributes["href"].Value select new SearchResult(title, url)).ToList();
        }


        private readonly Regex yhTitle = new Regex(@"[A-Z]");
        public async Task<List<SearchResult>> FromYahoo(string query, int results = 10)
        {
            var html = await _client.GetStringAsync($"https://search.yahoo.com/search?p={Uri.UnescapeDataString(query)}ei=UTF-8&b=1&pz={results}");

            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            
            var resultsL = new List<SearchResult>();
            var resultsNode = doc.DocumentNode.Descendants("div")
                .Where(node => node.GetAttributeValue("class", "")
                    .Contains("algo-sr")).ToList();
            
            foreach (var srNode in resultsNode)
            {
                var title = srNode.Descendants("a").FirstOrDefault()?.InnerText; // this is as good as it's getting lol
                if (title == null) continue;
                var m = yhTitle.Match(title); // usually all the junk before the title is lowercase, but sometimes it isn't
                title = title.Substring(m.Index);

                var url = srNode.Descendants("a").FirstOrDefault()?.GetAttributeValue("href", "");
                if (string.IsNullOrEmpty(url)) continue;
                
                url = Uri.UnescapeDataString(url.Split(new[] { "/RU=" }, StringSplitOptions.None)[1]);
                if (url.Contains("//RK=")) url = url.Split(new[] { "//RK" }, StringSplitOptions.None)[0];
                else if (url.Contains("/RK=")) url = url.Split(new[] { "/RK" }, StringSplitOptions.None)[0];
                
                resultsL.Add(new SearchResult(title, url));
            }

            return resultsL;
        }
        
        public async Task<List<SearchResult>> FromDuckDuckGo(string query)
        {
            // todo will need selenium-like solution
            var html = await _client.GetStringAsync($"https://duckduckgo.com/html/?q={Uri.UnescapeDataString(query)}");
            
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // //div[@class='result results_links results_links_deep web-result']
            var resultNodes = doc.DocumentNode.SelectNodes("//div[contains(@class, 'result')]");
            if (resultNodes == null) throw new Exception("Failed to scrape DuckDuckGo search results");
            
            return (from resultNode in resultNodes let titleNode = resultNode.SelectSingleNode(".//h2/a") let urlNode = resultNode.SelectSingleNode(".//a[@class='result__url") where titleNode != null && urlNode != null let title = titleNode.InnerText let url = urlNode.InnerText select new SearchResult(title, url)).ToList();
        }
    }
}