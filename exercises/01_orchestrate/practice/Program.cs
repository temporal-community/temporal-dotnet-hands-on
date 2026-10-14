// Program.cs
//
// Terminal 1 — start the worker:
//   dotnet run -- worker
//
// Terminal 2 — submit an application:
//   dotnet run -- starter

using Microsoft.Extensions.Logging;
using Temporalio.Client;
using Temporalio.Worker;
using LoanUnderwriting;

const string TaskQueue = "loan-underwriting";

var mode   = args.FirstOrDefault() ?? "starter";
// Without a LoggerFactory, Workflow.Logger and the activity logger write to
// nowhere. Wiring the console logger here is what makes the retry attempts
// visible in this terminal.
var client = await TemporalClient.ConnectAsync(new("localhost:7233")
{
    LoggerFactory = LoggerFactory.Create(builder => builder
        .AddSimpleConsole(options => options.TimestampFormat = "HH:mm:ss ")
        .SetMinimumLevel(LogLevel.Information)),
});

if (mode == "worker")
{
    using var worker = new TemporalWorker(
        client,
        new TemporalWorkerOptions(TaskQueue)
            .AddWorkflow<LoanUnderwritingWorkflow>()
            .AddAllActivities(new LoanUnderwritingActivities()));

    Console.WriteLine($"[Worker] Polling '{TaskQueue}'. Ctrl+C to stop.\n");

    using var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };
    await worker.ExecuteAsync(cts.Token);
}
else
{
    // Guid.NewGuid is fine here — this is ordinary client code, not workflow
    // code. Inside a workflow you would use Workflow.NewGuid instead.
    var app = new LoanApplication(
        ApplicationId:   $"APP-{Guid.NewGuid():N}"[..12].ToUpperInvariant(),
        ApplicantId:     "applicant-4471",
        RequestedAmount: 38_500.00m,
        TermMonths:      60
    );

    Console.WriteLine($"[Starter] Submitting {app.ApplicationId} for ${app.RequestedAmount:N2}...");
    Console.WriteLine("[Starter] The credit bureau will fail twice before succeeding.\n");

    var result = await client.ExecuteWorkflowAsync(
        (LoanUnderwritingWorkflow wf) => wf.RunAsync(app),
        new WorkflowOptions(id: $"loan-{app.ApplicationId}", taskQueue: TaskQueue));

    Console.WriteLine($"[Starter] Done: {result.Status}");
    Console.WriteLine($"  Credit score: {result.CreditScore}");
    Console.WriteLine($"  Risk tier:    {result.RiskTier}");
    Console.WriteLine($"  Rate:         {result.ApprovedRate:P2}");
}
