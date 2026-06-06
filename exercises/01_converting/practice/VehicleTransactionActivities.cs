// VehicleTransactionActivities.cs
//
// Activities are the failure-prone steps of your workflow — the parts that
// call external services and can be automatically retried by Temporal.
//
// Each method here corresponds to one step in VehicleTransactionPipeline.cs.
// Your job: implement the three activity methods below.
//
// The [Activity] attribute marks a method as a Temporal activity. Temporal
// will call these methods on your worker and retry them automatically if they
// throw an exception.

using Temporalio.Activities;

namespace VehicleTransaction;

public class VehicleTransactionActivities
{
    // TODO: Implement CheckFraudAsync
    //
    // Goal: call FraudService.CheckAsync(order.BuyerId) and return the result.
    //       If the service returns false, throw ApplicationFailureException
    //       with a non-retryable error so Temporal stops retrying immediately.
    //
    // Pattern:
    //   [Activity]
    //   public async Task<ReturnType> YourActivityAsync(InputType input)
    //   {
    //       var result = await SomeService.DoSomethingAsync(input.SomeField);
    //       if (!result.IsOk)
    //           throw new ApplicationFailureException("reason", nonRetryable: true);
    //       return result;
    //   }
    //
    // Note: ApplicationFailureException is how you tell Temporal "don't retry
    // this — it's a business rule failure, not a transient error."
    [Activity]
    public async Task<bool> CheckFraudAsync(VehicleOrder order)
    {
        throw new NotImplementedException("TODO: implement CheckFraudAsync");
    }

    // TODO: Implement ProcessPaymentAsync
    //
    // Goal: call PaymentService.ChargeAsync(order.BuyerId, order.PurchasePrice)
    //       and return the confirmation string.
    //
    // No special error handling needed here — let Temporal retry on any exception.
    [Activity]
    public async Task<string> ProcessPaymentAsync(VehicleOrder order)
    {
        throw new NotImplementedException("TODO: implement ProcessPaymentAsync");
    }

    // TODO: Implement TransferTitleAsync
    //
    // Goal: call TitleService.TransferAsync(order.VehicleId, order.BuyerId).
    //       Return void — this activity just needs to complete without throwing.
    [Activity]
    public async Task TransferTitleAsync(VehicleOrder order)
    {
        throw new NotImplementedException("TODO: implement TransferTitleAsync");
    }
}
