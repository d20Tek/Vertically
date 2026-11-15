using System.Diagnostics.CodeAnalysis;
using D20Tek.Vertically.Registration;
using IssueTracker.Application.Features.Issues;
using IssueTracker.Application.Features.Users;

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
    [RequiresUnreferencedCode("Uses assembly scanning. Use AddIssueTrackerApplicationTyped for trimmed or native AOT hosts.")]
    [RequiresDynamicCode("Uses assembly scanning. Use AddIssueTrackerApplicationTyped for trimmed or native AOT hosts.")]
    public static IServiceCollection AddIssueTrackerApplication(
        this IServiceCollection services,
        Action<IBehaviorRegistrationBuilder>? configureBehaviors = null) =>
        services.AddVertically(builder =>
        {
            builder.Handlers.RegisterFromAssembly(typeof(CreateIssue).Assembly);
            configureBehaviors?.Invoke(builder.Behaviors);
        });

    /// <summary>
    /// Registers the Issue Tracker Application layer without assembly scanning. Each feature is
    /// added explicitly with <c>AddFeature&lt;TFeature&gt;()</c>, and features register their handlers
    /// with the typed overloads, so this path is trim- and native AOT-compatible. Hosts supply
    /// their pipeline behaviors via <paramref name="configureBehaviors"/>.
    /// </summary>
    public static IServiceCollection AddIssueTrackerApplicationTyped(
        this IServiceCollection services,
        Action<IBehaviorRegistrationBuilder>? configureBehaviors = null) =>
        services.AddVertically(builder =>
        {
            builder.Handlers
                .AddFeature<CreateIssue>()
                .AddFeature<EditIssueDetails>()
                .AddFeature<AssignIssue>()
                .AddFeature<ChangeIssuePriority>()
                .AddFeature<ChangeIssueStatus>()
                .AddFeature<GetIssueById>()
                .AddFeature<GetIssueByKey>()
                .AddFeature<GetIssues>()
                .AddFeature<CreateUser>()
                .AddFeature<GetUsers>();
            configureBehaviors?.Invoke(builder.Behaviors);
        });
}
