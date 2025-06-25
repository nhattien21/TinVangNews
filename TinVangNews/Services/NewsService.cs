// Services/NewsService.cs
using System.Text.Json;
using TinVangNews.ViewModels;

namespace TinVangNews.Services
{
    public class NewsService : INewsService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly string _apiKey;

        private const string BaseUrl = "https://newsapi.org/v2";

        public NewsService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _apiKey = _configuration["NewsApi:ApiKey"] ?? throw new InvalidOperationException("NewsAPI key not found.");
        }

        public async Task<List<ArticleViewModel>> GetNewsAsync(string category = "general", string? query = null)
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");

            string url;


            if (!string.IsNullOrEmpty(query))
            {
                url = $"{BaseUrl}/everything?q={Uri.EscapeDataString(query)}&sortBy=relevancy&apiKey={_apiKey}";
            }
            else
            {
                url = $"{BaseUrl}/top-headlines?country=us&category={Uri.EscapeDataString(category)}&apiKey={_apiKey}";
            }

            try
            {
                var response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();

                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                    };
                    var newsApiResponse = JsonSerializer.Deserialize<NewsApiResponse>(jsonString, options);

                    var validArticles = newsApiResponse?.Articles?
                        .Where(a => !string.IsNullOrEmpty(a.Title) && !string.IsNullOrEmpty(a.Url)).ToList();

                    return validArticles ?? new List<ArticleViewModel>();
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"--- NewsAPI Error ---");
                    Console.WriteLine($"URL: {url}");
                    Console.WriteLine($"Status Code: {response.StatusCode}");
                    Console.WriteLine($"Response: {errorContent}");
                    Console.WriteLine($"---------------------");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"--- Exception in NewsService ---");
                Console.WriteLine(ex.ToString());
                Console.WriteLine($"--------------------------------");
            }

            return new List<ArticleViewModel>();
        }
    }
}   