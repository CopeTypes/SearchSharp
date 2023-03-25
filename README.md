# Read this
This project is for educational use only, scraping may violate the terms of service for some search engines.

The software will ***not*** be held accountable for **any** consequences that result from it's use. 

# SearchSharp
An easy to use search engine api for C# projects.

# Supported search engines
- Google
- Bing

# WIP
- Yahoo
- DuckDuckGo

# Usage
    var results = await new WebSearch().FromGoogle("how to play chess"); // basic usage
    
    var results = await new WebSearch().FromGoogle("how to play chess", // using the optional params
                 25, // results per page
                 2, // page number
                 WebSearch.SearchFreshness.PastWeek,
                "chess.com"); // filter by site
