﻿using BuildingBlocks.Enums;
using Carter;
using MediatR;
using Order.Application.Features.Command.Orders.CompleteOrder;

namespace Order.API.Endpoints.Internal.Orders;

public class CompleteOrderInternalEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/internal/orders/{id:guid}/complete", async (
            Guid id,
            CompleteOrderInternalRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var command = new CompleteOrderCommand(id);
            await sender.Send(command, ct);
            return Results.NoContent();
        })
        .WithName("CompleteOrderInternal")
        .WithDescription("تکمیل سفارش توسط سرویس‌های داخلی")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound)
        .WithTags("Internal")
        .AllowAnonymous();
    }
}

public record CompleteOrderInternalRequest(
    OrderStatus Status = OrderStatus.Completed,
    string Reason = "");