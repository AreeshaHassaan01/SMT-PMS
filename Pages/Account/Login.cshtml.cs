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
    public class LoginModel : PageModel
    {
        private readonly IAccountRepository _accountRepository;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(IAccountRepository accountRepository, ILogger<LoginModel> logger)
        {
            _accountRepository = accountRepository;
            _logger = logger;
        }

        [BindProperty]
        public string Email { get; set; } = string.Empty;

        [BindProperty]
        public string Password { get; set; } = string.Empty;

        [BindProperty]
        public string? SecretCode { get; set; }

        public string? ReturnUrl { get; set; }

        public void OnGet(string? returnUrl = null)
        {
            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            ReturnUrl = returnUrl ?? "/Index";

            string trimmedSecret = (SecretCode ?? string.Empty).Trim();
            bool isSecretCodeValid = string.Equals(trimmedSecret, "ANYONEUSEIT", StringComparison.OrdinalIgnoreCase);

            // Case 1: SECRET-CODE ANYONEUSEIT provided
            if (isSecretCodeValid)
            {
                string userEmail = string.IsNullOrWhiteSpace(Email) ? "anyonecanuseit03@gmail.com" : Email.Trim();
                string fullName = "Admin & Stock Manager";

                int userId;
                // Check if account already exists in SQL Server
                var existingAccount = await _accountRepository.GetByEmailOrUsernameAsync(userEmail);
                if (existingAccount != null)
                {
                    userId = existingAccount.Id;
                    fullName = existingAccount.FullName;
                    // Elevate/ensure role is Admin & Stock Manager
                    if (existingAccount.Role != "Admin & Stock Manager")
                    {
                        await _accountRepository.UpdateRoleAsync(existingAccount.Id, "Admin & Stock Manager");
                    }
                    await _accountRepository.UpdateLastLoginAsync(existingAccount.Id);
                }
                else
                {
                    // Create account in SQL Server with Admin & Stock Manager role
                    var newAdmin = new UserAccount
                    {
                        Username = userEmail.Split('@')[0],
                        FullName = "Admin & Stock Manager",
                        Email = userEmail,
                        PasswordHash = DbInitializer.HashPassword(string.IsNullOrWhiteSpace(Password) ? "ANYONEUSEIT" : Password),
                        Role = "Admin & Stock Manager",
                        CreatedAt = DateTime.UtcNow,
                        LastLoginAt = DateTime.UtcNow
                    };
                    userId = await _accountRepository.CreateAsync(newAdmin);
                }

                // Issue Authentication Cookie as Admin & Stock Manager
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                    new Claim(ClaimTypes.Name, fullName),
                    new Claim(ClaimTypes.Email, userEmail),
                    new Claim(ClaimTypes.Role, "Admin & Stock Manager"),
                    new Claim("RoleDisplayName", "Admin & Stock Manager")
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

                TempData["SuccessMessage"] = "Authorized via SECRET-CODE! Welcome, Admin & Stock Manager.";
                return LocalRedirect(ReturnUrl);
            }

            // Case 2: Standard Login without Secret Code -> automatically logged in as Customer (or their assigned role)
            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                ModelState.AddModelError(string.Empty, "Please enter your Email Address and Password.");
                return Page();
            }

            var account = await _accountRepository.GetByEmailOrUsernameAsync(Email.Trim());
            if (account == null || !DbInitializer.VerifyPassword(Password, account.PasswordHash))
            {
                ModelState.AddModelError(string.Empty, "Invalid email address or password. If you don't have an account, click 'Create one' below.");
                return Page();
            }

            // If account has no special role or wasn't authorized with secret code, role is Customer
            string role = string.IsNullOrEmpty(account.Role) || account.Role == "Customer" ? "Customer" : account.Role;

            var userClaims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, account.Id.ToString()),
                new Claim(ClaimTypes.Name, account.FullName),
                new Claim(ClaimTypes.Email, account.Email),
                new Claim(ClaimTypes.Role, role),
                new Claim("RoleDisplayName", role)
            };

            var userAuthIdentity = new ClaimsIdentity(userClaims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(userAuthIdentity));
            await _accountRepository.UpdateLastLoginAsync(account.Id);

            TempData["SuccessMessage"] = $"Welcome back, {account.FullName}! Logged in as {role}.";
            if (role == "Customer" && (ReturnUrl == "/Index" || string.IsNullOrWhiteSpace(ReturnUrl)))
            {
                return RedirectToPage("/Customer/Dashboard");
            }
            return LocalRedirect(ReturnUrl);
        }
    }
}
