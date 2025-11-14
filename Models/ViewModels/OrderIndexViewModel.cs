namespace ABCRetailers.Models.ViewModels
{
    public class OrderIndexViewModel
    {
        public List<Order> Orders { get; set; } = new();

        public string Search { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Sort { get; set; } = "newest";
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalCount { get; set; }

        // Make TotalPages a computed property (read-only)
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }
}