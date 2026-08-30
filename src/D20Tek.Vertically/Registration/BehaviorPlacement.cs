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
/// <param name="BehaviorType">The open generic behavior type definition.</param>
/// <param name="Kind">The placement strategy.</param>
/// <param name="Anchor">The anchor behavior type when <see cref="Kind"/> is <see cref="PlacementKind.Before"/>.</param>
internal readonly record struct BehaviorPlacement(Type BehaviorType, PlacementKind Kind, Type? Anchor);
