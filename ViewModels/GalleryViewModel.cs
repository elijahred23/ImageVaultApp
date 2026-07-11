public class GalleryViewModel
{
    public IReadOnlyList<Image> Images { get; set; } = [];
    public string? SearchTerm { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalImages { get; set; }
    public int TotalPages => TotalImages == 0 ? 1 : (int)Math.Ceiling(TotalImages / (double)PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
    public int FirstItemIndex => TotalImages == 0 ? 0 : ((Page - 1) * PageSize) + 1;
    public int LastItemIndex => Math.Min(Page * PageSize, TotalImages);
    public List<UniqueTitle> UniqueTitles {get;set;} = new List<UniqueTitle>();
}
