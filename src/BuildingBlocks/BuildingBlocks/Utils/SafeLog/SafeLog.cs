using BuildingBlocks.Utils.SafeLog.LogService;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace BuildingBlocks.Utils.SafeLog;


public static class SafeLog
{
    private static SafeLogService? _service;

    public static void Configure(SafeLogService service)
    {
        _service = service;
    }
    public static string GetCallerInfo(int skipFrames = 2)
    {
        var stackTrace = new StackTrace(skipFrames, true);
        var frame = stackTrace.GetFrame(0);
        var method = frame?.GetMethod();
        var methodName = method?.Name ?? "UnknownMethod";

        var declaringType = method?.DeclaringType;
        if (declaringType != null && methodName == "MoveNext")
        {
            var originalMethod = declaringType.Name; // مثل: "<SomeMethod>d__5"
            methodName = Regex.Match(originalMethod, @"<(.+?)>").Groups[1].Value;
        }

        var file = frame?.GetFileName();
        var line = frame?.GetFileLineNumber();
        var fileName = string.IsNullOrEmpty(file) ? "UnknownFile" : Path.GetFileName(file);

        return $"{fileName}:{methodName}=>(Line {line})";
    }

    public static void Info(string message)
    {
        _service?.Info(message, GetCallerInfo());
    }

    public static void Error(string message, string stackTrace)
    {
        _service?.Error(message, stackTrace, GetCallerInfo());
    }

    public static void Request(string url, Dictionary<string, string> headers, object body, int responseStatus, long responseTimeMs)
    {
        _service?.Request(url, headers, body, responseStatus, responseTimeMs, GetCallerInfo());
    }
}
