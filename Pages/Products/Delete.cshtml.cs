using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PMS.Models;
using PMS.Repositories;

namespace PMS.Pages.Products
{
    public class DeleteModel : PageModel
    {
        private readonly IProductRepository _productRepository;
        private readonly ILogger<DeleteModel> _logger;

        public DeleteModel(IProductRepository productRepository, ILogger<DeleteModel> logger)
        {
            _productRepository = productRepository;
            _logger = logger;
        }

        [BindProperty]
        public Product Product { get; set; } = default!;

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
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
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

            try
            {
                var product = await _productRepository.GetByIdAsync(id.Value);
                var productName = product?.Name ?? $"ID {id.Value}";

                bool deleted = await _productRepository.DeleteAsync(id.Value);
                if (deleted)
                {
                    TempData["SuccessMessage"] = $"Product '{productName}' was permanently deleted.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Could not delete product. It may have already been removed.";
                }

                return RedirectToPage("/Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete product ID {Id}", id);
                TempData["ErrorMessage"] = "Database error deleting product: " + ex.Message;
                return RedirectToPage("/Index");
            }
        }
    }
}

