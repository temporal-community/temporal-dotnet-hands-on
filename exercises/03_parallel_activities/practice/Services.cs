// Services.cs — simulated downstream services for Exercise 3

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

public static class InventoryService
{
    public static async Task<bool> ConfirmAsync(string vehicleId)
    {
        await Task.Delay(300);
        if (Random.Shared.NextDouble() < 0.3)
            throw new HttpRequestException("Inventory service temporarily unavailable");
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
