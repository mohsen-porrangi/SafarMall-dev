using BuildingBlocks.Enums;

namespace BuildingBlocks.Models.DTOs;

/// <summary>
/// Shared passenger information DTO for train services
/// </summary>
public record TrainPassengerInfo
{
    public string FirstNameFa { get; init; } = string.Empty;
    public string LastNameFa { get; init; } = string.Empty;
    public string FirstNameEn { get; init; } = string.Empty;
    public string LastNameEn { get; init; } = string.Empty;
    public DateTime BirthDate { get; init; }
    public Gender Gender { get; init; }
    public bool IsIranian { get; init; }
    public string? NationalCode { get; init; }
    public string? PassportNo { get; init; }
}