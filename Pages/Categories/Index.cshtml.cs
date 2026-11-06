using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PMS.Models;
using PMS.Repositories;

namespace PMS.Pages.Categories
{
    public class IndexModel : PageModel
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ICategoryRepository categoryRepository, ILogger<IndexModel> logger)
        {
            _categoryRepository = categoryRepository;
            _logger = logger;
        }

        public IEnumerable<Category> Categories { get; set; } = new List<Category>();

        [BindProperty]
        public Category NewCategory { get; set; } = new();

        [BindProperty]
        public Category EditCategory { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            if (User.IsInRole("Customer"))
            {
                TempData["ErrorMessage"] = "Access restricted to Admin & Stock Manager.";
                return RedirectToPage("/Customer/Dashboard");
            }

            Categories = await _categoryRepository.GetAllAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (User.IsInRole("Customer"))
            {
                TempData["ErrorMessage"] = "Access restricted to Admin & Stock Manager.";
                return RedirectToPage("/Customer/Dashboard");
            }

            if (string.IsNullOrWhiteSpace(NewCategory.Name))
            {
                ModelState.AddModelError("NewCategory.Name", "Category name is required.");
                Categories = await _categoryRepository.GetAllAsync();
                return Page();
            }

            try
            {
                await _categoryRepository.CreateAsync(NewCategory);
                TempData["SuccessMessage"] = $"Category '{NewCategory.Name}' created successfully!";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create category");
                TempData["ErrorMessage"] = "Error creating category: " + ex.Message;
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnPostEditAsync()
        {
            if (User.IsInRole("Customer"))
            {
                TempData["ErrorMessage"] = "Access restricted to Admin & Stock Manager.";
                return RedirectToPage("/Customer/Dashboard");
            }

            if (string.IsNullOrWhiteSpace(EditCategory.Name))
            {
                TempData["ErrorMessage"] = "Category name cannot be empty.";
                return RedirectToPage();
            }

            try
            {
                await _categoryRepository.UpdateAsync(EditCategory);
                TempData["SuccessMessage"] = $"Category '{EditCategory.Name}' updated successfully!";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update category");
                TempData["ErrorMessage"] = "Error updating category: " + ex.Message;
                return RedirectToPage();
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            if (User.IsInRole("Customer"))
            {
                TempData["ErrorMessage"] = "Access restricted to Admin & Stock Manager.";
                return RedirectToPage("/Customer/Dashboard");
            }

            try
            {
                var cat = await _categoryRepository.GetByIdAsync(id);
                if (cat != null && cat.ProductCount > 0)
                {
                    TempData["ErrorMessage"] = $"Cannot delete category '{cat.Name}' because it contains {cat.ProductCount} product(s). Please reassign or delete them first.";
                    return RedirectToPage();
                }

                await _categoryRepository.DeleteAsync(id);
                TempData["SuccessMessage"] = "Category deleted successfully!";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete category");
                TempData["ErrorMessage"] = "Error deleting category: " + ex.Message;
                return RedirectToPage();
            }
        }
    }
}

