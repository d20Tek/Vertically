namespace D20Tek.Vertically.Tests.Fakes;

/// <summary>Sample command returning a string result, used across registration/behavior tests.</summary>
public sealed record SampleCommand(string Value) : ICommand<string>;

/// <summary>A second sample command used to test per-handler scoping in isolation.</summary>
public sealed record OtherCommand(int Value) : ICommand<int>;

/// <summary>Command handled by the throwing handler, kept distinct to avoid scan conflicts.</summary>
public sealed record ThrowingCommand(string Value) : ICommand<string>;

/// <summary>Sample query returning a string result.</summary>
[ExcludeFromCodeCoverage]
public sealed record SampleQuery(string Value) : IQuery<string>;

