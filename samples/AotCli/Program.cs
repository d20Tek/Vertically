using AotCli;
using D20Tek.Vertically;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddVertically(builder =>
{
    builder.Handlers.AddFeature<PrintMessage>();
    builder.Behaviors.AddExceptionToResult()
                     .AddValidation();
});

using var provider = services.BuildServiceProvider(new ServiceProviderOptions
{
    ValidateOnBuild = true,
    ValidateScopes = true,
});
using var scope = provider.CreateScope();
var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<PrintMessage.Command, string>>();

var message = args.Length > 0 ? string.Join(' ', args) : "Hello from a native AOT Vertically CLI!";
var result = await handler.HandleAsync(new PrintMessage.Command(message));

return result.Match(
    value =>
    {
        Console.WriteLine($"[result] success: {value}");
        return 0;
    },
    errors =>
    {
        foreach (var error in errors)
        {
            Console.WriteLine($"[result] failure: {error.Code} - {error.Message}");
        }
        return 1;
    });
