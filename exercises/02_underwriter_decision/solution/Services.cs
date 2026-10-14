// Services.cs — simulated downstream services.
//
// These stand in for the real systems an underwriting platform would call.
// They live outside the workflow on purpose: they sleep, they use random
// numbers, and they fail. None of that is safe inside workflow code, which
// is exactly why they are only ever called from activities.

namespace LoanUnderwriting;

public static class CreditBureauService
{
    public static async Task<CreditReport> PullAsync(string applicantId, string idempotencyKey)
    {
        await Task.Delay(400);
        return new CreditReport(
            Score:        680 + Math.Abs(applicantId.GetHashCode() % 120),
            ExistingDebt: 12_500.00m
        );
    }
}

public static class FraudService
{
    public static async Task<FraudCheck> ScreenAsync(string applicantId)
    {
        await Task.Delay(600);
        return new FraudCheck(Cleared: true, Signal: "no-match");
    }
}

public static class RiskEngineService
{
    public static async Task<RiskAssessment> ScoreAsync(int creditScore, decimal requestedAmount)
    {
        await Task.Delay(400);

        var (tier, rate) = creditScore switch
        {
            >= 760 => ("prime",      0.0549m),
            >= 700 => ("near-prime", 0.0729m),
            _      => ("subprime",   0.1149m),
        };

        return new RiskAssessment(tier, rate);
    }
}
