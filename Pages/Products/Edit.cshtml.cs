using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using PMS.Models;
using PMS.Repositories;

namespace PMS.Pages.Products
{
    public class EditModel : PageModel
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<EditModel> _logger;

        public EditModel(
            IProductRepository productRepository,
            ICategoryRepository categoryRepository,
            IWebHostEnvironment environment,
            ILogger<EditModel> logger)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _environment = environment;
            _logger = logger;
        }

        [BindProperty]
        public Product Product { get; set; } = default!;

        [BindProperty]
        public IFormFile? ImageUpload { get; set; }

        public SelectList CategoryList { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (User.IsInRole("Customer"))
            {
                TempData["ErrorMessage"] = "Access restricted to Admin & Stock Manager.";
                return RedirectToPage("/Customer/Dashboard");
            }

            if (id == null)
            {
                return NotFound();
            }

            var product = await _productRepository.GetByIdAsync(id.Value);
            if (product == null)
            {
                return NotFound();
            }

            Product = product;
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
                // Handle new file upload if supplied
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

                bool updated = await _productRepository.UpdateAsync(Product);
                if (!updated)
                {
                    TempData["ErrorMessage"] = "Product update failed. Item not found or database error.";
                    return RedirectToPage("/Index");
                }

                TempData["SuccessMessage"] = $"Product '{Product.Name}' was successfully updated!";
                return RedirectToPage("/Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update product.");
                ModelState.AddModelError(string.Empty, "Error updating product: " + ex.Message);
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

