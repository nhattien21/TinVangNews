// Services/INewsService.cs
using TinVangNews.ViewModels;

namespace TinVangNews.Services
{
    public interface INewsService
    {
        Task<List<ArticleViewModel>> GetNewsAsync(string category, string query);
    }
}