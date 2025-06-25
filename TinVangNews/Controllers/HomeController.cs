// Controllers/HomeController.cs

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using TinVangNews.Data;
using TinVangNews.Hubs;
using TinVangNews.Models;
using TinVangNews.Services;
using TinVangNews.ViewModels;
using X.PagedList.Extensions;

public class HomeController : Controller
{
    private readonly INewsService _newsService;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IHubContext<DashboardHub> _dashboardHubContext; 


    public HomeController(
        INewsService newsService,
        ApplicationDbContext context,
        UserManager<IdentityUser> userManager,
        IHubContext<DashboardHub> dashboardHubContext) 
    {
        _newsService = newsService;
        _context = context;
        _userManager = userManager;
        _dashboardHubContext = dashboardHubContext; 
    }

    public async Task<IActionResult> Index(string category = "general", string? query = null, int page = 1)
    {
        ViewData["Title"] = "Trang Tin Tức";
        var categoryDisplay = category switch
        {
            "business" => "Kinh doanh",
            "sports" => "Thể thao",
            "technology" => "Công nghệ",
            "entertainment" => "Giải trí",
            _ => "Tổng hợp"
        };
        ViewData["CurrentCategory"] = category;
        ViewData["CurrentQuery"] = query;
        if (!string.IsNullOrEmpty(query))
        {
            ViewData["NewsType"] = $"Kết quả tìm kiếm cho: '{query}'";
        }
        else
        {
            ViewData["NewsType"] = categoryDisplay;
        }
        var allArticles = await _newsService.GetNewsAsync(category, query);
        int pageSize = 12;
        int pageNumber = page;
        var pagedArticles = allArticles.ToPagedList(pageNumber, pageSize);
        return View(pagedArticles);
    }


    [HttpPost]
    [Authorize]
    public async Task<IActionResult> AddToFavorites([FromBody] ArticleViewModel article)
    {
        if (article == null || string.IsNullOrEmpty(article.Url))
        {
            return BadRequest(new { message = "Dữ liệu bài viết không hợp lệ." });
        }

        var userId = _userManager.GetUserId(User);
        if (userId == null)
        {
            return Unauthorized(new { message = "Vui lòng đăng nhập." });
        }
        var existingFavorite = await _context.FavoriteArticles
                                   .FirstOrDefaultAsync(f => f.UserId == userId && f.Url == article.Url);

        if (existingFavorite != null)
        {
            return Ok(new { message = "Bài viết này đã được lưu." });
        }

        var favorite = new FavoriteArticle
        {
            UserId = userId,
            Title = article.Title ?? "Không có tiêu đề",
            Description = article.Description,
            Url = article.Url,
            UrlToImage = article.UrlToImage,
            PublishedAt = article.PublishedAt
        };

        _context.FavoriteArticles.Add(favorite);
        await _context.SaveChangesAsync();

        await _dashboardHubContext.Clients.Group("Admins").SendAsync("UpdateDashboardStats");

        return Ok(new { message = "Đã lưu bài viết thành công!" });
    }

    [Authorize]
    public async Task<IActionResult> Favorites()
    {
        ViewData["Title"] = "Bài viết yêu thích";
        var userId = _userManager.GetUserId(User);
        var favoriteArticles = await _context.FavoriteArticles
                                        .Where(f => f.UserId == userId)
                                        .OrderByDescending(f => f.Id)
                                        .ToListAsync();
        return View(favoriteArticles);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> RemoveFavorite(int id)
    {
        var userId = _userManager.GetUserId(User);
        var favorite = await _context.FavoriteArticles.FirstOrDefaultAsync(f => f.Id == id && f.UserId == userId);

        if (favorite == null)
        {
            return NotFound();
        }

        _context.FavoriteArticles.Remove(favorite);
        await _context.SaveChangesAsync();

        //  GỬI REAL-TIME ĐẾN ADMIN DASHBOARD 
        await _dashboardHubContext.Clients.Group("Admins").SendAsync("UpdateDashboardStats");

        TempData["SuccessMessage"] = "Đã xóa bài viết khỏi danh sách yêu thích.";
        return RedirectToAction(nameof(Favorites));
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}