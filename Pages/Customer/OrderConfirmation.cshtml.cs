using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PMS.Models;
using PMS.Repositories;

namespace PMS.Pages.Customer
{
    public class OrderConfirmationModel : PageModel
    {
        private readonly IOrderRepository _orderRepo;
        private readonly ICartRepository _cartRepo;

        public OrderConfirmationModel(IOrderRepository orderRepo, ICartRepository cartRepo)
        {
            _orderRepo = orderRepo;
            _cartRepo = cartRepo;
        }

        public Order? Order { get; set; }
        public int CartCount { get; set; }

        private int GetUserId()
        {
            var val = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0";
            return int.TryParse(val, out int id) ? id : 0;
        }

        public async Task<IActionResult> OnGetAsync(int orderId)
        {
            int userId = GetUserId();
            if (userId <= 0) return RedirectToPage("/Account/Login");

            Order = await _orderRepo.GetOrderByIdAsync(orderId, userId);
            if (Order == null)
            {
                TempData["ErrorMessage"] = "Order not found.";
                return RedirectToPage("/Customer/Orders");
            }

            CartCount = await _cartRepo.GetCartItemCountAsync(userId);
            return Page();
        }
    }
}

