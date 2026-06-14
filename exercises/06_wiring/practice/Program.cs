// Program.cs
//
// Run as a worker (Terminal 1):
//   dotnet run -- worker
//
// Run as a starter — standard order (Terminal 2):
//   dotnet run -- starter
//
// Run as a starter — high-value order requiring approval (Terminal 2):
//   dotnet run -- starter high
//
// Approve a paused workflow (Terminal 2):
//   dotnet run -- approve <workflow-id>
//
// Query a running workflow (Terminal 2):
//   dotnet run -- query <workflow-id>

using Temporalio.Client;
using Temporalio.Worker;
using VehicleTransaction;

var mode = args.FirstOrDefault() ?? "starter";

if (mode == "worker")
{
    var client = await TemporalClient.ConnectAsync(new("localhost:7233"));

    using var worker = new TemporalWorker(
        client,
        new TemporalWorkerOptions("vehicle-transaction")
            .AddWorkflow<VehicleTransactionWorkflow>()
            // TODO (Stage A): Register your activities class here.
            // Without this line the worker will never schedule activities,
            // even after you add them to VehicleTransactionActivities.cs.
            //
            // Add: .AddAllActivities(new VehicleTransactionActivities())
            );

    Console.WriteLine("[Worker] Connected. Polling vehicle-transaction task queue...");
    Console.WriteLine("[Worker] Press Ctrl+C to stop.\n");

    using var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };
    await worker.ExecuteAsync(cts.Token);
}
else if (mode == "starter")
{
    var client = await TemporalClient.ConnectAsync(new("localhost:7233"));

    var highValue = args.ElementAtOrDefault(1) == "high";

    var order = new VehicleOrder(
        VehicleId:     "VIN-2026-COXAUTO-006",
        BuyerId:       "buyer-alex-t",
        PurchasePrice: highValue ? 75_000.00m : 28_500.00m
    );

    var workflowId = $"vehicle-tx-{order.VehicleId}";

    Console.WriteLine($"[Starter] Submitting order for vehicle {order.VehicleId}...");
    if (highValue)
    {
        Console.WriteLine($"[Starter] High-value order (${order.PurchasePrice:N0}) — manager approval required.");
        Console.WriteLine($"[Starter] Workflow ID: {workflowId}");
        Console.WriteLine($"[Starter] Approve with: dotnet run -- approve {workflowId}");
        Console.WriteLine($"[Starter] Query with:   dotnet run -- query {workflowId}");
    }

    var result = await client.ExecuteWorkflowAsync(
        (VehicleTransactionWorkflow wf) => wf.RunAsync(order),
        new Temporalio.Client.WorkflowOptions(
            id:        workflowId,
            taskQueue: "vehicle-transaction"
        )
    );

    Console.WriteLine($"[Starter] Transaction complete!");
    Console.WriteLine($"  Vehicle:  {result.VehicleId}");
    Console.WriteLine($"  Payment:  {result.PaymentConfirmation}");
    Console.WriteLine($"  Status:   {result.Status}");
}
else if (mode == "approve" && args.Length > 1)
{
    var client = await TemporalClient.ConnectAsync(new("localhost:7233"));
    var workflowId = args[1];
    var handle = client.GetWorkflowHandle(workflowId);
    await handle.SignalAsync("ApproveTransaction", Array.Empty<object?>());
    Console.WriteLine($"[Signal] Sent ApproveTransaction to {workflowId}");
}
else if (mode == "query" && args.Length > 1)
{
    var client = await TemporalClient.ConnectAsync(new("localhost:7233"));
    var workflowId = args[1];
    var handle = client.GetWorkflowHandle(workflowId);
    var status = await handle.QueryAsync<WorkflowStatus>("GetStatus", new List<object?>());
    Console.WriteLine($"[Query] Stage:              {status.Stage}");
    Console.WriteLine($"[Query] Approval required:  {status.ManagerApprovalRequired}");
    Console.WriteLine($"[Query] Approved:           {status.ManagerApproved}");
}
else
{
    Console.WriteLine("Usage:");
    Console.WriteLine("  dotnet run -- worker");
    Console.WriteLine("  dotnet run -- starter          (standard order, $28,500)");
    Console.WriteLine("  dotnet run -- starter high     (high-value order, $75,000)");
    Console.WriteLine("  dotnet run -- approve <id>");
    Console.WriteLine("  dotnet run -- query <id>");
}
