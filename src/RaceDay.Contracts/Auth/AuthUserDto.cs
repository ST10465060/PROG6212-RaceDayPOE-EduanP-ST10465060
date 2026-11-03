namespace RaceDay.Contracts.Auth;

// What we hand back after a successful register or login. 
// The MVC app storesthese values in session so it knows who is signed in and what they may do.
// The password hash is deliberately absent, it must never leave the API.
public class AuthUserDto
{
    public int UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
}
