namespace D20Tek.Vertically.Registration;

/// <summary>
/// On-demand debugging helpers that render the handler/validator registrations collected by an
/// <see cref="IVerticallyBuilder"/>, including their <see cref="RegistrationSource"/>. Intended
/// for ad-hoc diagnostics (console/log output), not as an always-on feature.
/// </summary>
public static class VerticallyDiagnostics
{
    /// <summary>
    /// Builds a formatted table of all collected handler and validator registrations, including
    /// the request/command type, the handler/validator implementation, and the
    /// <see cref="RegistrationSource"/> that produced each entry.
    /// </summary>
    /// <param name="builder">The builder whose registrations should be rendered.</param>
    /// <returns>A formatted, multi-line string suitable for writing to a console or logger.</returns>
    public static string PrintRegistrations(IVerticallyBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var sb = new StringBuilder();

        sb.AppendLine("Handler registrations:");
        if (builder.HandlerRegistrations.Count == 0)
        {
            sb.AppendLine("  (none)");
        }
        else
        {
            foreach (var registration in builder.HandlerRegistrations)
            {
                var kind = registration.IsCommand ? "Command" : "Query";
                sb.AppendLine(
                    $"  [{registration.Source,-9}] {kind,-7} {registration.RequestType.Name,-30} -> " +
                    $"{registration.ImplementationType.Name}");
            }
        }

        sb.AppendLine("Validator registrations:");
        if (builder.ValidatorRegistrations.Count == 0)
        {
            sb.AppendLine("  (none)");
        }
        else
        {
            foreach (var registration in builder.ValidatorRegistrations)
            {
                sb.AppendLine(
                    $"  [{registration.Source,-9}] {registration.ServiceType.Name,-30} -> " +
                    $"{registration.ImplementationType.Name}");
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Scans the collected handler and validator registration attempts for overlap: the same
    /// service type being registered more than once (e.g. once by a feature and again by the
    /// loose assembly scan, or across two different implementations). Exact duplicate
    /// (service, implementation) pairs registered from the same or different sources are
    /// included, since they still represent redundant registration attempts worth surfacing.
    /// </summary>
    /// <param name="builder">The builder whose registrations should be checked for overlap.</param>
    /// <returns>
    /// A formatted, multi-line string describing each service type with more than one
    /// registration attempt, or a message indicating no duplicates were found.
    /// </returns>
    public static string PrintDuplicateRegistrations(IVerticallyBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (builder is not VerticallyBuilder verticallyBuilder)
        {
            throw new ArgumentException(
                $"The builder must be the internal '{nameof(VerticallyBuilder)}' implementation.",
                nameof(builder));
        }

        var sb = new StringBuilder();

        var duplicateHandlers = verticallyBuilder.HandlerRegistrationAttempts
            .GroupBy(r => r.ServiceType)
            .Where(g => g.Count() > 1)
            .ToArray();

        var duplicateValidators = verticallyBuilder.ValidatorRegistrationAttempts
            .GroupBy(r => r.ServiceType)
            .Where(g => g.Count() > 1)
            .ToArray();

        if (duplicateHandlers.Length == 0 && duplicateValidators.Length == 0)
        {
            sb.AppendLine("No duplicate/overlapping registrations found.");
            return sb.ToString();
        }

        if (duplicateHandlers.Length > 0)
        {
            sb.AppendLine("Duplicate handler registrations:");
            foreach (var group in duplicateHandlers)
            {
                sb.AppendLine($"  {group.Key.Name}:");
                foreach (var registration in group)
                {
                    sb.AppendLine(
                        $"    [{registration.Source,-9}] {registration.ImplementationType.Name}");
                }
            }
        }

        if (duplicateValidators.Length > 0)
        {
            sb.AppendLine("Duplicate validator registrations:");
            foreach (var group in duplicateValidators)
            {
                sb.AppendLine($"  {group.Key.Name}:");
                foreach (var registration in group)
                {
                    sb.AppendLine(
                        $"    [{registration.Source,-9}] {registration.ImplementationType.Name}");
                }
            }
        }

        return sb.ToString();
    }
}
