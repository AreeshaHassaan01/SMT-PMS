using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PMS.Models;
using PMS.Repositories;

namespace PMS.Pages.Customer
{
    public class DashboardModel : PageModel
    {
        private readonly IProductRepository _productRepo;
        private readonly ICategoryRepository _categoryRepo;
        private readonly ICartRepository _cartRepo;

        public DashboardModel(IProductRepository productRepo, ICategoryRepository categoryRepo, ICartRepository cartRepo)
        {
            _productRepo = productRepo;
            _categoryRepo = categoryRepo;
            _cartRepo = cartRepo;
        }

        public IEnumerable<Product> Products { get; set; } = new List<Product>();
        public IEnumerable<Category> Categories { get; set; } = new List<Category>();
        public int CartCount { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? Search { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? CategoryId { get; set; }

        private int GetUserId()
        {
            var val = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0";
            return int.TryParse(val, out int id) ? id : 0;
        }

        public async Task OnGetAsync()
        {
            Products = await _productRepo.GetAllAsync(Search, CategoryId);
            Categories = await _categoryRepo.GetAllAsync();
            int userId = GetUserId();
            if (userId > 0)
                CartCount = await _cartRepo.GetCartItemCountAsync(userId);
        }

        public async Task<IActionResult> OnPostAddToCartAsync(int productId, int quantity)
        {
            int userId = GetUserId();
            if (userId <= 0)
                return RedirectToPage("/Account/Login");

            if (quantity < 1) quantity = 1;

            await _cartRepo.AddToCartAsync(userId, productId, quantity);
            TempData["SuccessMessage"] = "Item added to cart!";
            return RedirectToPage(new { Search, CategoryId });
        }
    }
}

