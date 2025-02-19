// See https://aka.ms/new-console-template for more information
using Microsoft.Extensions.Logging;
using Pulumi;
using Pulumi.Automation;

class Program
{

    public const string ParentOutput = "hello from parent";

    static async Task Main(string[] args)
    {
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .AddFilter("Microsoft", LogLevel.Warning)
                .AddFilter("System", LogLevel.Warning)
                .AddFilter("LoggingConsoleApp.Program", LogLevel.Debug)
                .AddConsole();
        });
        ILogger logger = loggerFactory.CreateLogger<Program>();
        logger.LogInformation("Example log message");

        var parentProgram = PulumiFn.Create(() => {
            return new Dictionary<string, object?>
            {
                [nameof(ParentOutput)] = ParentOutput,
            };
        });

        var orgName = "rnd_digizuite";
        if (args.Any() && args[0] == "keyshot") {
          orgName = "KeyShot";
        }

        logger.LogInformation("orgname is {orgName}", orgName);

        var projectName = "keyshot-stackreference-reproduction";
        var stackName = "stackreference-reproduction";

        var stackArgs = new InlineProgramArgs(projectName, stackName, parentProgram);
        var stack = await LocalWorkspace.CreateOrSelectStackAsync(stackArgs);
        
        await stack.RefreshAsync(new() {
            OnStandardOutput = Console.WriteLine,
            OnStandardError = Console.WriteLine
        });

        await stack.UpAsync(new() {
            OnStandardOutput = Console.WriteLine,
            OnStandardError = Console.WriteLine
        });

        var childProgram = PulumiFn.Create(() => {
          var stackRef = new StackReference($"{orgName}/{projectName}/{stackName}");
          var valueFromParent = stackRef.RequireOutput(nameof(ParentOutput));
          return new Dictionary<string, object?> {
            ["ChildOutput"] = valueFromParent
          };
        });

        var stackNameChild = "stackreference-reproduction-child";

        var stackArgsChild = new InlineProgramArgs(projectName, stackNameChild, childProgram);
        var stackChild = await LocalWorkspace.CreateOrSelectStackAsync(stackArgsChild);

        await stackChild.RefreshAsync(new() {
            OnStandardOutput = Console.WriteLine,
            OnStandardError = Console.WriteLine
        });

        await stackChild.UpAsync(new() {
            OnStandardOutput = Console.WriteLine,
            OnStandardError = Console.WriteLine
        });

        logger.LogInformation("Done diddly done.");
    }
}
