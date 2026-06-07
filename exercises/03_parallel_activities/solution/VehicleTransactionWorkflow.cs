// VehicleTransactionWorkflow.cs — SOLUTION

using Temporalio.Workflows;
using Temporalio.Common;

namespace VehicleTransaction;

[Workflow]
public class VehicleTransactionWorkflow
{
    private static readonly ActivityOptions DefaultOptions = new()
    {
        StartToCloseTimeout = TimeSpan.FromSeconds(30),
        RetryPolicy = new RetryPolicy
        {
            InitialInterval    = TimeSpan.FromSeconds(1),
            MaximumInterval    = TimeSpan.FromSeconds(30),
            BackoffCoefficient = 2.0f,
        }
    };

    [WorkflowRun]
    public async Task<TransactionResult> RunAsync(VehicleOrder order)
    {
        // Start both checks without awaiting — they run concurrently.
        var fraudTask = Workflow.ExecuteActivityAsync(
            (VehicleTransactionActivities a) => a.CheckFraudAsync(order),
            DefaultOptions);

        var inventoryTask = Workflow.ExecuteActivityAsync(
            (VehicleTransactionActivities a) => a.ConfirmInventoryAsync(order),
            DefaultOptions);

        // Wait for both to complete. If either throws, the workflow fails here.
        await Task.WhenAll(fraudTask, inventoryTask);

        var paymentConfirmation = await Workflow.ExecuteActivityAsync(
            (VehicleTransactionActivities a) => a.ProcessPaymentAsync(order),
            DefaultOptions);

        await Workflow.ExecuteActivityAsync(
            (VehicleTransactionActivities a) => a.TransferTitleAsync(order),
            DefaultOptions);

        return new TransactionResult(
            VehicleId:           order.VehicleId,
            PaymentConfirmation: paymentConfirmation,
            Status:              "Completed"
        );
    }
}
