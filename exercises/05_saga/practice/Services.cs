// Services.cs — simulated downstream services for Exercise 5
//
// TitleService.TransferAsync fails ~60% of the time in this exercise
// so you can reliably observe compensations running.

namespace VehicleTransaction;

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
        if (Random.Shared.NextDouble() < 0.3)
            throw new HttpRequestException("Payment gateway timeout");
        return $"PAY-{Guid.NewGuid():N}".ToUpper()[..16];
    }

    public static async Task RefundAsync(string buyerId, decimal amount, string confirmation)
    {
        await Task.Delay(200);
        // Refunds are reliable — they always succeed after retries
    }
}

public static class TitleService
{
    public static async Task TransferAsync(string vehicleId, string buyerId)
    {
        await Task.Delay(250);
        // Fails more often so compensations are easy to observe
        if (Random.Shared.NextDouble() < 0.6)
            throw new HttpRequestException("Title registry service unavailable");
    }

    public static async Task RevertAsync(string vehicleId, string buyerId)
    {
        await Task.Delay(200);
        // Reversions are reliable
    }
}
