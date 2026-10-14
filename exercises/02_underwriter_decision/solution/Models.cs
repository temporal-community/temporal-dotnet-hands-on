// Models.cs — shared types for Challenge 2

namespace LoanUnderwriting;

/// <summary>An auto-loan application submitted for underwriting.</summary>
public record LoanApplication(
    string  ApplicationId,
    string  ApplicantId,
    decimal RequestedAmount,
    int     TermMonths
);

/// <summary>Result of pulling the applicant's credit file.</summary>
public record CreditReport(
    int     Score,
    decimal ExistingDebt
);

/// <summary>Result of the fraud screen.</summary>
public record FraudCheck(
    bool   Cleared,
    string Signal
);

/// <summary>Result of scoring the application for risk.</summary>
public record RiskAssessment(
    string  Tier,
    decimal ApprovedRate
);

/// <summary>Final output of the underwriting workflow.</summary>
public record UnderwritingResult(
    string  ApplicationId,
    string  Status,
    int     CreditScore,
    string  RiskTier,
    decimal ApprovedRate
);

/// <summary>An underwriter's ruling on an application. Decision is "approve" or "decline".</summary>
public record UnderwriterDecision(
    string  Decision,
    string  Underwriter,
    string? Notes = null
);

/// <summary>Live view of an in-flight application, served by the query handler.</summary>
public record ApplicationStatus(
    string  Stage,
    int     CreditScore,
    string  RiskTier,
    string? Decision,
    bool    Escalated
);
