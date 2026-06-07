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
        await Workflow.ExecuteActivityAsync(
            (VehicleTransactionActivities a) => a.CheckFraudAsync(order),
            DefaultOptions);

        var paymentConfirmation = await Workflow.ExecuteActivityAsync(
            (VehicleTransactionActivities a) => a.ProcessPaymentAsync(order),
            DefaultOptions);

        await Workflow.ExecuteActivityAsync(
            (VehicleTransactionActivities a) => a.TransferTitleAsync(order),
            DefaultOptions);

        var docRequest = new DocumentRequest(
            VehicleId:           order.VehicleId,
            BuyerId:             order.BuyerId,
            PaymentConfirmation: paymentConfirmation
        );

        var docs = await Workflow.ExecuteChildWorkflowAsync(
            (DocumentGenerationWorkflow wf) => wf.RunAsync(docRequest),
            new ChildWorkflowOptions { Id = $"docs-{order.VehicleId}" });

        return new TransactionResult(
            VehicleId:           order.VehicleId,
            PaymentConfirmation: paymentConfirmation,
            BillOfSaleUrl:       docs.BillOfSaleUrl,
            TitleDocumentUrl:    docs.TitleDocumentUrl,
            Status:              "Completed"
        );
    }
}
