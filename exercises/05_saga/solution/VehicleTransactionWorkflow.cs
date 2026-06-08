// VehicleTransactionWorkflow.cs — SOLUTION

using Temporalio.Workflows;
using Temporalio.Common;
using Microsoft.Extensions.Logging;

namespace VehicleTransaction;

[Workflow]
public class VehicleTransactionWorkflow
{
    private static readonly ActivityOptions DefaultOptions = new()
    {
        StartToCloseTimeout = TimeSpan.FromSeconds(30),
        RetryPolicy = new RetryPolicy
        {
            // Cap attempts so a persistently failing step eventually gives up
            // and triggers compensation, instead of retrying forever.
            MaximumAttempts    = 3,
            InitialInterval    = TimeSpan.FromSeconds(1),
            MaximumInterval    = TimeSpan.FromSeconds(30),
            BackoffCoefficient = 2.0f,
        }
    };

    private static readonly ActivityOptions CompensationOptions = new()
    {
        StartToCloseTimeout = TimeSpan.FromSeconds(30),
        RetryPolicy = new RetryPolicy
        {
            MaximumAttempts    = 3,
            InitialInterval    = TimeSpan.FromSeconds(1),
            MaximumInterval    = TimeSpan.FromSeconds(10),
            BackoffCoefficient = 2.0f,
        }
    };

    [WorkflowRun]
    public async Task<TransactionResult> RunAsync(VehicleOrder order)
    {
        var compensations = new Stack<Func<Task>>();

        try
        {
            // Step 1: Fraud check — no compensation needed (read-only)
            await Workflow.ExecuteActivityAsync(
                (VehicleTransactionActivities a) => a.CheckFraudAsync(order),
                DefaultOptions);

            // Step 2: Payment — compensate with a refund if a later step fails
            var paymentConfirmation = await Workflow.ExecuteActivityAsync(
                (VehicleTransactionActivities a) => a.ProcessPaymentAsync(order),
                DefaultOptions);

            compensations.Push(() => Workflow.ExecuteActivityAsync(
                (VehicleTransactionActivities a) =>
                    a.RefundPaymentAsync(order, paymentConfirmation),
                CompensationOptions));

            // Step 3: Title transfer — compensate with a reversion
            await Workflow.ExecuteActivityAsync(
                (VehicleTransactionActivities a) => a.TransferTitleAsync(order),
                DefaultOptions);

            compensations.Push(() => Workflow.ExecuteActivityAsync(
                (VehicleTransactionActivities a) => a.RevertTitleTransferAsync(order),
                CompensationOptions));

            return new TransactionResult(
                VehicleId:           order.VehicleId,
                PaymentConfirmation: paymentConfirmation,
                Status:              "Completed"
            );
        }
        catch (Exception)
        {
            // Run compensations in reverse order (stack unwinds naturally).
            // Isolate each one so a failed compensation doesn't prevent the
            // remaining compensations from running.
            foreach (var compensate in compensations)
            {
                try
                {
                    await compensate();
                }
                catch (Exception compErr)
                {
                    Workflow.Logger.LogError(compErr, "Compensation failed");
                }
            }

            throw;
        }
    }
}
