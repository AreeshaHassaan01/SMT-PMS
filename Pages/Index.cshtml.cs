using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PMS.Models;
using PMS.Repositories;

namespace PMS.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(
            IProductRepository productRepository, 
            ICategoryRepository categoryRepository, 
            ILogger<IndexModel> logger)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _logger = logger;
        }

        public DashboardStats Stats { get; set; } = new();
        public IEnumerable<Product> Products { get; set; } = new List<Product>();
        public IEnumerable<Category> Categories { get; set; } = new List<Category>();

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? CategoryId { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // Customers get their own shopping dashboard
            if (User.IsInRole("Customer"))
                return RedirectToPage("/Customer/Dashboard");

            try
            {
                Stats = await _productRepository.GetDashboardStatsAsync();
                Products = await _productRepository.GetAllAsync(SearchTerm, CategoryId);
                Categories = await _categoryRepository.GetAllAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard data.");
                TempData["ErrorMessage"] = "Could not load dashboard data. Check database connection.";
            }
            return Page();
        }
    }
}
