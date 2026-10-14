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
    // Call FraudService.ScreenAsync(app.ApplicantId) and return the result.
    //
    // If the screen does NOT clear, that is a business decision, not a glitch.
    // Retrying would burn attempts and reach the same answer, so throw a
    // non-retryable failure and let the workflow fail fast:
    //
    //   throw new ApplicationFailureException(
    //       $"Fraud screen flagged application {app.ApplicationId}: {check.Signal}",
    //       nonRetryable: true);
    [Activity]
    public async Task<FraudCheck> CheckFraudAsync(LoanApplication app)
    {
        throw new NotImplementedException("TODO 2: implement CheckFraudAsync");
    }

    // TODO 3 — Implement ScoreRiskAsync.
    //
    // Call RiskEngineService.ScoreAsync(credit.Score, app.RequestedAmount)
    // and return the RiskAssessment. Log the resulting tier if you like —
    // the logger comes from ActivityExecutionContext.Current.Logger.
    [Activity]
    public async Task<RiskAssessment> ScoreRiskAsync(LoanApplication app, CreditReport credit)
    {
        throw new NotImplementedException("TODO 3: implement ScoreRiskAsync");
    }
}
