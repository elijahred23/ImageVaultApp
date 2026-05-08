
namespace ImageVaultApp.ViewModels;
public class ImageUploadViewModel
{
    public string? ImageUrl {get;set;}
    public IFormFile File {get;set;}
    public string? Title {get;set;}
    public string? Description {get;set;}
    public bool IsNSFW {get;set;}
    public string? MimeType {get;set;}
    public long? FileSizeBytes {get;set;}
}
