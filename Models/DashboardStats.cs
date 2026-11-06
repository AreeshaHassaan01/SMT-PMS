namespace PMS.Models
{
    public class DashboardStats
    {
        public int TotalProducts { get; set; }
        public int TotalCategories { get; set; }
        public int InStock { get; set; }
        public int LowStock { get; set; }
        public int OutOfStock { get; set; }
    }
}

