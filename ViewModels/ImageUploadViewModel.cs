
namespace ImageVaultApp.ViewModels;
public class ImageUploadViewModel
{
    public IFormFile File {get;set;}
    public string? Title {get;set;}
    public string? Description {get;set;}
    public bool IsNSFW {get;set;}
}
