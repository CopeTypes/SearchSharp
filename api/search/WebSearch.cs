using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
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
        
        public async Task<List<SearchResult>> FromGoogle(string query)
        { // todo optional params for results per page, page number etc
            var html = await _client.GetStringAsync($"https://www.google.com/search?q={query}&num=20");

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var resultNodes = doc.DocumentNode.SelectNodes("//*[@class='yuRUbf']");
            if (resultNodes == null) throw new Exception("Failed to scrape Google search results");
            
            return (from resultNode in resultNodes let titleNode = resultNode.SelectSingleNode(".//h3[@class='LC20lb MBeuO DKV0Md']") let urlNode = resultNode.SelectSingleNode(".//a[@href]") where titleNode != null && urlNode != null let title = titleNode.InnerText let url = urlNode.Attributes["href"].Value select new SearchResult(title, url)).ToList();
        }
        
        public async Task<List<SearchResult>> FromBing(string query)
        {
            var html = await _client.GetStringAsync($"https://www.bing.com/search?q={query}&count=20");
            // freshness - Day/Week/Month/Year
            // setLang - self explanatory
            // mkt - what "market" to use for search results (en-US etc.)
            // cc - country code for search results

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var resultNodes = doc.DocumentNode.SelectNodes("//li[@class='b_algo']");
            if (resultNodes == null) throw new Exception("Failed to scrape Bing search results");

            return (from resultNode in resultNodes let titleNode = resultNode.SelectSingleNode(".//h2/a") let urlNode = resultNode.SelectSingleNode(".//h2/a[@href]") where titleNode != null && urlNode != null let title = titleNode.InnerText let url = urlNode.Attributes["href"].Value select new SearchResult(title, url)).ToList();
        }
        
        public async Task<List<SearchResult>> FromYahoo(string query)
        {
            var html = await _client.GetStringAsync($"https://search.yahoo.com/search?p={query}&fr=yfp-t-s&fp=1&toggle=1&cop=mss&ei=UTF-8&b=1&pz=10");
            // pz - results per page
            // bct - search category
            // b - should be 1, something about # of 1st results?
            // need to see about the rest lol
            // ei - unique identifier for search

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            
            var resultsNode = doc.DocumentNode.Descendants("div")
                .Where(node => node.GetAttributeValue("class", "")
                    .Contains("algo-sr")).ToList(); // best i can come up with rn to select the most at once

            //Console.WriteLine($@"Results node: {resultsNode.Count}");
            var results = new List<SearchResult>();
            foreach (var srNode in resultsNode)
            {
                var title = srNode.Descendants("a").FirstOrDefault()?.InnerText; // title but still ugly, the nice title is between like 7 tags, lovely.
                //Console.WriteLine(test);
                
                var url = srNode.Descendants("a").FirstOrDefault()?.GetAttributeValue("href", "");
                if (string.IsNullOrEmpty(url)) continue;
                url = Uri.UnescapeDataString(url.Split(new[] { "/RU=" }, StringSplitOptions.None)[1]);
                if (url.Contains("//RK=")) url = url.Split(new[] { "//RK" }, StringSplitOptions.None)[0];
                else if (url.Contains("/RK=")) url = url.Split(new[] { "/RK" }, StringSplitOptions.None)[0];
                //Console.WriteLine(url);

                results.Add(new SearchResult(title, url));
            }

            return results;
        }
        
        public async Task<List<SearchResult>> FromYandex(string query)
        {
            var html = await _client.GetStringAsync($"https://www.yandex.com/search/?text={query}&lr=87&c=225");
            // numdoc - number of results per page
            // lr - language for results
            // what - "type" of results (web, images, video, news)
            // filters - not entirely sure, but definitely useful
            // query_id - optional unique id for the query, wouldn't hurt to generate one

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var resultNodes = doc.DocumentNode.SelectNodes("//li[@class='serp-item serp-item_card']");
            //todo fixed the selector node, still throwing exception for some reason.
            if (resultNodes == null) throw new Exception("Failed to scrape Yandex search results");

            return (from resultNode in resultNodes let titleNode = resultNode.SelectSingleNode(".//a[@class='organic__url link link_theme_normal']") let urlNode = resultNode.SelectSingleNode(".//a[@class='organic__url link link_theme_normal']/@href") where titleNode != null && urlNode != null let title = titleNode.InnerText let url = urlNode.InnerText select new SearchResult(title, url)).ToList();
        }
        
        public async Task<List<SearchResult>> FromDuckDuckGo(string query)
        {
            var html = await _client.GetStringAsync($"https://duckduckgo.com/html/?q={query}");
            // no_redirect (0/1)
            // no_suggestions (0/1)
            // skip_qr (0/1) - skip "quick answer links" (probably default to 1)
            // t - "type" of results
            // lang
            // region
            

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // //div[@class='result results_links results_links_deep web-result']
            var resultNodes = doc.DocumentNode.SelectNodes("//*[@class='result__a']"); // this is working but then the other selectors aren't
            if (resultNodes == null) throw new Exception("Failed to scrape DuckDuckGo search results");

            //todo debug and fix
            return (from resultNode in resultNodes let titleNode = resultNode.SelectSingleNode(".//h2[@class='result__title']/a") let urlNode = resultNode.SelectSingleNode(".//h2[@class='result__title']/a/@href") where titleNode != null && urlNode != null let title = titleNode.InnerText let url = urlNode.InnerText select new SearchResult(title, url)).ToList();
        }
    }
}