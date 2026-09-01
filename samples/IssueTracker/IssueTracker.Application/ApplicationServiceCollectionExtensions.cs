using D20Tek.Vertically.Registration;
using IssueTracker.Application.Features.Issues;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Composition helpers for wiring the Issue Tracker Application layer (D20Tek.Vertically feature
/// handlers and validators) into a host's service collection.
/// </summary>
public static class ApplicationServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Issue Tracker Application layer: all Vertically feature handlers/validators from
    /// this assembly. Handler registration is intrinsic to the Application layer, but pipeline
    /// behaviors are a host policy decision, so hosts (Api/Cli/Web) supply their own set via
    /// <paramref name="configureBehaviors"/> (for example, an API may want exception-to-result while a
    /// CLI may prefer exceptions to bubble up).
    /// </summary>
    public static IServiceCollection AddIssueTrackerApplication(
        this IServiceCollection services,
        Action<IBehaviorRegistrationBuilder>? configureBehaviors = null) =>
        services.AddVertically(builder =>
        {
            builder.Handlers.RegisterFromAssembly(typeof(CreateIssue).Assembly);
            configureBehaviors?.Invoke(builder.Behaviors);
        });
}
