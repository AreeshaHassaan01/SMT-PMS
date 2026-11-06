using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PMS.Models;
using PMS.Repositories;

namespace PMS.Pages.Customer
{
    public class CheckoutModel : PageModel
    {
        private readonly ICartRepository _cartRepo;
        private readonly IOrderRepository _orderRepo;
        private readonly IUserProfileRepository _profileRepo;

        public CheckoutModel(ICartRepository cartRepo, IOrderRepository orderRepo, IUserProfileRepository profileRepo)
        {
            _cartRepo = cartRepo;
            _orderRepo = orderRepo;
            _profileRepo = profileRepo;
        }

        public IEnumerable<CartItem> CartItems { get; set; } = new List<CartItem>();
        public decimal Subtotal { get; set; }
        public int CartCount { get; set; }
        public string? SavedAddress { get; set; }
        public string? ProfileAddress { get; set; }
        public string? ProfileCity { get; set; }
        public string? ProfileProvince { get; set; }
        public string? ProfilePostalCode { get; set; }
        public string? ProfileCountry { get; set; }

        private int GetUserId()
        {
            var val = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0";
            return int.TryParse(val, out int id) ? id : 0;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            int userId = GetUserId();
            if (userId <= 0) return RedirectToPage("/Account/Login");

            CartItems = await _cartRepo.GetCartByUserIdAsync(userId);
            if (!CartItems.Any())
            {
                TempData["ErrorMessage"] = "Your cart is empty.";
                return RedirectToPage("/Customer/Cart");
            }
            Subtotal = CartItems.Sum(i => i.Subtotal);
            CartCount = CartItems.Sum(i => i.Quantity);

            // Pre-fill from saved profile
            var profile = await _profileRepo.GetByUserIdAsync(userId);
            if (profile != null)
            {
                SavedAddress = profile.ShippingAddress;
                ProfileAddress = profile.Address;
                ProfileCity = profile.City;
                ProfileProvince = profile.Province;
                ProfilePostalCode = profile.PostalCode;
                ProfileCountry = profile.Country;
            }
            return Page();
        }

        public async Task<IActionResult> OnPostPlaceOrderAsync(
            string paymentMethod,
            string? address, string? city, string? province, string? postalCode, string? country)
        {
            int userId = GetUserId();
            if (userId <= 0) return RedirectToPage("/Account/Login");

            var cartItems = (await _cartRepo.GetCartByUserIdAsync(userId)).ToList();
            if (!cartItems.Any())
            {
                TempData["ErrorMessage"] = "Your cart is empty.";
                return RedirectToPage("/Customer/Cart");
            }

            // Build shipping address string
            string shippingAddress = string.Join(", ",
                new[] { address, city, province, postalCode, country }
                .Where(s => !string.IsNullOrWhiteSpace(s)));

            var order = new Order
            {
                UserId = userId,
                TotalAmount = cartItems.Sum(i => i.Subtotal),
                PaymentMethod = string.IsNullOrWhiteSpace(paymentMethod) ? "Cash on Delivery" : paymentMethod,
                ShippingAddress = shippingAddress,
                Status = "Pending",
            };

            int orderId = await _orderRepo.CreateOrderAsync(order, cartItems);
            await _cartRepo.ClearCartAsync(userId);

            // Also save address to profile
            if (!string.IsNullOrWhiteSpace(address))
            {
                var profile = await _profileRepo.GetByUserIdAsync(userId) ?? new UserProfile { UserId = userId };
                profile.UserId = userId;
                profile.Address = address;
                profile.City = city;
                profile.Province = province;
                profile.PostalCode = postalCode;
                profile.Country = string.IsNullOrWhiteSpace(country) ? "Pakistan" : country;
                if (string.IsNullOrWhiteSpace(profile.FullName))
                    profile.FullName = User.Identity?.Name ?? string.Empty;
                await _profileRepo.UpsertAsync(profile);
            }

            return RedirectToPage("/Customer/OrderConfirmation", new { orderId });
        }
    }
}

