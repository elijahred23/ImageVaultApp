namespace ImageVaultApp.ViewModels;

public class ImageImportUploadViewModel
{
    public List<IFormFile> JsonFiles { get; set; } = [];
    public bool IsNSFW { get; set; }
    public int ImportedFileCount { get; set; }
    public string? StatusMessage { get; set; }
    public string? DropFolderPath { get; set; }
}
