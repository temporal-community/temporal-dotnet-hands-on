// LoanUnderwritingWorkflow.cs
//
// The workflow is the durable part. Everything it does is recorded in an
// event history, so if the worker process dies the next worker replays that
// history and picks up exactly where this one left off.
//
// That durability is why workflow code must be deterministic: replay has to
// produce the same decisions every time. Inside a workflow, never use
// DateTime.Now, Random, Guid.NewGuid, or Task.Delay. Temporal gives you
// replay-safe equivalents: Workflow.UtcNow, Workflow.Random, Workflow.NewGuid,
// Workflow.DelayAsync.

using Microsoft.Extensions.Logging;
using Temporalio.Common;
using Temporalio.Workflows;

namespace LoanUnderwriting;

[Workflow]
public class LoanUnderwritingWorkflow
{
    // TODO 1 — Define the ActivityOptions for the credit pull.
    //
    // PullCreditAsync fails its first two attempts, so this retry policy is
    // the only reason the workflow survives. Aim for attempts at roughly
    // t=0s, t=5s, and t=20s — that 15-second gap before the third attempt is
    // your window to kill the worker in Part C.
    //
    //   private static readonly ActivityOptions CreditOptions = new()
    //   {
    //       StartToCloseTimeout = TimeSpan.FromSeconds(30),
    //       RetryPolicy = new RetryPolicy
    //       {
    //           InitialInterval    = TimeSpan.FromSeconds(5),
    //           BackoffCoefficient = 3.0f,
    //           MaximumInterval    = TimeSpan.FromSeconds(30),
    //           MaximumAttempts    = 5,
    //       },
    //   };
    //
    // Then add a second, plainer "DefaultOptions" for the fraud and risk
    // activities — those services are reliable, so a 1s initial interval,
    // coefficient 2.0 and 3 maximum attempts is plenty. Different failure
    // profiles deserve different policies.

    [WorkflowRun]
    public async Task<UnderwritingResult> RunAsync(LoanApplication app)
    {
        // TODO 2 — Implement the workflow body.
        //
        //   a) Capture the start time with Workflow.UtcNow (NOT DateTime.Now —
        //      DateTime.Now returns a different value on every replay and
        //      corrupts the execution):
        //        var startedAt = Workflow.UtcNow;
        //
        //   b) Log that the application started. Workflow.Logger suppresses
        //      duplicate lines during replay, so each line prints once:
        //        Workflow.Logger.LogInformation(
        //            "Underwriting {ApplicationId} started at {StartedAt:u} (workflow {WorkflowId})",
        //            app.ApplicationId, startedAt, Workflow.Info.WorkflowId);
        //
        //      Workflow.Info also carries RunId, Attempt, TaskQueue and more.
        //
        //   c) Call the three activities in order. The lambda is never run
        //      locally — it tells Temporal which activity to schedule:
        //        var credit = await Workflow.ExecuteActivityAsync(
        //            (LoanUnderwritingActivities a) => a.PullCreditAsync(app),
        //            CreditOptions);
        //
        //      Then the other two. CheckFraudAsync returns a value you do not
        //      need here, so you can simply await it without assigning:
        //        await Workflow.ExecuteActivityAsync(
        //            (LoanUnderwritingActivities a) => a.CheckFraudAsync(app),
        //            DefaultOptions);
        //
        //        var risk = await Workflow.ExecuteActivityAsync(
        //            (LoanUnderwritingActivities a) => a.ScoreRiskAsync(app, credit),
        //            DefaultOptions);
        //
        //   d) Return the result. The field names are in Models.cs:
        //        return new UnderwritingResult(
        //            ApplicationId: app.ApplicationId,
        //            Status:        "Underwritten",
        //            CreditScore:   credit.Score,
        //            RiskTier:      risk.Tier,
        //            ApprovedRate:  risk.ApprovedRate
        //        );
        throw new NotImplementedException("TODO 2: implement RunAsync");
    }
}
