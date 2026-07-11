namespace ImageVaultApp.ViewModels;

public class ImageUploadPageViewModel
{
    public ImageUploadViewModel Upload { get; set; } = new();
    public ImageImportUploadViewModel Import { get; set; } = new();
    public string ActiveTab { get; set; } = "images";
}
