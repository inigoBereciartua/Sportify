public class UserInfo
{
    public string Id { get; set; }
    public string Email { get; set; }
    public string Username { get; set; }

    public UserInfo(string id, string email, string username)
    {
        Id = id;
        Email = email;
        Username = username;
    }
}