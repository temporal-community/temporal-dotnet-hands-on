// Program.cs — run as worker or starter
//
// Run as a worker (Terminal 1):
//   dotnet run -- worker
//
// Run as a starter (Terminal 2):
//   dotnet run -- starter

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
            .AddWorkflow<DocumentGenerationWorkflow>()
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
        VehicleId:     "VIN-2026-COXAUTO-002",
        BuyerId:       "buyer-alex-m",
        PurchasePrice: 31_200.00m
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
    Console.WriteLine($"  Vehicle:       {result.VehicleId}");
    Console.WriteLine($"  Payment:       {result.PaymentConfirmation}");
    Console.WriteLine($"  Bill of Sale:  {result.BillOfSaleUrl}");
    Console.WriteLine($"  Title Doc:     {result.TitleDocumentUrl}");
    Console.WriteLine($"  Status:        {result.Status}");
}
