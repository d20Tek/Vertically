namespace D20Tek.Vertically.Tests.Fakes;

/// <summary>
/// Configurable validator for <see cref="SampleCommand"/>. When <see cref="ShouldFail"/> is true,
/// it reports a validation error; otherwise it returns no errors. Records invocation for lazy-
/// resolution tests.
/// </summary>
public sealed class SampleCommandValidator : IValidator<SampleCommand>
{
    public const string ErrorCode = "sample.invalid";

    public bool ShouldFail { get; set; }

    public int CallCount { get; private set; }

    public ValidationErrors Validate(SampleCommand input)
    {
        CallCount++;

        var errors = ValidationErrors.Create();
        errors.AddIfError(() => ShouldFail, ErrorCode, "sample command is invalid");
        return errors;
    }
}

/// <summary>A second validator for <see cref="SampleCommand"/> to test multiple-validator behavior.</summary>
public sealed class SecondSampleCommandValidator : IValidator<SampleCommand>
{
    public const string ErrorCode = "sample.invalid.second";

    public bool ShouldFail { get; set; }

    public int CallCount { get; private set; }

    public ValidationErrors Validate(SampleCommand input)
    {
        CallCount++;

        var errors = ValidationErrors.Create();
        errors.AddIfError(() => ShouldFail, ErrorCode, "sample command failed second validator");
        return errors;
    }
}

/// <summary>Validator for <see cref="SampleCommand"/> that always fails, for end-to-end short-circuit tests.</summary>
public sealed class FailingSampleCommandValidator : IValidator<SampleCommand>
{
    public const string ErrorCode = "sample.always.invalid";

    public ValidationErrors Validate(SampleCommand input)
    {
        var errors = ValidationErrors.Create();
        errors.AddIfError(() => true, ErrorCode, "sample command always fails");
        return errors;
    }
}

