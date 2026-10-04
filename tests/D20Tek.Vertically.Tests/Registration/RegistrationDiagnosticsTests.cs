namespace D20Tek.Vertically.Tests.Registration;

[TestClass]
public sealed class RegistrationDiagnosticsTests
{
    private static Assembly TestAssembly => typeof(SampleFeature).Assembly;

    [TestMethod]
    public void InternalValidatorRegistrations_DedupedSameServiceAndImplementation_ReturnsSingleEntry()
    {
        // Arrange
        var services = new ServiceCollection();
        IVerticallyBuilder? captured = null;

        // Act
        services.AddVertically(b =>
        {
            b.Handlers.AddValidator<SampleCommandValidator>();
            b.Handlers.AddValidator<SampleCommandValidator>();
            captured = b;
        });

        // Assert
        var builder = (VerticallyBuilder)captured!;
        var registrations = builder.InternalValidatorRegistrations
            .Where(r => r.ImplementationType == typeof(SampleCommandValidator))
            .ToArray();
        Assert.HasCount(1, registrations);
        Assert.AreEqual(typeof(IValidator<SampleCommand>), registrations[0].ServiceType);
    }

    [TestMethod]
    public void HandlerRegistrations_ManualAdd_TaggedAsManual()
    {
        // Arrange
        var services = new ServiceCollection();
        IVerticallyBuilder? captured = null;

        // Act
        services.AddVertically(b =>
        {
            b.Handlers.AddCommandHandler<SampleCommandHandler>();
            captured = b;
        });

        // Assert
        Assert.IsNotNull(captured);
        var registration = captured.HandlerRegistrations.Single(r => r.RequestType == typeof(SampleCommand));
        Assert.AreEqual(RegistrationSource.Manual, registration.Source);
    }

    [TestMethod]
    public void ValidatorRegistrations_ManualAdd_TaggedAsManual()
    {
        // Arrange
        var services = new ServiceCollection();
        IVerticallyBuilder? captured = null;

        // Act
        services.AddVertically(b =>
        {
            b.Handlers.AddValidator<SampleCommandValidator>();
            captured = b;
        });

        // Assert
        Assert.IsNotNull(captured);
        var registration = captured.ValidatorRegistrations
            .Single(r => r.ServiceType == typeof(IValidator<SampleCommand>));
        Assert.AreEqual(RegistrationSource.Manual, registration.Source);
    }

    [TestMethod]
    public void HandlerRegistrations_FeatureRegisteredViaScan_TaggedAsFeature()
    {
        // Arrange
        var services = new ServiceCollection();
        IVerticallyBuilder? captured = null;

        // Act
        services.AddVertically(b =>
        {
            b.Handlers.RegisterFromAssembly(TestAssembly);
            captured = b;
        });

        // Assert
        Assert.IsNotNull(captured);
        var registration = captured.HandlerRegistrations.Single(r => r.RequestType == typeof(SampleFeature.Command));
        Assert.AreEqual(RegistrationSource.Feature, registration.Source);

        var validatorRegistration = captured.ValidatorRegistrations
            .Single(r => r.ServiceType == typeof(IValidator<SampleFeature.Command>));
        Assert.AreEqual(RegistrationSource.Feature, validatorRegistration.Source);
    }

    [TestMethod]
    public void HandlerRegistrations_ScanDiscovered_TaggedAsScan()
    {
        // Arrange
        var services = new ServiceCollection();
        IVerticallyBuilder? captured = null;

        // Act
        services.AddVertically(b =>
        {
            b.Handlers.RegisterFromAssembly(TestAssembly);
            captured = b;
        });

        // Assert
        Assert.IsNotNull(captured);
        var registration = captured.HandlerRegistrations.Single(r => r.RequestType == typeof(SampleCommand));
        Assert.AreEqual(RegistrationSource.Scan, registration.Source);
    }

    [TestMethod]
    public void RegisterFromAssembly_FeatureOwnedHandler_TaggedAsFeature_NotScan()
    {
        // Arrange
        var services = new ServiceCollection();
        IVerticallyBuilder? captured = null;

        // Act
        services.AddVertically(b =>
        {
            b.Handlers.RegisterFromAssembly(TestAssembly);
            captured = b;
        });

        // Assert
        Assert.IsNotNull(captured);
        var registration = captured.HandlerRegistrations.Single(r => r.RequestType == typeof(SampleFeature.Command));
        Assert.AreEqual(RegistrationSource.Feature, registration.Source);
    }

    [TestMethod]
    public void PrintRegistrations_IncludesHandlerAndValidatorEntriesWithSource()
    {
        // Arrange
        var services = new ServiceCollection();
        IVerticallyBuilder? captured = null;
        services.AddVertically(b =>
        {
            b.Handlers.AddCommandHandler<SampleCommandHandler>();
            b.Handlers.AddValidator<SampleCommandValidator>();
            captured = b;
        });

        // Act
        var output = VerticallyDiagnostics.PrintRegistrations(captured!);

        // Assert
        Assert.Contains("Handler registrations:", output);
        Assert.Contains("Validator registrations:", output);
        Assert.Contains(nameof(SampleCommandHandler), output);
        Assert.Contains(nameof(RegistrationSource.Manual), output);
    }

    [TestMethod]
    public void PrintRegistrations_QueryHandler_LabeledAsQuery()
    {
        // Arrange
        var services = new ServiceCollection();
        IVerticallyBuilder? captured = null;
        services.AddVertically(b =>
        {
            b.Handlers.AddQueryHandler<SampleQueryHandler>();
            captured = b;
        });

        // Act
        var output = VerticallyDiagnostics.PrintRegistrations(captured!);

        // Assert
        Assert.Contains("Query", output);
        Assert.Contains(nameof(SampleQueryHandler), output);
    }

    [TestMethod]
    public void PrintRegistrations_NoRegistrations_ReturnsNoneMarkers()
    {
        // Arrange
        var services = new ServiceCollection();
        IVerticallyBuilder? captured = null;
        services.AddVertically(b => captured = b);

        // Act
        var output = VerticallyDiagnostics.PrintRegistrations(captured!);

        // Assert
        Assert.Contains("(none)", output);
    }

    [TestMethod]
    public void PrintRegistrations_NullBuilder_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsExactly<ArgumentNullException>(ActThrows);

        [ExcludeFromCodeCoverage]
        static void ActThrows() => VerticallyDiagnostics.PrintRegistrations(null!);
    }

    [TestMethod]
    public void PrintDuplicateRegistrations_NoOverlap_ReportsNoDuplicates()
    {
        // Arrange
        var services = new ServiceCollection();
        IVerticallyBuilder? captured = null;
        services.AddVertically(b =>
        {
            b.Handlers.AddCommandHandler<SampleCommandHandler>();
            captured = b;
        });

        // Act
        var output = VerticallyDiagnostics.PrintDuplicateRegistrations(captured!);

        // Assert
        Assert.Contains("No duplicate/overlapping registrations found.", output);
    }

    [TestMethod]
    public void PrintDuplicateRegistrations_FeatureAndScanOverlap_ReportsDuplicate()
    {
        // Arrange
        var services = new ServiceCollection();
        IVerticallyBuilder? captured = null;

        // Act
        services.AddVertically(b =>
        {
            // Register the feature's handler explicitly first (simulating a manual + feature overlap),
            // then scan the assembly, which re-registers the feature (same service/impl pair, deduped)
            // but still logs an additional registration attempt for the duplicate report.
            new SampleFeature().Register(b);
            b.Handlers.RegisterFromAssembly(TestAssembly);
            captured = b;
        });
        var output = VerticallyDiagnostics.PrintDuplicateRegistrations(captured!);

        // Assert
        Assert.Contains("Duplicate handler registrations:", output);
        Assert.Contains(nameof(SampleFeature.Command), output);
    }

    [TestMethod]
    public void PrintDuplicateRegistrations_NullBuilder_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.ThrowsExactly<ArgumentNullException>(ActThrows);

        [ExcludeFromCodeCoverage]
        static void ActThrows() => VerticallyDiagnostics.PrintDuplicateRegistrations(null!);
    }

    [TestMethod]
    public void PrintDuplicateRegistrations_BuilderIsNotInternalImplementation_ThrowsArgumentException()
    {
        // Arrange
        var builder = new FakeVerticallyBuilder();

        // Act & Assert
        Assert.ThrowsExactly<ArgumentException>(ActThrows);

        [ExcludeFromCodeCoverage]
        void ActThrows() => VerticallyDiagnostics.PrintDuplicateRegistrations(builder);
    }
}
