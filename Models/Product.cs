using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace PMS.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Product name is required")]
        [StringLength(150, MinimumLength = 2, ErrorMessage = "Product name must be between 2 and 150 characters")]
        [Display(Name = "Product Name")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select a category")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a valid category")]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }

        [Display(Name = "Category")]
        public string? CategoryName { get; set; }

        [Required(ErrorMessage = "Price is required")]
        [Range(0.01, 10000000.00, ErrorMessage = "Price must be greater than 0")]
        [DataType(DataType.Currency)]
        [Display(Name = "Price (PKR)")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Stock quantity is required")]
        [Range(0, 100000, ErrorMessage = "Stock quantity must be 0 or greater")]
        [Display(Name = "Stock Quantity")]
        public int Stock { get; set; }

        [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
        public string? Description { get; set; }

        [Display(Name = "Product Image")]
        public string? ImageUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Helper calculations
        public bool IsLowStock => Stock > 0 && Stock <= 5;
        public bool IsOutOfStock => Stock == 0;
        public bool IsInStock => Stock > 5;
    }
}

