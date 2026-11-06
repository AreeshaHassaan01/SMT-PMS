using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PMS.Models;
using PMS.Repositories;

namespace PMS.Pages.Customer
{
    public class CartModel : PageModel
    {
        private readonly ICartRepository _cartRepo;

        public CartModel(ICartRepository cartRepo)
        {
            _cartRepo = cartRepo;
        }

        public IEnumerable<CartItem> CartItems { get; set; } = new List<CartItem>();
        public decimal Subtotal { get; set; }
        public int CartCount { get; set; }

        private int GetUserId()
        {
            var val = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0";
            return int.TryParse(val, out int id) ? id : 0;
        }

        public async Task OnGetAsync()
        {
            int userId = GetUserId();
            if (userId > 0)
            {
                CartItems = await _cartRepo.GetCartByUserIdAsync(userId);
                Subtotal = CartItems.Sum(i => i.Subtotal);
                CartCount = CartItems.Sum(i => i.Quantity);
            }
        }

        public async Task<IActionResult> OnPostUpdateQtyAsync(int cartItemId, int quantity)
        {
            int userId = GetUserId();
            if (userId > 0 && quantity >= 1)
                await _cartRepo.UpdateQuantityAsync(cartItemId, userId, quantity);
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRemoveAsync(int cartItemId)
        {
            int userId = GetUserId();
            if (userId > 0)
                await _cartRepo.RemoveFromCartAsync(cartItemId, userId);
            TempData["SuccessMessage"] = "Item removed from cart.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostClearCartAsync()
        {
            int userId = GetUserId();
            if (userId > 0)
                await _cartRepo.ClearCartAsync(userId);
            TempData["SuccessMessage"] = "Cart cleared.";
            return RedirectToPage();
        }
    }
}

