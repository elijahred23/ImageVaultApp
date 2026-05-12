public class ImageEditViewModel
{
    public int Id {get;set;}
    public string? Title {get;set;}
    public string? Description {get;set;}
    public bool IsNSFW {get;set;}
    public string? ExistingImagePath {get;set;}
    public IFormFile? NewFile { get; set; }
}