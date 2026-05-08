public class UserSettings
{
    public int Id {get;set;}
    public int UserId {get;set;}
    public bool AllowNSFW {get;set;} = false;
    public bool BlurNSFW {get;set;} = true;

    public DateTime CreatedAt {get;set;} = DateTime.UtcNow;
}