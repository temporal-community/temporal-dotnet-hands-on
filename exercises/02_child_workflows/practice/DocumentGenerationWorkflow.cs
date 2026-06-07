// DocumentGenerationWorkflow.cs
//
// This is the child workflow. It owns the document generation sub-process:
// bill of sale and title document. It has its own event history and its own
// retry boundary — failures here don't pollute the parent workflow's history.
//
// Your job: implement RunAsync so it generates both documents and returns
// a DocumentResult.

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

    // TODO: Implement RunAsync
    //
    // Goal: call GenerateBillOfSaleAsync and GenerateTitleDocumentAsync in
    //       sequence, then return a DocumentResult with both URLs.
    //
    // These activities take a DocumentRequest (already your input parameter).
    // Call them the same way you called activities in Exercise 1:
    //   var url = await Workflow.ExecuteActivityAsync(
    //       (VehicleTransactionActivities a) => a.SomeActivityAsync(request),
    //       DefaultOptions);
    [WorkflowRun]
    public async Task<DocumentResult> RunAsync(DocumentRequest request)
    {
        throw new NotImplementedException("TODO: implement RunAsync");
    }
}
