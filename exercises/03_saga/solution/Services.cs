// Services.cs — simulated downstream services.
//
// Each service keeps a little in-memory state so that "undo something that
// never happened" is a real, observable no-op rather than a comment. In
// production this state lives in the downstream system; the behaviour that
// matters — an undo for an action that never landed must be harmless — is
// the same.
//
// (State is per-worker-process, so restarting the worker forgets it. That is
// fine for a lab.)

using System.Collections.Concurrent;

namespace LoanUnderwriting;

public static class CreditHoldService
{
    private static readonly ConcurrentDictionary<string, decimal> Holds = new();

    public static async Task<string> PlaceAsync(string key, decimal amount)
    {
        await Task.Delay(300);
        Holds[key] = amount;
        return $"HOLD-{key[^8..]}";
    }

    /// <summary>Returns true if a hold actually existed. Safe to call any number of times.</summary>
    public static async Task<bool> ReleaseAsync(string key)
    {
        await Task.Delay(200);
        return Holds.TryRemove(key, out _);
    }
}

public static class DocumentService
{
    private static readonly ConcurrentDictionary<string, string> Packages = new();

    public static async Task<string> RequestAsync(string key)
    {
        await Task.Delay(400);
        var packageId = $"DOC-{key[^8..]}";
        Packages[key] = packageId;
        return packageId;
    }

    /// <summary>Returns true if a package actually existed. Safe to call any number of times.</summary>
    public static async Task<bool> CancelAsync(string key)
    {
        await Task.Delay(200);
        return Packages.TryRemove(key, out _);
    }
}

public static class FundingService
{
    /// <summary>The funding partner will not take a single loan above this amount.</summary>
    public const decimal PartnerLimit = 50_000m;

    private static readonly ConcurrentDictionary<string, decimal> Disbursements = new();

    public static async Task<string> DisburseAsync(string key, decimal amount)
    {
        await Task.Delay(400);
        Disbursements[key] = amount;
        return $"FUND-{key[^8..]}";
    }

    /// <summary>Returns true if funds had actually been disbursed. Safe to call any number of times.</summary>
    public static async Task<bool> ReverseAsync(string key)
    {
        await Task.Delay(300);
        return Disbursements.TryRemove(key, out _);
    }
}
