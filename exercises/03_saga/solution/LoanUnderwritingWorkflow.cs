// LoanUnderwritingWorkflow.cs — SOLUTION
//
// Temporal guarantees your workflow runs to completion. It cannot un-ring a
// bell in someone else's system: if you placed a credit hold and disbursed
// funds, those happened. Undoing them is your job, and the saga pattern is
// how you structure it.
//
// The rule that makes this work: register each compensation BEFORE running
// the action it undoes. Register it afterwards and there is a window — the
// action succeeded, the push had not happened yet, the worker died — where
// the undo is lost forever. Registering first closes that window, at the cost
// of sometimes undoing something that never happened. That is a trade you
// want, and it is why every compensation must be idempotent.

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

    // Small helpers so the saga below reads as the pattern rather than as
    // Temporal boilerplate. Two overloads: one for activities that return a
    // value, one for activities that return plain Task.
    private static Task<TResult> Run<TResult>(
        Expression<Func<LoanUnderwritingActivities, Task<TResult>>> call) =>
        Workflow.ExecuteActivityAsync(call, DefaultOptions);

    private static Task Run(
        Expression<Func<LoanUnderwritingActivities, Task>> call) =>
        Workflow.ExecuteActivityAsync(call, DefaultOptions);

    [WorkflowRun]
    public async Task<FundingResult> RunAsync(LoanApplication app)
    {
        Workflow.Logger.LogInformation(
            "Funding {ApplicationId} for ${Amount:N2}",
            app.ApplicationId, app.RequestedAmount);

        var compensations = new Stack<Func<Task>>();

        try
        {
            compensations.Push(() => Run(a => a.ReleaseCreditHoldAsync(app)));
            await Run(a => a.PlaceCreditHoldAsync(app));

            compensations.Push(() => Run(a => a.CancelDocumentRequestAsync(app)));
            await Run(a => a.RequestDocumentsAsync(app));

            compensations.Push(() => Run(a => a.ReverseFundingAsync(app)));
            var fundingReference = await Run(a => a.FundLoanAsync(app));

            Workflow.Logger.LogInformation(
                "{ApplicationId} funded as {Reference}",
                app.ApplicationId, fundingReference);

            return new FundingResult(app.ApplicationId, "Funded", fundingReference);
        }
        catch
        {
            // Unwind. A Stack pops in reverse order, so the most recent step
            // is undone first — release the funds before cancelling the
            // documents before releasing the hold.
            Workflow.Logger.LogWarning(
                "{ApplicationId} failed — running {Count} compensation(s) in reverse",
                app.ApplicationId, compensations.Count);

            while (compensations.TryPop(out var undo))
            {
                await undo();
            }

            // A compensated failure is still a failure. Re-throw so the
            // workflow ends Failed and the caller finds out.
            throw;
        }
    }
}
