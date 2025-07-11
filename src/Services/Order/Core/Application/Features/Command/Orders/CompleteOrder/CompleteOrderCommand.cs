using BuildingBlocks.CQRS;

namespace Order.Application.Features.Command.Orders.CompleteOrder;

public record CompleteOrderCommand(Guid OrderId) : ICommand;