// VehicleTransactionPipeline.cs
//
// This is the BEFORE — a realistic but fragile implementation of a vehicle
// purchase transaction. Read through it before opening the other files.
//
// Problems to spot:
//   - Retry logic is manual, inconsistent, and invisible to any observer
//   - State lives only in local variables: if this process crashes, the
//     transaction is lost with no way to resume
//   - A failure in step 3 leaves step 1 and 2 partially applied with no
//     compensation
//   - Every engineer who touches this has to understand the retry math

namespace VehicleTransaction;

public static class VehicleTransactionPipeline
{
    public static async Task<string> ProcessAsync(VehicleOrder order)
    {
        Console.WriteLine($"[Pipeline] Starting transaction for vehicle {order.VehicleId}");

        // Step 1: Fraud check — retry up to 3 times with fixed delay
        bool fraudCleared = false;
        for (int attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                fraudCleared = await FraudService.CheckAsync(order.BuyerId);
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Pipeline] Fraud check attempt {attempt} failed: {ex.Message}");
                if (attempt == 3) throw;
                await Task.Delay(TimeSpan.FromSeconds(2));
            }
        }

        if (!fraudCleared)
            throw new InvalidOperationException("Fraud check failed — transaction blocked");

        // Step 2: Process payment — retry up to 5 times with backoff
        string? paymentConfirmation = null;
        for (int attempt = 1; attempt <= 5; attempt++)
        {
            try
            {
                paymentConfirmation = await PaymentService.ChargeAsync(
                    order.BuyerId, order.PurchasePrice);
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Pipeline] Payment attempt {attempt} failed: {ex.Message}");
                if (attempt == 5) throw;
                await Task.Delay(TimeSpan.FromSeconds(attempt * 2)); // ad-hoc backoff
            }
        }

        // Step 3: Transfer title — no retries (someone forgot)
        await TitleService.TransferAsync(order.VehicleId, order.BuyerId);

        Console.WriteLine($"[Pipeline] Transaction complete. Confirmation: {paymentConfirmation}");
        return paymentConfirmation!;
    }
}

// ── Simulated downstream services ────────────────────────────────────────────
// These stand in for real HTTP calls. They fail randomly to simulate
// the transient errors that make the retry logic above necessary.

public static class FraudService
{
    public static async Task<bool> CheckAsync(string buyerId)
    {
        await Task.Delay(200);
        if (Random.Shared.NextDouble() < 0.4)
            throw new HttpRequestException("Fraud service temporarily unavailable");
        return true;
    }
}

public static class PaymentService
{
    public static async Task<string> ChargeAsync(string buyerId, decimal amount)
    {
        await Task.Delay(300);
        if (Random.Shared.NextDouble() < 0.4)
            throw new HttpRequestException("Payment gateway timeout");
        return $"PAY-{Guid.NewGuid():N}".ToUpper()[..16];
    }
}

public static class TitleService
{
    public static async Task TransferAsync(string vehicleId, string buyerId)
    {
        await Task.Delay(250);
        if (Random.Shared.NextDouble() < 0.4)
            throw new HttpRequestException("Title registry service unavailable");
    }
}
