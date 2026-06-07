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
            DefaultOptions);

        if (order.PurchasePrice > 50_000m)
        {
            _managerApprovalRequired = true;
            _stage = "awaiting-approval";

            await Workflow.WaitConditionAsync(() => _managerApproved);
        }

        _stage = "payment";

        var paymentConfirmation = await Workflow.ExecuteActivityAsync(
            (VehicleTransactionActivities a) => a.ProcessPaymentAsync(order),
            DefaultOptions);

        _stage = "title-transfer";

        await Workflow.ExecuteActivityAsync(
            (VehicleTransactionActivities a) => a.TransferTitleAsync(order),
            DefaultOptions);

        _stage = "completed";

        return new TransactionResult(
            VehicleId:           order.VehicleId,
            PaymentConfirmation: paymentConfirmation,
            Status:              "Completed"
        );
    }
}
