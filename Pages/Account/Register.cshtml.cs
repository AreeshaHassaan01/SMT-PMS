using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PMS.Data;
using PMS.Models;
using PMS.Repositories;

namespace PMS.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly IAccountRepository _accountRepository;
        private readonly ILogger<RegisterModel> _logger;

        public RegisterModel(IAccountRepository accountRepository, ILogger<RegisterModel> logger)
        {
            _accountRepository = accountRepository;
            _logger = logger;
        }

        [BindProperty]
        [Required(ErrorMessage = "Full Name is required.")]
        public string FullName { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Email Address is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        public string Email { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Password is required.")]
        [MinLength(4, ErrorMessage = "Password must be at least 4 characters.")]
        public string Password { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Please confirm your password.")]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [BindProperty]
        public string? SecretCode { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                // Check if an account already exists with this email
                var existing = await _accountRepository.GetByEmailOrUsernameAsync(Email.Trim());
                if (existing != null)
                {
                    ModelState.AddModelError("Email", "An account with this email address already exists. Please Sign In.");
                    return Page();
                }

                // Check SECRET-CODE: ANYONEUSEIT
                string trimmedSecret = (SecretCode ?? string.Empty).Trim();
                bool isAdminCode = string.Equals(trimmedSecret, "ANYONEUSEIT", StringComparison.OrdinalIgnoreCase);

                // If code matches ANYONEUSEIT -> Admin & Stock Manager
                // If code is not entered -> automatically Customer!
                string assignedRole = isAdminCode ? "Admin & Stock Manager" : "Customer";

                var newAccount = new UserAccount
                {
                    Username = Email.Trim().Split('@')[0],
                    FullName = FullName.Trim(),
                    Email = Email.Trim(),
                    PasswordHash = DbInitializer.HashPassword(Password),
                    Role = assignedRole,
                    CreatedAt = DateTime.UtcNow,
                    LastLoginAt = DateTime.UtcNow
                };

                int newId = await _accountRepository.CreateAsync(newAccount);

                // Automatically log in the user upon registration
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, newId.ToString()),
                    new Claim(ClaimTypes.Name, newAccount.FullName),
                    new Claim(ClaimTypes.Email, newAccount.Email),
                    new Claim(ClaimTypes.Role, assignedRole),
                    new Claim("RoleDisplayName", assignedRole)
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

                if (isAdminCode)
                {
                    TempData["SuccessMessage"] = $"Welcome {newAccount.FullName}! Account created with SECRET-CODE as Admin & Stock Manager.";
                    return RedirectToPage("/Index");
                }
                else
                {
                    TempData["SuccessMessage"] = $"Welcome {newAccount.FullName}! Account created successfully as Customer.";
                    return RedirectToPage("/Customer/Dashboard");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating account in SQL Server");
                ModelState.AddModelError(string.Empty, "An error occurred while creating your account: " + ex.Message);
                return Page();
            }
        }
    }
}

