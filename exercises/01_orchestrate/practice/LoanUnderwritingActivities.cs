// LoanUnderwritingActivities.cs
//
// Activities are where the dangerous work happens: network calls, writes to
// other systems, anything that can fail or take time. Temporal retries them
// for you according to the RetryPolicy you set in ActivityOptions.
//
// Your job: implement the three activities below.

using Microsoft.Extensions.Logging;
using Temporalio.Activities;
using Temporalio.Exceptions;

namespace LoanUnderwriting;

public class LoanUnderwritingActivities
{
    // TODO 1 — Implement PullCreditAsync.
    //
    // This activity must do four things:
    //
    //   a) Get the activity context:
    //        var info   = ActivityExecutionContext.Current.Info;
    //        var logger = ActivityExecutionContext.Current.Logger;
    //
    //   b) Build an idempotency key from the workflow id and the activity id:
    //        var idempotencyKey = $"{info.WorkflowId}:{info.ActivityId}";
    //
    //      Why those two fields? The ActivityId stays the same across every
    //      retry of this call, but differs from every other activity call. So
    //      a retry reuses the key and the downstream service can recognise the
    //      duplicate instead of pulling credit twice.
    //
    //   c) Log the attempt number so you can watch retries happen:
    //        logger.LogInformation(
    //            "Pulling credit for {ApplicantId} (attempt {Attempt}, idempotency key {Key})",
    //            app.ApplicantId, info.Attempt, idempotencyKey);
    //
    //   d) Simulate the flaky bureau — fail on the first two attempts:
    //        if (info.Attempt < 3)
    //            throw new HttpRequestException(
    //                $"Credit bureau temporarily unavailable (attempt {info.Attempt})");
    //
    //      Then return await CreditBureauService.PullAsync(app.ApplicantId, idempotencyKey);
    [Activity]
    public async Task<CreditReport> PullCreditAsync(LoanApplication app)
    {
        throw new NotImplementedException("TODO 1: implement PullCreditAsync");
    }

    // TODO 2 — Implement CheckFraudAsync.
    //
    //   a) Call the service and await it. ScreenAsync returns Task<FraudCheck>;
    //      awaiting it gives you the FraudCheck itself, so you can read its
    //      fields:
    //        var check = await FraudService.ScreenAsync(app.ApplicantId);
    //
    //   b) If the screen did NOT clear, throw. A flagged applicant is a
    //      business decision, not a glitch — retrying would burn attempts and
    //      reach the same answer, so mark it non-retryable and let the
    //      workflow fail fast:
    //        if (!check.Cleared)
    //            throw new ApplicationFailureException(
    //                $"Fraud screen flagged application {app.ApplicationId}: {check.Signal}",
    //                nonRetryable: true);
    //
    //   c) Otherwise return the check:
    //        return check;
    //
    // The two paths never collide: throw leaves the method immediately, so
    // the return is only reached when the screen cleared.
    //
    // (In this lab the fraud service always clears, so the throw is the path
    // you write but will not see. Flip Cleared to false in Services.cs if you
    // want to watch a non-retryable failure stop the workflow dead.)
    [Activity]
    public async Task<FraudCheck> CheckFraudAsync(LoanApplication app)
    {
        throw new NotImplementedException("TODO 2: implement CheckFraudAsync");
    }

    // TODO 3 — Implement ScoreRiskAsync.
    //
    // Same shape as TODO 2, minus the error case:
    //
    //   var assessment = await RiskEngineService.ScoreAsync(credit.Score, app.RequestedAmount);
    //   return assessment;
    //
    // Optionally log the tier first — the logger comes from
    // ActivityExecutionContext.Current.Logger:
    //
    //   logger.LogInformation(
    //       "Risk scored for {ApplicationId}: tier {Tier}",
    //       app.ApplicationId, assessment.Tier);
    [Activity]
    public async Task<RiskAssessment> ScoreRiskAsync(LoanApplication app, CreditReport credit)
    {
        throw new NotImplementedException("TODO 3: implement ScoreRiskAsync");
    }
}
