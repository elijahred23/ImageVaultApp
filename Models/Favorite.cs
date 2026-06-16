public class Favorite
{
    public int Id {get;set;}
    public int UserId {get;set;}
    public int ImageId {get;set;}
    public DateTime CreatedAt {get;set;} = DateTime.UtcNow;
}
