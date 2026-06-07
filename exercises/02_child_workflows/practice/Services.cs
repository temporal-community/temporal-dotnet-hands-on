// Services.cs — simulated downstream services for Exercise 2
//
// These stand in for real HTTP calls. They fail randomly to simulate
// the transient errors that Temporal's retry policy handles automatically.

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

public static class DocumentService
{
    public static async Task<string> GenerateBillOfSaleAsync(
        string vehicleId, string buyerId, string paymentConfirmation)
    {
        await Task.Delay(400);
        if (Random.Shared.NextDouble() < 0.3)
            throw new HttpRequestException("Document service temporarily unavailable");
        return $"https://docs.coxauto.internal/bill-of-sale/{vehicleId}-{paymentConfirmation}.pdf";
    }

    public static async Task<string> GenerateTitleDocumentAsync(
        string vehicleId, string buyerId)
    {
        await Task.Delay(350);
        if (Random.Shared.NextDouble() < 0.3)
            throw new HttpRequestException("Document service temporarily unavailable");
        return $"https://docs.coxauto.internal/title/{vehicleId}-{buyerId}.pdf";
    }
}
