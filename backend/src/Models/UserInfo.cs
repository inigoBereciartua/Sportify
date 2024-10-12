public class UserInfo
{
    public string Email { get; set; }
    public string Username { get; set; }

    public UserInfo(string email, string username)
    {
        Email = email;
        Username = username;
    }
}