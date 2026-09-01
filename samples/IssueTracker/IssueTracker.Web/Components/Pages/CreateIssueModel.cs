using System.ComponentModel.DataAnnotations;
using IssueTracker.Application.Domain;
using IssueTracker.Application.Features.Issues;

namespace IssueTracker.Web.Components.Pages;

/// <summary>
/// Mutable form-bound model for the create-issue page. Editable inputs bind here, and the model is
/// projected into a <see cref="CreateIssue.Command"/> on submit. Authoritative validation is performed
/// by the Application pipeline; the light annotations only support the client-side <see cref="EditForm"/>.
/// </summary>
public sealed class CreateIssueModel
{
    /// <summary>Short summary of the issue.</summary>
    [Required(ErrorMessage = "Title is required.")]
    [StringLength(CreateIssue.MaxTitleLength, ErrorMessage = "Title must not exceed {1} characters.")]
    public string Title { get; set; } = string.Empty;

    /// <summary>Optional detailed description.</summary>
    [StringLength(CreateIssue.MaxDescriptionLength, ErrorMessage = "Description must not exceed {1} characters.")]
    public string? Description { get; set; }

    /// <summary>Selected priority for the new issue.</summary>
    public IssuePriority Priority { get; set; } = IssuePriority.Medium;

    /// <summary>Projects the form model into the Application create command.</summary>
    public CreateIssue.Command ToCommand() => new(Title.Trim(), Description, Priority);
}
