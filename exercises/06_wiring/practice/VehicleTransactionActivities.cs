// VehicleTransactionActivities.cs
//
// This class is the home for your activity methods.
// Right now it is empty. Your job in Stage A is to move the three service
// calls out of the workflow and into this class, then mark each method
// with the [Activity] attribute so Temporal knows how to schedule them.
//
// The [Activity] attribute is what makes a method discoverable by the worker.
// Without it, Temporal will not schedule the method and will throw at startup.

using Temporalio.Activities;
using Temporalio.Exceptions;

namespace VehicleTransaction;

public class VehicleTransactionActivities
{
    // TODO (Stage A): Add CheckFraudAsync
    //
    // Move the FraudService.CheckAsync call here from the workflow.
    // Add [Activity] above the method signature.
    // If the service returns false, throw ApplicationFailureException with
    // nonRetryable: true — fraud failure is a business rule, not a transient error.
    //
    // Signature:
    //   [Activity]
    //   public async Task<bool> CheckFraudAsync(VehicleOrder order) { ... }


    // TODO (Stage A): Add ProcessPaymentAsync
    //
    // Move the PaymentService.ChargeAsync call here from the workflow.
    // Add [Activity] above the method signature.
    // Return the confirmation string from the service.
    //
    // Signature:
    //   [Activity]
    //   public async Task<string> ProcessPaymentAsync(VehicleOrder order) { ... }


    // TODO (Stage A): Add TransferTitleAsync
    //
    // Move the TitleService.TransferAsync call here from the workflow.
    // Add [Activity] above the method signature.
    //
    // Signature:
    //   [Activity]
    //   public async Task TransferTitleAsync(VehicleOrder order) { ... }
}
