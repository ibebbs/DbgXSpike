// See https://aka.ms/new-console-template for more information
using DbgX;
using DbgX.Interfaces.Services;
using DbgX.Requests;
using DbgX.Requests.Initialization;
using Nito.AsyncEx;
using WindowsDebugger.DbgEng;

var dumpFile = Environment.GetCommandLineArgs().Skip(1).First();

Console.WriteLine($"Extracting exception information from '{dumpFile}'");

// Get the path of the "ext.dll" (note: this currently needs to be manually copied
// to the appropriate directories)
string extensions = Path.Join(
    Path.GetDirectoryName(typeof(DebugEngine).Assembly.Location),
    Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE"),
    "ext.dll"
);
Console.WriteLine($"Using: Extensions at '{extensions}'");

AsyncContext.Run(async () =>
{
    DebugEngine engine = new DebugEngine();
    engine.DmlOutput += Engine_DmlOutput;
    await engine.SendRequestAsync(new OpenDumpFileRequest(dumpFile, new EngineOptions { SymPath = "srv*", SymOptAutoPublics = true, SymOptNoUnqualifiedLoads = false, SymOptDebug = false }));
    await engine.SendRequestAsync(new ExecuteRequest(".symopt- 100"));
    await engine.SendRequestAsync(new ExecuteRequest($".load {extensions.Replace(@"\", @"\\")}"));
    await engine.SendRequestAsync(new ExecuteRequest(".symfix;.reload"));
    await engine.SendRequestAsync(new ExecuteRequest("!analyze -v"));

    string? command;

    while (!string.IsNullOrWhiteSpace(command = Console.ReadLine()))
    {
        await engine.SendRequestAsync(new ExecuteRequest(command));
    }
});


void Engine_DmlOutput(object? sender, OutputEventArgs e)
{
    Console.Write(e.Output);
}

