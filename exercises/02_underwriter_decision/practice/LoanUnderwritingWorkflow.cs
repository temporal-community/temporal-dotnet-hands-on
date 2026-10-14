// LoanUnderwritingWorkflow.cs
//
// Challenge 1 ran straight through. Real underwriting doesn't: after the
// automated checks, a human has to rule on the application. That human might
// answer in thirty seconds or next Tuesday.
//
// A workflow waiting on Workflow.WaitConditionAsync consumes no CPU, no
// thread, and no memory on any worker. The workflow's state lives on the
// server, so workers can be restarted or scaled to zero while this
// application waits, and the decision will still land correctly.
//
// The three activity calls are already wired up from Challenge 1. Your job is
// the human-in-the-loop part: an update handler, a query handler, and the
// wait that sits between them.

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

    // TODO 1 — Make this an update handler, and record the ruling.
    //
    // Why an update rather than a signal? An update returns a value to the
    // caller, so the underwriter learns whether their ruling was accepted.
    // A signal is fire-and-forget.
    //
    //   a) Decorate this method with [WorkflowUpdate].
    //   b) In the body: set _decision = decision, then return a short
    //      confirmation string such as
    //        $"Recorded '{decision.Decision}' by {decision.Underwriter}"
    public async Task<string> SubmitDecisionAsync(UnderwriterDecision decision)
    {
        throw new NotImplementedException("TODO 1: record the decision and return a confirmation");
    }

    // TODO 2 — Add a validator for that update.
    //
    // A validator runs BEFORE the update is accepted. If it throws, the update
    // is rejected, the caller is told why, and nothing is written to the event
    // history at all. Validators must not mutate state and must not await.
    //
    //   [WorkflowUpdateValidator(nameof(SubmitDecisionAsync))]
    //   public void ValidateDecision(UnderwriterDecision decision)
    //   {
    //       // Reject anything that is not "approve" or "decline".
    //       // Reject a second ruling if _decision is already set.
    //       // Throw ApplicationFailureException with a clear message.
    //   }

    // TODO 3 — Make this a query handler.
    //
    // The body is already written. Queries read state without changing it,
    // never appear in the event history, and must be synchronous — no await,
    // no mutation. All this method needs is the [WorkflowQuery] attribute.
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

        // TODO 4 — Wait for the decision, and escalate if nobody rules in time.
        //
        //   a) Wait, with a timeout. WaitConditionAsync returns false when the
        //      timeout wins rather than the condition:
        //        var decided = await Workflow.WaitConditionAsync(
        //            () => _decision is not null,
        //            EscalationThreshold);
        //
        //   b) If it timed out: set _escalated = true, set _stage = "escalated",
        //      and log a warning with Workflow.Logger.LogWarning.
        //
        //   c) Then wait again — this time with NO timeout, so the workflow
        //      waits as long as it takes:
        //        await Workflow.WaitConditionAsync(() => _decision is not null);
        //
        //      We deliberately do not fail or auto-decline. An application
        //      nobody looked at is not a declined application. The timer raises
        //      a flag; it does not make the decision for the desk.
        throw new NotImplementedException("TODO 4: wait for the decision, escalating if it is slow");

        // Once TODO 4 is done, delete the throw above and uncomment the rest:
        //
        // var decision = _decision!;
        // _stage = "completed";
        //
        // var status = decision.Decision == "approve" ? "Approved" : "Declined";
        //
        // Workflow.Logger.LogInformation(
        //     "{ApplicationId} {Status} by {Underwriter} after {Elapsed:g}",
        //     app.ApplicationId, status, decision.Underwriter, Workflow.UtcNow - startedAt);
        //
        // return new UnderwritingResult(
        //     ApplicationId: app.ApplicationId,
        //     Status:        status,
        //     CreditScore:   credit.Score,
        //     RiskTier:      risk.Tier,
        //     ApprovedRate:  risk.ApprovedRate
        // );
    }
}
