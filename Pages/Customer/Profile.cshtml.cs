using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PMS.Models;
using PMS.Repositories;

namespace PMS.Pages.Customer
{
    public class ProfileModel : PageModel
    {
        private readonly IUserProfileRepository _profileRepo;
        private readonly IAccountRepository _accountRepo;
        private readonly ICartRepository _cartRepo;

        public ProfileModel(
            IUserProfileRepository profileRepo,
            IAccountRepository accountRepo,
            ICartRepository cartRepo)
        {
            _profileRepo = profileRepo;
            _accountRepo = accountRepo;
            _cartRepo = cartRepo;
        }

        [BindProperty]
        public ProfileInputModel Input { get; set; } = new();

        public string Email { get; set; } = string.Empty;
        public string? SavedAddressDisplay { get; set; }
        public int CartCount { get; set; }

        public class ProfileInputModel
        {
            [Required(ErrorMessage = "Full Name is required")]
            [StringLength(100, ErrorMessage = "Full Name cannot exceed 100 characters")]
            [Display(Name = "Full Name")]
            public string FullName { get; set; } = string.Empty;

            [Phone(ErrorMessage = "Please enter a valid phone number")]
            [StringLength(20)]
            [Display(Name = "Phone Number")]
            public string? Phone { get; set; }

            [Required(ErrorMessage = "House / Street address is required for delivery")]
            [StringLength(300, ErrorMessage = "Address cannot exceed 300 characters")]
            [Display(Name = "House / Street Address")]
            public string? Address { get; set; }

            [Required(ErrorMessage = "City is required")]
            [StringLength(100)]
            [Display(Name = "City")]
            public string? City { get; set; }

            [StringLength(100)]
            [Display(Name = "Province / State")]
            public string? Province { get; set; }

            [StringLength(20)]
            [Display(Name = "Postal Code")]
            public string? PostalCode { get; set; }

            [Required(ErrorMessage = "Country is required")]
            [StringLength(100)]
            [Display(Name = "Country")]
            public string Country { get; set; } = "Pakistan";
        }

        private int GetUserId()
        {
            var val = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0";
            return int.TryParse(val, out int id) ? id : 0;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            int userId = GetUserId();
            if (userId <= 0) return RedirectToPage("/Account/Login");

            Email = User.FindFirst(ClaimTypes.Email)?.Value ?? "";

            var account = await _accountRepo.GetByIdAsync(userId);
            var profile = await _profileRepo.GetByUserIdAsync(userId);

            if (profile != null)
            {
                Input = new ProfileInputModel
                {
                    FullName = profile.FullName,
                    Phone = profile.Phone,
                    Address = profile.Address,
                    City = profile.City,
                    Province = profile.Province,
                    PostalCode = profile.PostalCode,
                    Country = profile.Country
                };
                SavedAddressDisplay = profile.ShippingAddress;
            }
            else if (account != null)
            {
                Input.FullName = account.FullName;
            }

            CartCount = await _cartRepo.GetCartItemCountAsync(userId);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            int userId = GetUserId();
            if (userId <= 0) return RedirectToPage("/Account/Login");

            if (!ModelState.IsValid)
            {
                Email = User.FindFirst(ClaimTypes.Email)?.Value ?? "";
                CartCount = await _cartRepo.GetCartItemCountAsync(userId);
                return Page();
            }

            var profile = new UserProfile
            {
                UserId = userId,
                FullName = Input.FullName.Trim(),
                Phone = Input.Phone?.Trim(),
                Address = Input.Address?.Trim(),
                City = Input.City?.Trim(),
                Province = Input.Province?.Trim(),
                PostalCode = Input.PostalCode?.Trim(),
                Country = string.IsNullOrWhiteSpace(Input.Country) ? "Pakistan" : Input.Country.Trim()
            };

            await _profileRepo.UpsertAsync(profile);

            // Also update FullName on UserAccount if changed
            var account = await _accountRepo.GetByIdAsync(userId);
            if (account != null && account.FullName != profile.FullName)
            {
                account.FullName = profile.FullName;
                await _accountRepo.UpdateAsync(account);
            }

            TempData["SuccessMessage"] = "Profile and delivery address updated successfully!";
            return RedirectToPage();
        }
    }
}

