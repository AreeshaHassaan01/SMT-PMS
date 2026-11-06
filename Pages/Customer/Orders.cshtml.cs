using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PMS.Models;
using PMS.Repositories;

namespace PMS.Pages.Customer
{
    public class OrdersModel : PageModel
    {
        private readonly IOrderRepository _orderRepo;
        private readonly ICartRepository _cartRepo;

        public OrdersModel(IOrderRepository orderRepo, ICartRepository cartRepo)
        {
            _orderRepo = orderRepo;
            _cartRepo = cartRepo;
        }

        public IEnumerable<Order> Orders { get; set; } = new List<Order>();
        public int CartCount { get; set; }

        private int GetUserId()
        {
            var val = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0";
            return int.TryParse(val, out int id) ? id : 0;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            int userId = GetUserId();
            if (userId <= 0) return RedirectToPage("/Account/Login");

            Orders = await _orderRepo.GetOrdersByUserIdAsync(userId);
            CartCount = await _cartRepo.GetCartItemCountAsync(userId);
            return Page();
        }
    }
}

