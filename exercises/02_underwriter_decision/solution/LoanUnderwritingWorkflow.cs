// LoanUnderwritingWorkflow.cs — SOLUTION
//
// Challenge 1 ran straight through. Real underwriting doesn't: after the
// automated checks, a human has to rule on the application. That human might
// answer in thirty seconds or next Tuesday.
//
// A workflow waiting on Workflow.WaitConditionAsync consumes no CPU, no
// thread, and no memory on any worker. The workflow's state lives on the
// server. Workers can be redeployed, restarted, or scaled to zero while this
// application sits here waiting, and the decision will still land correctly.

using Microsoft.Extensions.Logging;
using Temporalio.Common;
using Temporalio.Exceptions;
using Temporalio.Workflows;

namespace LoanUnderwriting;

[Workflow]
public class LoanUnderwritingWorkflow
{
    /// <summary>
    /// How long we wait for an underwriter before escalating. Short for the
    /// lab — a real desk would use hours. Drop it to 20 seconds if you want to
    /// watch the escalation fire without waiting around.
    /// </summary>
    private static readonly TimeSpan EscalationThreshold = TimeSpan.FromMinutes(2);

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

    // Workflow state. Plain fields — the event history is what makes them
    // durable, so there is nothing to persist by hand.
    private string               _stage       = "starting";
    private int                  _creditScore;
    private string               _riskTier    = "unknown";
    private UnderwriterDecision? _decision;
    private bool                 _escalated;

    /// <summary>
    /// Records the underwriter's ruling. An update (rather than a signal) because
    /// the caller gets a response back: they learn whether the ruling was accepted,
    /// and the validator can reject a bad one before it ever touches history.
    /// </summary>
    [WorkflowUpdate]
    public async Task<string> SubmitDecisionAsync(UnderwriterDecision decision)
    {
        _decision = decision;

        Workflow.Logger.LogInformation(
            "Decision '{Decision}' recorded by {Underwriter}",
            decision.Decision, decision.Underwriter);

        return $"Recorded '{decision.Decision}' by {decision.Underwriter}";
    }

    /// <summary>
    /// Runs before the update is accepted. If it throws, the update is rejected,
    /// the caller is told why, and nothing is written to the event history.
    /// A validator must not mutate state and must not await.
    /// </summary>
    [WorkflowUpdateValidator(nameof(SubmitDecisionAsync))]
    public void ValidateDecision(UnderwriterDecision decision)
    {
        if (decision.Decision is not ("approve" or "decline"))
        {
            throw new ApplicationFailureException(
                $"Decision must be 'approve' or 'decline', got '{decision.Decision}'");
        }

        if (_decision is not null)
        {
            throw new ApplicationFailureException(
                $"Application already decided by {_decision.Underwriter}");
        }
    }

    /// <summary>
    /// Reads current state without changing it. Queries never appear in the
    /// event history and can be called any time the workflow is open.
    /// Must be synchronous: no await, no mutation.
    /// </summary>
    [WorkflowQuery]
    public ApplicationStatus GetStatus() =>
        new(_stage, _creditScore, _riskTier, _decision?.Decision, _escalated);

    [WorkflowRun]
    public async Task<UnderwritingResult> RunAsync(LoanApplication app)
    {
        var startedAt = Workflow.UtcNow;

        Workflow.Logger.LogInformation(
            "Underwriting {ApplicationId} for ${Amount:N2} started at {StartedAt:u}",
            app.ApplicationId, app.RequestedAmount, startedAt);

        _stage = "credit-check";
        var credit = await Workflow.ExecuteActivityAsync(
            (LoanUnderwritingActivities a) => a.PullCreditAsync(app),
            CreditOptions);
        _creditScore = credit.Score;

        _stage = "fraud-check";
        await Workflow.ExecuteActivityAsync(
            (LoanUnderwritingActivities a) => a.CheckFraudAsync(app),
            DefaultOptions);

        _stage = "risk-scoring";
        var risk = await Workflow.ExecuteActivityAsync(
            (LoanUnderwritingActivities a) => a.ScoreRiskAsync(app, credit),
            DefaultOptions);
        _riskTier = risk.Tier;

        // ── Human in the loop ────────────────────────────────────────────────
        _stage = "awaiting-decision";

        Workflow.Logger.LogInformation(
            "{ApplicationId} is ready for an underwriter ({Tier} tier, score {Score})",
            app.ApplicationId, risk.Tier, credit.Score);

        // WaitConditionAsync with a timeout returns false if the timeout wins.
        var decided = await Workflow.WaitConditionAsync(
            () => _decision is not null,
            EscalationThreshold);

        if (!decided)
        {
            // Escalate and keep waiting. We deliberately do NOT fail or
            // auto-decline: an application nobody looked at is not a declined
            // application. The timer exists to raise a flag, not to make the
            // decision for the desk.
            _escalated = true;
            _stage     = "escalated";

            Workflow.Logger.LogWarning(
                "No decision on {ApplicationId} after {Minutes} minutes — escalating to the senior desk. Still waiting.",
                app.ApplicationId, EscalationThreshold.TotalMinutes);

            // No timeout this time: wait as long as it takes.
            await Workflow.WaitConditionAsync(() => _decision is not null);
        }

        var decision = _decision!;
        _stage = "completed";

        var status = decision.Decision == "approve" ? "Approved" : "Declined";

        Workflow.Logger.LogInformation(
            "{ApplicationId} {Status} by {Underwriter} after {Elapsed:g}",
            app.ApplicationId, status, decision.Underwriter, Workflow.UtcNow - startedAt);

        return new UnderwritingResult(
            ApplicationId: app.ApplicationId,
            Status:        status,
            CreditScore:   credit.Score,
            RiskTier:      risk.Tier,
            ApprovedRate:  risk.ApprovedRate
        );
    }
}
