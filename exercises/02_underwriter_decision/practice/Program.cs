// Program.cs
//
// Terminal 1 — start the worker:
//   dotnet run -- worker
//
// Terminal 2 — submit an application (it will pause for a decision):
//   dotnet run -- starter
//
// Terminal 3 — read the application's live status:
//   dotnet run -- status loan-APP-XXXXXXXX
//
// Terminal 3 — record the underwriter's ruling:
//   dotnet run -- decide loan-APP-XXXXXXXX approve
//   dotnet run -- decide loan-APP-XXXXXXXX decline

using Microsoft.Extensions.Logging;
using Temporalio.Client;
using Temporalio.Exceptions;
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

switch (mode)
{
    case "worker":
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
        break;
    }

    case "status":
    {
        var id = args.ElementAtOrDefault(1)
            ?? throw new ArgumentException("Usage: dotnet run -- status <workflow-id>");

        var handle = client.GetWorkflowHandle<LoanUnderwritingWorkflow>(id);
        var status = await handle.QueryAsync(wf => wf.GetStatus());

        Console.WriteLine($"[Status] {id}");
        Console.WriteLine($"  Stage:        {status.Stage}");
        Console.WriteLine($"  Credit score: {status.CreditScore}");
        Console.WriteLine($"  Risk tier:    {status.RiskTier}");
        Console.WriteLine($"  Decision:     {status.Decision ?? "(none yet)"}");
        Console.WriteLine($"  Escalated:    {status.Escalated}");
        break;
    }

    case "decide":
    {
        var id = args.ElementAtOrDefault(1)
            ?? throw new ArgumentException("Usage: dotnet run -- decide <workflow-id> <approve|decline>");
        var verdict = args.ElementAtOrDefault(2) ?? "approve";

        var handle   = client.GetWorkflowHandle<LoanUnderwritingWorkflow>(id);
        var decision = new UnderwriterDecision(verdict, "underwriter-dana-k", "Reviewed in lab");

        try
        {
            // ExecuteUpdateAsync blocks until the handler finishes and hands
            // back its return value — that round trip is what separates an
            // update from a signal.
            var ack = await handle.ExecuteUpdateAsync(wf => wf.SubmitDecisionAsync(decision));
            Console.WriteLine($"[Decide] Accepted: {ack}");
        }
        catch (WorkflowUpdateFailedException ex)
        {
            // The validator rejected it. Nothing was written to history.
            Console.WriteLine($"[Decide] Rejected: {ex.InnerException?.Message ?? ex.Message}");
        }
        catch (RpcException ex)
        {
            // Most likely the application was already decided and the workflow
            // has closed — there is nothing left to update.
            Console.WriteLine($"[Decide] Could not deliver the decision: {ex.Message}");
        }
        break;
    }

    default:
    {
        var app = new LoanApplication(
            ApplicationId:   $"APP-{Guid.NewGuid():N}"[..12].ToUpperInvariant(),
            ApplicantId:     "applicant-4471",
            RequestedAmount: 38_500.00m,
            TermMonths:      60
        );

        var workflowId = $"loan-{app.ApplicationId}";

        var handle = await client.StartWorkflowAsync(
            (LoanUnderwritingWorkflow wf) => wf.RunAsync(app),
            new WorkflowOptions(id: workflowId, taskQueue: TaskQueue));

        Console.WriteLine($"[Starter] Started {workflowId} for ${app.RequestedAmount:N2}.");
        Console.WriteLine($"[Starter] It will pause for an underwriter decision. In another terminal:\n");
        Console.WriteLine($"    dotnet run -- status {workflowId}");
        Console.WriteLine($"    dotnet run -- decide {workflowId} approve\n");
        Console.WriteLine("[Starter] Waiting for the result...");

        var result = await handle.GetResultAsync();

        Console.WriteLine($"\n[Starter] {result.ApplicationId}: {result.Status}");
        Console.WriteLine($"  Credit score: {result.CreditScore}");
        Console.WriteLine($"  Risk tier:    {result.RiskTier}");
        Console.WriteLine($"  Rate:         {result.ApprovedRate:P2}");
        break;
    }
}
