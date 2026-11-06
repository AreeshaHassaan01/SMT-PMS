using System.ComponentModel.DataAnnotations;

namespace PMS.Models
{
    public class UserProfile
    {
        public int Id { get; set; }
        public int UserId { get; set; }

        [Display(Name = "Full Name")]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Display(Name = "Phone Number")]
        [StringLength(20)]
        public string? Phone { get; set; }

        [Display(Name = "Street Address")]
        [StringLength(300)]
        public string? Address { get; set; }

        [Display(Name = "City")]
        [StringLength(100)]
        public string? City { get; set; }

        [Display(Name = "Province / State")]
        [StringLength(100)]
        public string? Province { get; set; }

        [Display(Name = "Postal Code")]
        [StringLength(20)]
        public string? PostalCode { get; set; }

        [Display(Name = "Country")]
        [StringLength(100)]
        public string Country { get; set; } = "Pakistan";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Helper — full shipping address string
        public string ShippingAddress =>
            string.Join(", ", new[] { Address, City, Province, PostalCode, Country }
                .Where(s => !string.IsNullOrWhiteSpace(s)));
    }
}

