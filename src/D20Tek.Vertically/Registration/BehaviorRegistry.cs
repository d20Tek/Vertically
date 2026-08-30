namespace D20Tek.Vertically.Registration;

/// <summary>
/// Owns global and per-handler pipeline behavior placement intent, and computes the ordered
/// open-generic behavior definitions that apply to a given handler. Extracted from
/// <see cref="VerticallyBuilder"/> to keep behavior-placement concerns cohesive.
/// </summary>
internal sealed class BehaviorRegistry
{
    private readonly List<Type> _globalBehaviors = [];
    private readonly Dictionary<Type, List<BehaviorPlacement>> _handlerBehaviors = [];

    internal void AddGlobal(Type openGenericBehaviorType)
    {
        if (!_globalBehaviors.Contains(openGenericBehaviorType))
        {
            _globalBehaviors.Add(openGenericBehaviorType);
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
    /// Returns the ordered open-generic behavior definitions that apply to a given handler,
    /// outermost first. Starts from the global behaviors (registration order) and merges in
    /// any per-handler behaviors, which sit innermost by default unless a placement override
    /// (<see cref="PlacementKind.Outermost"/> / <see cref="PlacementKind.Before"/>) is set.
    /// </summary>
    internal IReadOnlyList<Type> GetDefinitionsFor(HandlerRegistration registration)
    {
        if (!_handlerBehaviors.TryGetValue(registration.RequestType, out var placements) || placements.Count == 0)
            return _globalBehaviors;

        var ordered = new List<Type>(_globalBehaviors);
        foreach (var placement in placements)
        {
            switch (placement.Kind)
            {
                case PlacementKind.Outermost:
                    ordered.Insert(0, placement.BehaviorType);
                    break;

                case PlacementKind.Before:
                    var anchorIndex = ordered.IndexOf(placement.Anchor!);
                    if (anchorIndex < 0)
                    {
                        throw new InvalidOperationException(
                            $"Cannot place behavior '{placement.BehaviorType}' before " +
                            $"'{placement.Anchor}' for request '{registration.RequestType}' " +
                            "because the anchor behavior is not part of this handler's pipeline.");
                    }

                    ordered.Insert(anchorIndex, placement.BehaviorType);
                    break;

                default:
                    ordered.Add(placement.BehaviorType);
                    break;
            }
        }

        return ordered;
    }
}
