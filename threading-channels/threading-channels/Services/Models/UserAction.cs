namespace threading_channels.Services.Models;

public class UserAction
{
    public int Id { get; set; }
    public string UserId { get; set; }
    public string Action { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
}