// LoanUnderwritingActivities.cs
//
// Carried over complete from Challenge 1 — nothing to change here.
//
// Activities are where the dangerous work happens: network calls, writes to
// other systems, anything that can fail or take time. Temporal retries them
// for you according to the RetryPolicy in ActivityOptions.

using Microsoft.Extensions.Logging;
using Temporalio.Activities;
using Temporalio.Exceptions;

namespace LoanUnderwriting;

public class LoanUnderwritingActivities
{
    [Activity]
    public async Task<CreditReport> PullCreditAsync(LoanApplication app)
    {
        // ActivityExecutionContext.Current gives the activity its Temporal
        // identity: which workflow started it, which attempt this is, and the
        // ActivityId that stays stable across every retry of this same call.
        var info   = ActivityExecutionContext.Current.Info;
        var logger = ActivityExecutionContext.Current.Logger;

        // WorkflowId + ActivityId is stable across retries but unique per
        // activity call — exactly what an idempotency key needs to be. Send it
        // downstream so a retry after a half-finished call is a no-op, not a
        // second credit pull.
        var idempotencyKey = $"{info.WorkflowId}:{info.ActivityId}";

        logger.LogInformation(
            "Pulling credit for {ApplicantId} (attempt {Attempt}, idempotency key {Key})",
            app.ApplicantId, info.Attempt, idempotencyKey);

        // The credit bureau is flaky. The first two attempts fail; the third
        // succeeds. This is deterministic so the lab behaves the same for
        // everyone — a real bureau would fail unpredictably, and your retry
        // policy would absorb it the same way.
        if (info.Attempt < 3)
        {
            logger.LogWarning(
                "Credit bureau returned 503 on attempt {Attempt} — Temporal will retry",
                info.Attempt);

            throw new HttpRequestException(
                $"Credit bureau temporarily unavailable (attempt {info.Attempt})");
        }

        var report = await CreditBureauService.PullAsync(app.ApplicantId, idempotencyKey);

        logger.LogInformation(
            "Credit pulled for {ApplicantId}: score {Score}",
            app.ApplicantId, report.Score);

        return report;
    }

    [Activity]
    public async Task<FraudCheck> CheckFraudAsync(LoanApplication app)
    {
        var info   = ActivityExecutionContext.Current.Info;
        var logger = ActivityExecutionContext.Current.Logger;

        logger.LogInformation(
            "Screening {ApplicantId} for fraud (attempt {Attempt})",
            app.ApplicantId, info.Attempt);

        var check = await FraudService.ScreenAsync(app.ApplicantId);

        // A fraud hit is a business decision, not a transient glitch. Retrying
        // it would just burn attempts and reach the same answer, so mark it
        // non-retryable and let the workflow fail fast.
        if (!check.Cleared)
        {
            throw new ApplicationFailureException(
                $"Fraud screen flagged application {app.ApplicationId}: {check.Signal}",
                nonRetryable: true);
        }

        return check;
    }

    [Activity]
    public async Task<RiskAssessment> ScoreRiskAsync(LoanApplication app, CreditReport credit)
    {
        var logger = ActivityExecutionContext.Current.Logger;

        var assessment = await RiskEngineService.ScoreAsync(credit.Score, app.RequestedAmount);

        logger.LogInformation(
            "Risk scored for {ApplicationId}: tier {Tier} at {Rate:P2}",
            app.ApplicationId, assessment.Tier, assessment.ApprovedRate);

        return assessment;
    }
}
