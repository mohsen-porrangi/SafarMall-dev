using BuildingBlocks.Contracts;
using BuildingBlocks.Domain;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System.Collections.Concurrent;

namespace BuildingBlocks.Utils.SafeLog.LogService;
public class LogService : ILogService, IAsyncDisposable
{

    private readonly IMongoCollection<LogEntryModel> _logsInfo;
    private readonly IMongoCollection<LogEntryModel> _logsError;
    private readonly IMongoCollection<LogEntryModel> _logsRequest;
    private readonly ConcurrentQueue<LogEntryModel> _logQueue = new();
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly Task _backgroundTask;

    private readonly string _logFilePath;
    private readonly string _logMongoFilePath;

    public LogService(IMongoClient mongoClient, IOptions<LogOptions> loggingOptions, IOptions<MongoOptions> mongoOptions)
    {
        var database = mongoClient.GetDatabase(mongoOptions.Value.LogDatabaseName);

        _logsInfo = database.GetCollection<LogEntryModel>(loggingOptions.Value.MongoInfoCollection);
        _logsError = database.GetCollection<LogEntryModel>(loggingOptions.Value.MongoErrorCollection);
        _logsRequest = database.GetCollection<LogEntryModel>(loggingOptions.Value.MongoRequestCollection);

        var relativePath = loggingOptions.Value.LogFilePath;
        _logFilePath = Path.Combine(AppContext.BaseDirectory, relativePath);

        var dir = Path.GetDirectoryName(_logFilePath);
        if (!string.IsNullOrWhiteSpace(dir))
            Directory.CreateDirectory(dir);


        var relativeMongoErrorPath = loggingOptions.Value.InternalErrorMongo;
        _logMongoFilePath = Path.Combine(AppContext.BaseDirectory, relativeMongoErrorPath);

        var mongoDir = Path.GetDirectoryName(_logMongoFilePath);
        if (!string.IsNullOrWhiteSpace(mongoDir))
            Directory.CreateDirectory(mongoDir);

        _backgroundTask = Task.Run(ProcessLogQueueAsync);
    }

    public async ValueTask DisposeAsync()
    {
        _cancellationTokenSource.Cancel();
        await _backgroundTask;
    }

    private async Task ProcessLogQueueAsync()
    {
        while (!_cancellationTokenSource.Token.IsCancellationRequested)
        {
            try
            {
                if (_logQueue.TryDequeue(out var log))
                {
                    await WriteToMongoDB(log);
                }
                else
                {
                    await Task.Delay(100, _cancellationTokenSource.Token);
                }
            }
            catch (Exception ex)
            {
                try
                {
                    await WriteToFile(new LogEntryModel
                    {
                        Timestamp = DateTime.Now,
                        Level = LogLevelsEnums.Error.ToString(),
                        Message = $"Background log processor failed: {ex.Message}",
                        StackTrace = ex.StackTrace
                    });
                }
                catch
                {
                    // fallback نهایی: سکوت
                }
            }
        }
    }

    private async Task WriteToMongoDB(LogEntryModel log)
    {
        try
        {
            switch (log.Level)
            {
                case nameof(LogLevelsEnums.Info):
                    await _logsInfo.InsertOneAsync(log);
                    break;
                case nameof(LogLevelsEnums.Error):
                    await _logsError.InsertOneAsync(log);
                    break;
                case nameof(LogLevelsEnums.Request):
                    await _logsRequest.InsertOneAsync(log);
                    break;
            }
        }
        catch (Exception ex)
        {
            await WriteMongoErrorToFile(ex);
            await WriteToFile(log);
        }
    }

    private async Task WriteMongoErrorToFile(Exception ex)
    {
        var logMessage = ex.Message;
        await File.AppendAllTextAsync(_logMongoFilePath, logMessage);
    }

    private async Task WriteToFile(LogEntryModel log)
    {
        try
        {
            var logMessage = $"RequestId: {log.RequestId} - {log.Timestamp:o} - [{log.Level}] - {log.Message} - {log.Service} - {log.Method} - {log.UserId} - {log.IpAddress}{Environment.NewLine}";
            await File.AppendAllTextAsync(_logFilePath, logMessage);
        }
        catch (IOException ex)
        {
            // در صورت بروز مشکل در دسترسی به فایل، گزارش خطا
            Console.WriteLine($"Error writing to log file: {ex.Message}");
            // همچنین می‌توانید خطا را در سیستم لاگینگ دیگری ذخیره کنید یا یک آلارم ارسال کنید
        }
        catch (UnauthorizedAccessException ex)
        {
            // در صورت نداشتن دسترسی به فایل، گزارش خطا
            Console.WriteLine($"Access denied to log file: {ex.Message}");
        }
        catch (Exception ex)
        {
            // برای دیگر انواع خطاها، گزارش خطا
            Console.WriteLine($"Unexpected error: {ex.Message}");
        }
    }


    private void EnqueueLog(LogEntryModel log) => _logQueue.Enqueue(log);

    public Task LogInfo(string requestId, string message, string? method, string userId, string ipAddress, Dictionary<string, object>? additionalData = null)
    {
        EnqueueLog(new LogEntryModel
        {
            RequestId = requestId,
            Timestamp = DateTime.Now,
            Level = LogLevelsEnums.Info.ToString(),
            Message = message,
            Service = LogServiceProvidersEnums.ServiceAPI.ToString(),
            Method = method,
            UserId = userId,
            IpAddress = ipAddress,
            AdditionalData = additionalData
        });

        return Task.CompletedTask;
    }

    public Task LogError(string requestId, string message, string? method, string userId, string ipAddress, string stackTrace, Dictionary<string, object>? additionalData = null)
    {
        EnqueueLog(new LogEntryModel
        {
            RequestId = requestId,
            Timestamp = DateTime.Now,
            Level = LogLevelsEnums.Error.ToString(),
            Message = message,
            Service = LogServiceProvidersEnums.ServiceAPI.ToString(),
            Method = method,
            UserId = userId,
            IpAddress = ipAddress,
            StackTrace = stackTrace,
            AdditionalData = additionalData
        });

        return Task.CompletedTask;
    }

    public Task LogRequest(string requestId, string url, string? method, string userId, string ipAddress, Dictionary<string, string> headers, object body, int responseStatus, long responseTimeMs)
    {
        var additionalData = new Dictionary<string, object>
    {
        { "Url", url },
        { "Headers", headers },
        { "Body", body },
        { "ResponseStatus", responseStatus },
        { "ResponseTimeMs", responseTimeMs }
    };

        EnqueueLog(new LogEntryModel
        {
            RequestId = requestId,
            Timestamp = DateTime.Now,
            Level = LogLevelsEnums.Request.ToString(),
            Message = $"Request to {url} with response {responseStatus}",
            Service = LogServiceProvidersEnums.ServiceAPI.ToString(),
            Method = method,
            UserId = userId,
            IpAddress = ipAddress,
            AdditionalData = additionalData
        });

        return Task.CompletedTask;
    }
}

