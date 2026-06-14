// VehicleTransactionWorkflow.cs — SOLUTION

using Temporalio.Workflows;
using Temporalio.Common;

namespace VehicleTransaction;

[Workflow]
public class VehicleTransactionWorkflow
{
    // Fraud checks: fast call, short timeout, retry aggressively.
    private static readonly ActivityOptions FraudOptions = new()
    {
        StartToCloseTimeout = TimeSpan.FromSeconds(10),
        RetryPolicy = new RetryPolicy
        {
            InitialInterval    = TimeSpan.FromSeconds(1),
            MaximumInterval    = TimeSpan.FromSeconds(10),
            BackoffCoefficient = 2.0f,
        }
    };

    // Payment and title: external partners, slower and more sensitive —
    // longer timeout, fewer max attempts, more conservative backoff.
    private static readonly ActivityOptions PaymentOptions = new()
    {
        StartToCloseTimeout = TimeSpan.FromSeconds(60),
        RetryPolicy = new RetryPolicy
        {
            InitialInterval    = TimeSpan.FromSeconds(2),
            MaximumInterval    = TimeSpan.FromSeconds(60),
            BackoffCoefficient = 2.0f,
            MaximumAttempts    = 5,
        }
    };

    private string _stage = "starting";
    private bool _managerApprovalRequired = false;
    private bool _managerApproved = false;

    [WorkflowSignal]
    public async Task ApproveTransactionAsync()
    {
        _managerApproved = true;
    }

    [WorkflowQuery]
    public WorkflowStatus GetStatus() =>
        new(_stage, _managerApprovalRequired, _managerApproved);

    [WorkflowRun]
    public async Task<TransactionResult> RunAsync(VehicleOrder order)
    {
        _stage = "fraud-check";

        await Workflow.ExecuteActivityAsync(
            (VehicleTransactionActivities a) => a.CheckFraudAsync(order),
            FraudOptions);

        if (order.PurchasePrice > 50_000m)
        {
            _managerApprovalRequired = true;
            _stage = "awaiting-approval";
            await Workflow.WaitConditionAsync(() => _managerApproved);
        }

        _stage = "payment";

        var paymentConfirmation = await Workflow.ExecuteActivityAsync(
            (VehicleTransactionActivities a) => a.ProcessPaymentAsync(order),
            PaymentOptions);

        _stage = "title-transfer";

        await Workflow.ExecuteActivityAsync(
            (VehicleTransactionActivities a) => a.TransferTitleAsync(order),
            PaymentOptions);

        _stage = "completed";

        return new TransactionResult(
            VehicleId:           order.VehicleId,
            PaymentConfirmation: paymentConfirmation,
            Status:              "Completed"
        );
    }
}
