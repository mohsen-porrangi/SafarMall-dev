﻿//using MediatR;
//using Microsoft.Extensions.Logging;
//using System.Diagnostics;

//namespace Order.Application.Common.Behaviors;

//public class PerformanceBehavior<TRequest, TResponse>(
//    ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
//    : IPipelineBehavior<TRequest, TResponse>
//    where TRequest : IRequest<TResponse>
//{
//    private readonly Stopwatch _timer = new();

//    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
//    {
//        _timer.Restart();

//        var response = await next();

//        _timer.Stop();

//        var elapsedMilliseconds = _timer.ElapsedMilliseconds;

//        if (elapsedMilliseconds > 500)
//        {
//            var requestName = typeof(TRequest).Name;

//            logger.LogWarning("Long Running Request: {Name} ({ElapsedMilliseconds} milliseconds) {@Request}",
//                requestName, elapsedMilliseconds, request);
//        }

//        return response;
//    }
//}