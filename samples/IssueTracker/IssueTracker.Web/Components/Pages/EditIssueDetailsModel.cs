using System.ComponentModel.DataAnnotations;
using IssueTracker.Application.Features.Issues;

namespace IssueTracker.Web.Components.Pages;

/// <summary>
/// Mutable form-bound model for inline editing of an issue's title and description on the detail page.
/// Authoritative validation is performed by the Application pipeline; the light annotations only support
/// the client-side <see cref="EditForm"/>.
/// </summary>
public sealed class EditIssueDetailsModel
{
    /// <summary>Short summary of the issue.</summary>
    [Required(ErrorMessage = "Title is required.")]
    [StringLength(CreateIssue.MaxTitleLength, ErrorMessage = "Title must not exceed {1} characters.")]
    public string Title { get; set; } = string.Empty;

    /// <summary>Optional detailed description.</summary>
    [StringLength(CreateIssue.MaxDescriptionLength, ErrorMessage = "Description must not exceed {1} characters.")]
    public string? Description { get; set; }

    /// <summary>Resets the model's fields from an existing issue.</summary>
    public void LoadFrom(IssueResponse issue)
    {
        Title = issue.Title;
        Description = issue.Description;
    }

    /// <summary>Projects the form model into the Application edit command for the given issue.</summary>
    public EditIssueDetails.Command ToCommand(Guid issueId) => new(issueId, Title.Trim(), Description);
}
