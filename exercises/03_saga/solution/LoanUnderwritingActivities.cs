// LoanUnderwritingActivities.cs — SOLUTION
//
// Three forward steps, each paired with a compensation that undoes it.
//
// Every compensation here is IDEMPOTENT: calling it when the forward step
// never ran, or calling it twice, is a harmless no-op. That is not optional
// politeness. Compensations are registered *before* their action runs, so a
// failure in between means an undo fires for something that never happened.
// On top of that, Temporal retries compensations like any other activity, so
// one can legitimately run more than once.

using Microsoft.Extensions.Logging;
using Temporalio.Activities;
using Temporalio.Exceptions;

namespace LoanUnderwriting;

public class LoanUnderwritingActivities
{
    /// <summary>
    /// Stable per-application key. Every attempt of every call for this
    /// application derives the same key, which is what lets the downstream
    /// services recognise duplicates.
    /// </summary>
    private static string KeyFor(LoanApplication app) =>
        $"{ActivityExecutionContext.Current.Info.WorkflowId}:{app.ApplicationId}";

    // ── Step 1: credit hold ──────────────────────────────────────────────────

    [Activity]
    public async Task<string> PlaceCreditHoldAsync(LoanApplication app)
    {
        var logger = ActivityExecutionContext.Current.Logger;
        var holdId = await CreditHoldService.PlaceAsync(KeyFor(app), app.RequestedAmount);

        logger.LogInformation(
            "Placed credit hold {HoldId} for ${Amount:N2} on {ApplicationId}",
            holdId, app.RequestedAmount, app.ApplicationId);

        return holdId;
    }

    [Activity]
    public async Task ReleaseCreditHoldAsync(LoanApplication app)
    {
        var logger   = ActivityExecutionContext.Current.Logger;
        var released = await CreditHoldService.ReleaseAsync(KeyFor(app));

        logger.LogInformation(released
            ? $"[Compensation] Released credit hold on {app.ApplicationId}"
            : $"[Compensation] No credit hold on {app.ApplicationId} to release (no-op)");
    }

    // ── Step 2: document package ─────────────────────────────────────────────

    [Activity]
    public async Task<string> RequestDocumentsAsync(LoanApplication app)
    {
        var logger    = ActivityExecutionContext.Current.Logger;
        var packageId = await DocumentService.RequestAsync(KeyFor(app));

        logger.LogInformation(
            "Requested document package {PackageId} for {ApplicationId}",
            packageId, app.ApplicationId);

        return packageId;
    }

    [Activity]
    public async Task CancelDocumentRequestAsync(LoanApplication app)
    {
        var logger    = ActivityExecutionContext.Current.Logger;
        var cancelled = await DocumentService.CancelAsync(KeyFor(app));

        logger.LogInformation(cancelled
            ? $"[Compensation] Cancelled document package for {app.ApplicationId}"
            : $"[Compensation] No document package for {app.ApplicationId} to cancel (no-op)");
    }

    // ── Step 3: funding ──────────────────────────────────────────────────────

    [Activity]
    public async Task<string> FundLoanAsync(LoanApplication app)
    {
        var logger = ActivityExecutionContext.Current.Logger;

        // A late decline from the funding partner. This is a business rule,
        // not a transient fault, so it is non-retryable: retrying would waste
        // attempts and reach the same answer. It is also the most expensive
        // place to fail, because the hold and the documents already exist.
        if (app.RequestedAmount > FundingService.PartnerLimit)
        {
            logger.LogWarning(
                "Funding partner declined {ApplicationId}: ${Amount:N2} exceeds the ${Limit:N2} limit",
                app.ApplicationId, app.RequestedAmount, FundingService.PartnerLimit);

            throw new ApplicationFailureException(
                $"Funding partner declined {app.ApplicationId}: ${app.RequestedAmount:N2} exceeds the ${FundingService.PartnerLimit:N2} per-loan limit",
                nonRetryable: true);
        }

        var reference = await FundingService.DisburseAsync(KeyFor(app), app.RequestedAmount);

        logger.LogInformation(
            "Disbursed ${Amount:N2} for {ApplicationId} as {Reference}",
            app.RequestedAmount, app.ApplicationId, reference);

        return reference;
    }

    [Activity]
    public async Task ReverseFundingAsync(LoanApplication app)
    {
        var logger   = ActivityExecutionContext.Current.Logger;
        var reversed = await FundingService.ReverseAsync(KeyFor(app));

        logger.LogInformation(reversed
            ? $"[Compensation] Reversed funding for {app.ApplicationId}"
            : $"[Compensation] No funding for {app.ApplicationId} to reverse (no-op)");
    }
}
