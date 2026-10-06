using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CineStackAPI.Helpers.Messages;

public static class MessageGenerator
{
    /// <summary>
    /// GENERATES A REQUEST ERROR MESSAGE FOR LOGGING PURPOSES
    /// </summary>
    /// <param name="className"></param>
    /// <param name="methodName"></param>
    /// <param name="requestBody"></param>
    /// <returns></returns>
    public static string RequestErrorMessage(
        string className,
        string methodName,
        object? requestBody,
        string extraInfo = ""
        )
    {
        string? requestBodyContent = requestBody?.ToString() ?? "Request body is null or empty.";
        extraInfo = string.IsNullOrEmpty(extraInfo) ? "" : "\nExtra Details: " + extraInfo;
        return $"""
            INVALID REQUEST BODY RECEIVED IN {className}.{methodName}
            REQUEST: {requestBodyContent}
            {extraInfo}
            """;
    }
}
