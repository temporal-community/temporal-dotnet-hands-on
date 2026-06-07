// Program.cs
//
// Three modes:
//   dotnet run -- worker          start the worker
//   dotnet run -- starter         submit a high-value order (triggers approval gate)
//   dotnet run -- approve <id>    send the ApproveTransaction signal
//   dotnet run -- query <id>      query workflow status

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
            .AddAllActivities(new VehicleTransactionActivities()));

    Console.WriteLine("[Worker] Connected. Polling vehicle-transaction task queue...");
    Console.WriteLine("[Worker] Press Ctrl+C to stop.\n");

    await worker.ExecuteAsync(ct => Task.Delay(Timeout.Infinite, ct));
}
else if (mode == "starter")
{
    var client = await TemporalClient.ConnectAsync(new("localhost:7233"));

    // High-value order — will trigger the manager approval gate
    var order = new VehicleOrder(
        VehicleId:     "VIN-2026-COXAUTO-004",
        BuyerId:       "buyer-sam-k",
        PurchasePrice: 75_000.00m
    );

    var workflowId = $"vehicle-tx-{order.VehicleId}";

    Console.WriteLine($"[Starter] Submitting high-value order for vehicle {order.VehicleId}...");
    Console.WriteLine($"[Starter] Workflow ID: {workflowId}");
    Console.WriteLine($"[Starter] This order requires manager approval (price > $50,000).");
    Console.WriteLine($"[Starter] Run: dotnet run -- approve {workflowId}");
    Console.WriteLine($"[Starter] Run: dotnet run -- query {workflowId}");

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
    await handle.SignalAsync<VehicleTransactionWorkflow>(wf => wf.ApproveTransactionAsync());
    Console.WriteLine($"[Signal] Sent ApproveTransaction to {workflowId}");
}
else if (mode == "query" && args.Length > 1)
{
    var client = await TemporalClient.ConnectAsync(new("localhost:7233"));
    var workflowId = args[1];
    var handle = client.GetWorkflowHandle(workflowId);
    var status = await handle.QueryAsync<WorkflowStatus>(
        wf => ((VehicleTransactionWorkflow)(object)wf).GetStatus());
    Console.WriteLine($"[Query] Stage:                  {status.Stage}");
    Console.WriteLine($"[Query] Approval required:      {status.ManagerApprovalRequired}");
    Console.WriteLine($"[Query] Approved:               {status.ManagerApproved}");
}
else
{
    Console.WriteLine("Usage:");
    Console.WriteLine("  dotnet run -- worker");
    Console.WriteLine("  dotnet run -- starter");
    Console.WriteLine("  dotnet run -- approve <workflow-id>");
    Console.WriteLine("  dotnet run -- query <workflow-id>");
}
