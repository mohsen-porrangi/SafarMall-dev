using BuildingBlocks.CQRS;

namespace Order.Application.Features.Command.Passengers.DeleteSavedPassenger;

public record DeleteSavedPassengerCommand(long PassengerId, Guid UserId) : ICommand;
