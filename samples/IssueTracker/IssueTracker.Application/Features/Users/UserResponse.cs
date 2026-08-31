using IssueTracker.Application.Domain;

namespace IssueTracker.Application.Features.Users;

/// <summary>
/// Host-agnostic summary projection of a <see cref="User"/> used to populate assignee selectors.
/// </summary>
public sealed record UserResponse(Guid Id, string FullName, string Email)
{
    /// <summary>Creates a <see cref="UserResponse"/> from a domain <see cref="User"/>.</summary>
    public static UserResponse FromUser(User user) => new(user.Id, user.FullName, user.Email);
}
