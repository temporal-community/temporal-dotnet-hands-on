// Models.cs — shared types for Challenge 3

namespace LoanUnderwriting;

/// <summary>An approved auto-loan application, ready to be funded.</summary>
public record LoanApplication(
    string  ApplicationId,
    string  ApplicantId,
    decimal RequestedAmount,
    int     TermMonths
);

/// <summary>Outcome of the funding saga.</summary>
public record FundingResult(
    string ApplicationId,
    string Status,
    string FundingReference
);
