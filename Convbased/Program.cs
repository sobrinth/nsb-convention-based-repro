using System.Reflection;

var builder = WebApplication.CreateSlimBuilder(args);

var endpointConfiguration = new EndpointConfiguration("Convbased");

endpointConfiguration.UseTransport(new LearningTransport());
endpointConfiguration.UsePersistence<LearningPersistence>();

endpointConfiguration.UseSerialization<SystemJsonSerializer>();

endpointConfiguration.EnableInstallers();
endpointConfiguration.Conventions()
    .DefiningCommandsAs(IsCommand);

// Disabling the AssemblyScanner explicitly does lead to the message only being processed once, otherwise it's done twice
// endpointConfiguration.AssemblyScanner().Disable = true;
endpointConfiguration.Handlers.ConvbasedAssembly.AddAll();

builder.Services.AddNServiceBusEndpoint(endpointConfiguration);

var app = builder.Build();

app.MapGet("send/{msg}", async (string msg, IMessageSession session) => { await session.SendLocal(new ReproMessage(msg)); });
await app.RunAsync();

static bool IsCommand(Type type) => type.GetCustomAttribute<CommandAttribute>() is not null;

[Command]
public sealed record ReproMessage(string Text);

[AttributeUsage(AttributeTargets.Class)]
public sealed class CommandAttribute : Attribute;


[Handler]
internal sealed class ReproMessageHandler : IHandleMessages<ReproMessage>
{
    public async Task Handle(ReproMessage message, IMessageHandlerContext context)
    {
        Console.WriteLine($"Processing message: Text: `{message.Text}`");
        await Task.CompletedTask;
    }
}