namespace D20Tek.Vertically.Registration;

/// <summary>
/// Owns global and per-handler pipeline behavior placement intent, and computes the ordered
/// behavior entries that apply to a given handler. Extracted from
/// <see cref="VerticallyBuilder"/> to keep behavior-placement concerns cohesive.
/// </summary>
internal sealed class BehaviorRegistry
{
    private readonly List<BehaviorDescriptor> _globalBehaviors = [];
    private readonly Dictionary<Type, List<BehaviorPlacement>> _handlerBehaviors = [];

    internal void AddGlobal(BehaviorDescriptor behavior)
    {
        if (!_globalBehaviors.Exists(b => b.Identity == behavior.Identity))
        {
            _globalBehaviors.Add(behavior);
        }
    }

    internal void AddForHandler(Type requestType, BehaviorPlacement placement)
    {
        if (!_handlerBehaviors.TryGetValue(requestType, out var placements))
        {
            placements = [];
            _handlerBehaviors[requestType] = placements;
        }

        placements.Add(placement);
    }

    /// <summary>
    /// Returns the ordered behavior entries that apply to a given handler, outermost first.
    /// Starts from the global behaviors (registration order) and merges in any per-handler
    /// behaviors, which sit innermost by default unless a placement override
    /// (<see cref="PlacementKind.Outermost"/> / <see cref="PlacementKind.Before"/>) is set.
    /// </summary>
    internal IReadOnlyList<BehaviorEntry> GetEntriesFor(HandlerRegistration registration)
    {
        var ordered = _globalBehaviors.ConvertAll(b => new BehaviorEntry(b, IsRequired: false));
        if (!_handlerBehaviors.TryGetValue(registration.RequestType, out var placements))
            return ordered;

        foreach (var placement in placements)
        {
            var entry = new BehaviorEntry(placement.Behavior, IsRequired: true);
            switch (placement.Kind)
            {
                case PlacementKind.Outermost:
                    ordered.Insert(0, entry);
                    break;

                case PlacementKind.Before:
                    var anchorIndex = ordered.FindIndex(e => e.Descriptor.Identity == placement.Anchor);
                    if (anchorIndex < 0)
                    {
                        throw new InvalidOperationException(
                            $"Cannot place behavior '{placement.Behavior.Identity}' before " +
                            $"'{placement.Anchor}' for request '{registration.RequestType}' " +
                            "because the anchor behavior is not part of this handler's pipeline.");
                    }

                    ordered.Insert(anchorIndex, entry);
                    break;

                default:
                    ordered.Add(entry);
                    break;
            }
        }

        return ordered;
    }
}
