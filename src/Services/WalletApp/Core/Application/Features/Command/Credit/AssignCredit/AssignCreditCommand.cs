using BuildingBlocks.CQRS;
using BuildingBlocks.Enums;

namespace WalletApp.Application.Features.Command.Credit.AssignCredit;

/// <summary>
/// Command to assign credit limit to B2B customer
/// </summary>
public record AssignCreditCommand : ICommand<AssignCreditResponse>
{
    /// <summary>
    /// Target user ID for credit assignment
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// Credit limit amount
    /// </summary>
    public decimal CreditAmount { get; init; }

    /// <summary>
    /// Currency for credit (default: IRR)
    /// </summary>
    public CurrencyCode Currency { get; init; } = CurrencyCode.IRR;

    /// <summary>
    /// Credit due date
    /// </summary>
    public DateTime DueDate { get; init; }

    /// <summary>
    /// Description/reason for credit assignment
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Company context (B2B)
    /// </summary>
    public string? CompanyName { get; init; }

    /// <summary>
    /// Credit reference/contract number
    /// </summary>
    public string? ReferenceNumber { get; init; }
}

/// <summary>
/// Response for credit assignment
/// </summary>
public record AssignCreditResponse
{
    public bool IsSuccess { get; init; }
    public Guid? CreditId { get; init; }
    public string? ErrorMessage { get; init; }
    public decimal? AssignedAmount { get; init; }
    public DateTime? DueDate { get; init; }
}