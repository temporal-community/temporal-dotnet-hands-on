// LoanUnderwritingWorkflow.cs — SOLUTION
//
// The workflow is the durable part. Everything it does is recorded in an
// event history, so if the worker process dies the next worker replays that
// history and picks up exactly where this one left off.
//
// That durability is why workflow code must be deterministic: replay has to
// produce the same decisions every time. No DateTime.Now, no Random, no
// Guid.NewGuid, no Task.Delay. Temporal gives you safe equivalents —
// Workflow.UtcNow, Workflow.Random, Workflow.NewGuid, Workflow.DelayAsync.

using Microsoft.Extensions.Logging;
using Temporalio.Common;
using Temporalio.Workflows;

namespace LoanUnderwriting;

[Workflow]
public class LoanUnderwritingWorkflow
{
    // The credit bureau fails its first two attempts, so this policy is what
    // makes the workflow survive. Attempts land at roughly t=0s, t=5s, t=20s:
    // the 15-second gap before the third attempt is your window to kill the
    // worker and watch Temporal carry on without it.
    private static readonly ActivityOptions CreditOptions = new()
    {
        StartToCloseTimeout = TimeSpan.FromSeconds(30),
        RetryPolicy = new RetryPolicy
        {
            InitialInterval    = TimeSpan.FromSeconds(5),
            BackoffCoefficient = 3.0f,
            MaximumInterval    = TimeSpan.FromSeconds(30),
            MaximumAttempts    = 5,
        },
    };

    // The fraud and risk services are reliable, so a shorter, plainer policy
    // is enough. Different failure profiles deserve different policies.
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

    [WorkflowRun]
    public async Task<UnderwritingResult> RunAsync(LoanApplication app)
    {
        // Workflow.UtcNow is replay-safe: on every replay it returns the same
        // instant it returned the first time. DateTime.Now would return a new
        // value each replay and corrupt the execution.
        var startedAt = Workflow.UtcNow;

        // Workflow.Logger suppresses duplicate lines during replay, so the
        // worker console shows each step once rather than once per replay.
        Workflow.Logger.LogInformation(
            "Underwriting {ApplicationId} for ${Amount:N2} started at {StartedAt:u} (workflow {WorkflowId}, run {RunId})",
            app.ApplicationId,
            app.RequestedAmount,
            startedAt,
            Workflow.Info.WorkflowId,
            Workflow.Info.RunId);

        var credit = await Workflow.ExecuteActivityAsync(
            (LoanUnderwritingActivities a) => a.PullCreditAsync(app),
            CreditOptions);

        await Workflow.ExecuteActivityAsync(
            (LoanUnderwritingActivities a) => a.CheckFraudAsync(app),
            DefaultOptions);

        var risk = await Workflow.ExecuteActivityAsync(
            (LoanUnderwritingActivities a) => a.ScoreRiskAsync(app, credit),
            DefaultOptions);

        var elapsed = Workflow.UtcNow - startedAt;

        Workflow.Logger.LogInformation(
            "Underwriting {ApplicationId} complete in {Elapsed:g}: {Tier} tier at {Rate:P2}",
            app.ApplicationId, elapsed, risk.Tier, risk.ApprovedRate);

        return new UnderwritingResult(
            ApplicationId: app.ApplicationId,
            Status:        "Underwritten",
            CreditScore:   credit.Score,
            RiskTier:      risk.Tier,
            ApprovedRate:  risk.ApprovedRate
        );
    }
}
