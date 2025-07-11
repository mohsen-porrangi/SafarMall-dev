using BuildingBlocks.CQRS;
using Order.Application.Common.DTOs;

namespace Order.Application.Features.Queries.Passengers.GetSavedPassengers;

public record GetSavedPassengersQuery : IQuery<List<SavedPassengerDto>>;