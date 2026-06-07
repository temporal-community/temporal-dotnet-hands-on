// VehicleTransactionWorkflow.cs
//
// High-value transactions (over $50,000) require manager approval before
// payment can proceed. The workflow pauses and waits for a signal.
//
// Your job: implement the signal handler, the query handler, and the
// approval gate in RunAsync.

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

    // Workflow state — updated by signals, read by queries.
    private string _stage = "starting";
    private bool _managerApprovalRequired = false;
    private bool _managerApproved = false;

    // TODO: Implement the ApproveTransaction signal handler
    //
    // Goal: mark this workflow as approved when a signal arrives.
    //       Update _managerApproved to true.
    //
    // A signal is a method decorated with [WorkflowSignal]. It can update
    // workflow state but cannot return a value to the caller.
    //
    // Pattern:
    //   [WorkflowSignal]
    //   public async Task SignalNameAsync()
    //   {
    //       _someField = newValue;
    //   }

    // TODO: Implement the GetStatus query handler
    //
    // Goal: return a WorkflowStatus snapshot of the current workflow state.
    //
    // A query is a method decorated with [WorkflowQuery]. It reads state
    // synchronously and must not modify it or await anything.
    //
    // Pattern:
    //   [WorkflowQuery]
    //   public ReturnType QueryName()
    //   {
    //       return new ReturnType(_someField, _otherField);
    //   }

    [WorkflowRun]
    public async Task<TransactionResult> RunAsync(VehicleOrder order)
    {
        _stage = "fraud-check";

        await Workflow.ExecuteActivityAsync(
            (VehicleTransactionActivities a) => a.CheckFraudAsync(order),
            DefaultOptions);

        // High-value orders need manager approval before payment.
        if (order.PurchasePrice > 50_000m)
        {
            _managerApprovalRequired = true;
            _stage = "awaiting-approval";

            // TODO: Wait here until _managerApproved is true.
            //
            // Goal: pause the workflow until the ApproveTransaction signal
            //       sets _managerApproved = true.
            //
            // Workflow.WaitConditionAsync blocks the workflow until a
            // predicate becomes true. It is re-evaluated each time workflow
            // state changes (i.e. after each signal).
            //
            // Pattern:
            //   await Workflow.WaitConditionAsync(() => _someBoolField);

            throw new NotImplementedException("TODO: wait for manager approval");
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
