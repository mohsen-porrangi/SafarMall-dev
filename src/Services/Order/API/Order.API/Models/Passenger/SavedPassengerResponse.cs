﻿namespace Order.API.Models.Passenger;

public record SavedPassengerResponse
{
    public long Id { get; init; }
    public string FirstNameEn { get; init; } = string.Empty;
    public string LastNameEn { get; init; } = string.Empty;
    public string FirstNameFa { get; init; } = string.Empty;
    public string LastNameFa { get; init; } = string.Empty;
    public string NationalCode { get; init; } = string.Empty;
    public string? PassportNo { get; init; }
    public DateTime BirthDate { get; init; }
    public BuildingBlocks.Enums.Gender Gender { get; init; }
}