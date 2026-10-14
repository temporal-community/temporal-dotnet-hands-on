// LoanUnderwritingWorkflow.cs
//
// Temporal guarantees your workflow runs to completion. It cannot un-ring a
// bell in someone else's system: if you placed a credit hold and disbursed
// funds, those happened. Undoing them is your job, and the saga pattern is
// how you structure it.
//
// The activities and their compensations are already written for you in
// LoanUnderwritingActivities.cs. Your job is the orchestration.

using System.Linq.Expressions;
using Microsoft.Extensions.Logging;
using Temporalio.Common;
using Temporalio.Workflows;

namespace LoanUnderwriting;

[Workflow]
public class LoanUnderwritingWorkflow
{
    private static readonly ActivityOptions DefaultOptions = new()
    {
        StartToCloseTimeout = TimeSpan.FromSeconds(30),
        RetryPolicy = new RetryPolicy
        {
            InitialInterval    = TimeSpan.FromSeconds(1),
            BackoffCoefficient = 2.0f,
            MaximumInterval    = TimeSpan.FromSeconds(10),
            MaximumAttempts    = 3,
        },
    };

    // Small helpers so your saga reads as the pattern rather than as Temporal
    // boilerplate. Two overloads: one for activities that return a value, one
    // for activities that return plain Task.
    private static Task<TResult> Run<TResult>(
        Expression<Func<LoanUnderwritingActivities, Task<TResult>>> call) =>
        Workflow.ExecuteActivityAsync(call, DefaultOptions);

    private static Task Run(
        Expression<Func<LoanUnderwritingActivities, Task>> call) =>
        Workflow.ExecuteActivityAsync(call, DefaultOptions);

    [WorkflowRun]
    public async Task<FundingResult> RunAsync(LoanApplication app)
    {
        // TODO — Implement the funding saga.
        //
        // Three forward steps, each with a compensation that undoes it:
        //
        //   PlaceCreditHoldAsync      <->  ReleaseCreditHoldAsync
        //   RequestDocumentsAsync     <->  CancelDocumentRequestAsync
        //   FundLoanAsync             <->  ReverseFundingAsync
        //
        // The shape:
        //
        //   var compensations = new Stack<Func<Task>>();
        //   try
        //   {
        //       compensations.Push(() => Run(a => a.ReleaseCreditHoldAsync(app)));
        //       await Run(a => a.PlaceCreditHoldAsync(app));
        //
        //       // ...documents, then funding, each the same way...
        //
        //       var fundingReference = await Run(a => a.FundLoanAsync(app));
        //       return new FundingResult(app.ApplicationId, "Funded", fundingReference);
        //   }
        //   catch
        //   {
        //       while (compensations.TryPop(out var undo)) await undo();
        //       throw;
        //   }
        //
        // THE RULE THAT MATTERS: push each compensation BEFORE you run the
        // action it undoes.
        //
        // Push it afterwards and there is a window — the action succeeded, the
        // push had not happened yet, the worker died — where the undo is lost
        // forever and the money stays moved. Pushing first closes that window,
        // at the cost of sometimes undoing something that never happened.
        // That is why every compensation in this exercise is idempotent: an
        // undo for an action that never landed is a harmless no-op. You will
        // see one of those no-ops in the output.
        //
        // A Stack pops in reverse order, which is what you want: release the
        // funds before cancelling the documents before releasing the hold.
        //
        // Re-throw at the end of the catch. A compensated failure is still a
        // failure, and the caller needs to find out.
        throw new NotImplementedException("TODO: implement the funding saga");
    }
}
