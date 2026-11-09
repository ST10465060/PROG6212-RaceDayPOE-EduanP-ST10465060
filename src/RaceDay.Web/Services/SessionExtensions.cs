using RaceDay.Contracts.Auth;

namespace RaceDay.Web.Services;

/// <summary>
/// Wraps the session keys so no controller ever types a raw string.
/// One typo in "UserId" would silently log the user out, and that is a
/// miserable bug to track down.
/// </summary>
public static class SessionExtensions
{
    private const string UserIdKey = "UserId";
    private const string UserNameKey = "UserName";
    private const string UserRoleKey = "UserRole";
    private const string UserPictureKey = "UserPicture";

    public static void SignIn(this ISession session, AuthUserDto user)
    {
        session.SetInt32(UserIdKey, user.UserId);
        session.SetString(UserNameKey, $"{user.FirstName} {user.LastName}");
        session.SetString(UserRoleKey, user.Role);

        // Nullable, so only store it when there is actually a picture.
        if (!string.IsNullOrWhiteSpace(user.ProfilePictureUrl))
        {
            session.SetString(UserPictureKey, user.ProfilePictureUrl);
        }
    }

    public static int? GetUserId(this ISession session) =>
        session.GetInt32(UserIdKey);

    public static string? GetUserName(this ISession session) =>
        session.GetString(UserNameKey);

    public static string? GetUserRole(this ISession session) =>
        session.GetString(UserRoleKey);

    public static string? GetUserPicture(this ISession session) =>
        session.GetString(UserPictureKey);

    public static bool IsLoggedIn(this ISession session) =>
        session.GetInt32(UserIdKey).HasValue;

    public static bool IsOrganiser(this ISession session) =>
        session.GetString(UserRoleKey) == "Organiser";

    public static bool IsParticipant(this ISession session) =>
        session.GetString(UserRoleKey) == "Participant";
}