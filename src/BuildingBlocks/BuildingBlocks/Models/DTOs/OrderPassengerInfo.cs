using BuildingBlocks.Enums;

namespace BuildingBlocks.Models.DTOs;

/// <summary>
/// Order passenger information for external service communication
/// Standard DTO for passenger data exchange between services
/// </summary>
public record OrderPassengerInfo(
    string FirstName,
    string LastName,
    DateTime BirthDate,
    Gender Gender,
    bool IsIranian,
    string? NationalCode,
    string? PassportNo
);

/// <summary>
/// Create order passenger information for Order Service API
/// Matches Order Service CreateOrderPassengerInfo structure exactly
/// </summary>
public record CreateOrderPassengerInfo(
    string? FirstNameEn,
    string? LastNameEn,
    string? FirstNameFa,
    string? LastNameFa,
    DateTime BirthDate,
    Gender Gender,
    bool IsIranian,
    string? NationalCode,
    string? PassportNo
);