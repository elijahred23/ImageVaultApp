namespace ImageVault.ImportService.Models;

public class Image
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string? ImageUrl { get; set; } = "";
    public string? Title { get; set; }
    public string? Description { get; set; }
    public bool IsNSFW { get; set; }
    public string? MimeType { get; set; }
    public long? FileSizeBytes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
