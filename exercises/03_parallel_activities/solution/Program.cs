// Program.cs — SOLUTION

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

    using var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };
    await worker.ExecuteAsync(cts.Token);
}
else
{
    var client = await TemporalClient.ConnectAsync(new("localhost:7233"));

    var order = new VehicleOrder(
        VehicleId:     "VIN-2026-COXAUTO-003",
        BuyerId:       "buyer-jordan-t",
        PurchasePrice: 18_750.00m
    );

    Console.WriteLine($"[Starter] Submitting order for vehicle {order.VehicleId}...");

    var result = await client.ExecuteWorkflowAsync(
        (VehicleTransactionWorkflow wf) => wf.RunAsync(order),
        new Temporalio.Client.WorkflowOptions(
            id:        $"vehicle-tx-{order.VehicleId}",
            taskQueue: "vehicle-transaction"
        )
    );

    Console.WriteLine($"[Starter] Transaction complete!");
    Console.WriteLine($"  Vehicle:  {result.VehicleId}");
    Console.WriteLine($"  Payment:  {result.PaymentConfirmation}");
    Console.WriteLine($"  Status:   {result.Status}");
}
