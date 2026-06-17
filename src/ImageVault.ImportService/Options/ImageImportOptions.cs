namespace ImageVault.ImportService.Options;

public sealed class ImageImportOptions
{
    public const string SectionName = "ImageImporter";

    public int UserId { get; set; } = 1;
    public string DropRoot { get; set; } = "DropFolders";
    public string ProcessFolder { get; set; } = "process";
    public string ProcessNsfwFolder { get; set; } = "process-nsfw";
    public string ProcessingFolder { get; set; } = "processing";
    public string ProcessedFolder { get; set; } = "processed";
    public string ErrorFolder { get; set; } = "error";
    public string LogFolder { get; set; } = "logs";
    public int FileReadyRetryCount { get; set; } = 10;
    public int FileReadyRetryDelayMilliseconds { get; set; } = 500;
}
