using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using PMS.Models;
using PMS.Repositories;

namespace PMS.Pages.Products
{
    public class CreateModel : PageModel
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<CreateModel> _logger;

        public CreateModel(
            IProductRepository productRepository,
            ICategoryRepository categoryRepository,
            IWebHostEnvironment environment,
            ILogger<CreateModel> logger)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _environment = environment;
            _logger = logger;
        }

        [BindProperty]
        public Product Product { get; set; } = new();

        [BindProperty]
        public IFormFile? ImageUpload { get; set; }

        public SelectList CategoryList { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync()
        {
            if (User.IsInRole("Customer"))
            {
                TempData["ErrorMessage"] = "Access restricted to Admin & Stock Manager.";
                return RedirectToPage("/Customer/Dashboard");
            }
            await LoadCategoriesAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (User.IsInRole("Customer"))
            {
                TempData["ErrorMessage"] = "Access restricted to Admin & Stock Manager.";
                return RedirectToPage("/Customer/Dashboard");
            }

            if (!ModelState.IsValid)
            {
                await LoadCategoriesAsync();
                return Page();
            }

            try
            {
                // Handle file upload if present
                if (ImageUpload != null && ImageUpload.Length > 0)
                {
                    var uploadsDir = Path.Combine(_environment.WebRootPath, "uploads");
                    if (!Directory.Exists(uploadsDir))
                    {
                        Directory.CreateDirectory(uploadsDir);
                    }

                    var extension = Path.GetExtension(ImageUpload.FileName).ToLowerInvariant();
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".svg", ".webp", ".gif" };
                    if (!allowedExtensions.Contains(extension))
                    {
                        ModelState.AddModelError("ImageUpload", "Only image files (.jpg, .png, .svg, .webp) are allowed.");
                        await LoadCategoriesAsync();
                        return Page();
                    }

                    var fileName = $"{Guid.NewGuid():N}{extension}";
                    var filePath = Path.Combine(uploadsDir, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await ImageUpload.CopyToAsync(stream);
                    }

                    Product.ImageUrl = $"/uploads/{fileName}";
                }
                else if (string.IsNullOrWhiteSpace(Product.ImageUrl))
                {
                    // Fallback to default product image or based on category
                    Product.ImageUrl = "/images/products/default-product.svg";
                }

                int newId = await _productRepository.CreateAsync(Product);
                TempData["SuccessMessage"] = $"Product '{Product.Name}' was successfully created!";
                return RedirectToPage("/Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create product.");
                ModelState.AddModelError(string.Empty, "An error occurred while saving the product to the database. " + ex.Message);
                await LoadCategoriesAsync();
                return Page();
            }
        }

        private async Task LoadCategoriesAsync()
        {
            var categories = await _categoryRepository.GetAllAsync();
            CategoryList = new SelectList(categories, "Id", "Name");
        }
    }
}

