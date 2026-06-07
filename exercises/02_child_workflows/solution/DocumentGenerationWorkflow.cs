// DocumentGenerationWorkflow.cs — SOLUTION

using Temporalio.Workflows;
using Temporalio.Common;

namespace VehicleTransaction;

[Workflow]
public class DocumentGenerationWorkflow
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
    public async Task<DocumentResult> RunAsync(DocumentRequest request)
    {
        var billOfSaleUrl = await Workflow.ExecuteActivityAsync(
            (VehicleTransactionActivities a) => a.GenerateBillOfSaleAsync(request),
            DefaultOptions);

        var titleDocumentUrl = await Workflow.ExecuteActivityAsync(
            (VehicleTransactionActivities a) => a.GenerateTitleDocumentAsync(request),
            DefaultOptions);

        return new DocumentResult(
            BillOfSaleUrl:    billOfSaleUrl,
            TitleDocumentUrl: titleDocumentUrl
        );
    }
}
