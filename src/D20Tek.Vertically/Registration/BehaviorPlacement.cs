namespace D20Tek.Vertically.Registration;

/// <summary>
/// Where a per-handler behavior sits relative to the existing pipeline for that handler.
/// </summary>
internal enum PlacementKind
{
    /// <summary>Closest to the handler, inside all existing behaviors (default).</summary>
    Innermost,

    /// <summary>Outside all existing behaviors (runs first on the way in).</summary>
    Outermost,

    /// <summary>Immediately outside a named anchor behavior in the pipeline.</summary>
    Before,
}

/// <summary>
/// A single per-handler behavior registration together with its requested placement.
/// </summary>
/// <param name="Behavior">The behavior descriptor.</param>
/// <param name="Kind">The placement strategy.</param>
/// <param name="Anchor">
/// The anchor behavior identity when <see cref="Kind"/> is <see cref="PlacementKind.Before"/>
/// (an open generic definition, or a closed type for typed behaviors).
/// </param>
internal readonly record struct BehaviorPlacement(BehaviorDescriptor Behavior, PlacementKind Kind, Type? Anchor);
