// Program.cs — SOLUTION (same as practice)

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
else
{
    var client = await TemporalClient.ConnectAsync(new("localhost:7233"));

    var order = new VehicleOrder(
        VehicleId:     $"VIN-2026-COXAUTO-{Guid.NewGuid():N}"[..22],
        BuyerId:       "buyer-riley-w",
        PurchasePrice: 42_000.00m
    );

    Console.WriteLine($"[Starter] Submitting order for vehicle {order.VehicleId}...");
    Console.WriteLine($"[Starter] Note: title transfer fails ~60% of the time.");
    Console.WriteLine($"[Starter] When it fails after payment, watch for compensation log lines.");

    try
    {
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
    catch (Exception ex)
    {
        Console.WriteLine($"[Starter] Transaction failed (compensations ran): {ex.Message}");
    }
}
