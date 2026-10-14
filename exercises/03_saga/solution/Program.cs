// Program.cs — SOLUTION
//
// Terminal 1 — start the worker:
//   dotnet run -- worker
//
// Terminal 2 — fund a loan that succeeds:
//   dotnet run -- starter
//
// Terminal 2 — fund a loan the partner declines, unwinding the saga:
//   dotnet run -- starter large

using Microsoft.Extensions.Logging;
using Temporalio.Client;
using Temporalio.Worker;
using LoanUnderwriting;

const string TaskQueue = "loan-underwriting";

var mode = args.FirstOrDefault() ?? "starter";

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
    // "large" asks for more than the funding partner's per-loan limit, so the
    // third step fails after the first two have already committed.
    var large = args.ElementAtOrDefault(1) == "large";

    var app = new LoanApplication(
        ApplicationId:   $"APP-{Guid.NewGuid():N}"[..12].ToUpperInvariant(),
        ApplicantId:     "applicant-4471",
        RequestedAmount: large ? 72_000.00m : 38_500.00m,
        TermMonths:      60
    );

    Console.WriteLine($"[Starter] Funding {app.ApplicationId} for ${app.RequestedAmount:N2}...");
    if (large)
    {
        Console.WriteLine($"[Starter] That is over the ${FundingService.PartnerLimit:N2} partner limit — expect the saga to unwind.\n");
    }

    try
    {
        var result = await client.ExecuteWorkflowAsync(
            (LoanUnderwritingWorkflow wf) => wf.RunAsync(app),
            new WorkflowOptions(id: $"loan-{app.ApplicationId}", taskQueue: TaskQueue));

        Console.WriteLine($"\n[Starter] {result.ApplicationId}: {result.Status} ({result.FundingReference})");
    }
    catch (Exception ex)
    {
        // The workflow failed, but every committed step was undone first.
        Console.WriteLine($"\n[Starter] {app.ApplicationId} was not funded. Committed steps were compensated.");

        // Temporal wraps the real cause: WorkflowFailureException ->
        // ActivityFailureException -> ApplicationFailureException. Walk to the
        // innermost one to show the message the activity actually threw.
        var cause = ex;
        while (cause.InnerException is not null) cause = cause.InnerException;
        Console.WriteLine($"[Starter] Reason: {cause.Message}");
    }
}
