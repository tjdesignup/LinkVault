// src/LinkVault.Api/Middleware/GlobalExceptionHandler.cs

using LinkVault.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace LinkVault.Api.Middleware;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger = logger;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title) = MapException(exception);

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception occurred.");
        }

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = exception.Message
        };

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }

    private static (int statusCode, string title) MapException(Exception exception)
        => exception switch
    {
        // --- AUTH & ACCOUNT ---
        AccountLockedException => (StatusCodes.Status423Locked, "Account Locked"),
        InvalidLoginException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
        InvalidPasswordException => (StatusCodes.Status400BadRequest, "Bad Request"),
        InvalidTokenException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
        UserDeletedException => (StatusCodes.Status403Forbidden, "Forbidden"),
        
        // --- REGISTRATION & EMAIL ---
        EmailAlreadyConfirmedException => (StatusCodes.Status409Conflict, "Conflict"),
        EmailAlreadyInUseException => (StatusCodes.Status409Conflict, "Conflict"),
        EmailNotConfirmedException => (StatusCodes.Status403Forbidden, "Forbidden"), 
        ConfirmationTokenExpiredException => (StatusCodes.Status400BadRequest, "Token Expired"), 
        InvalidConfirmationTokenException => (StatusCodes.Status400BadRequest, "Bad Request"),
        InvalidRegisterException => (StatusCodes.Status400BadRequest, "Registration Failed"), 
        
        // --- FILES & STORAGE ---
        FileAttachmentLimitExceededException => (StatusCodes.Status400BadRequest, "Limit Exceeded"),
        FileInfectedException => (StatusCodes.Status422UnprocessableEntity, "Unprocessable Entity"),
        FileNotAvailableException => (StatusCodes.Status409Conflict, "Conflict"),
        FileTooLargeException => (StatusCodes.Status400BadRequest, "File Too Large"),
        
        // --- LINKS & LIMITS ---
        LinkLimitExceededException => (StatusCodes.Status402PaymentRequired, "Payment Required"),
        ProTierRequiredException => (StatusCodes.Status403Forbidden, "Forbidden"),
        InvalidUrlException => (StatusCodes.Status400BadRequest, "Bad Request"),
        
        // --- RESOURCES & PERMISSIONS ---
        ResourceForbiddenException => (StatusCodes.Status403Forbidden, "Forbidden"),
        ResourceNotFoundException => (StatusCodes.Status404NotFound, "Not Found"),
        
        // --- OSTATNÍ Z OBRÁZKU ---
        SamePasswordException => (StatusCodes.Status400BadRequest, "Same Password"), 
        
        // Default pro nečekané chyby (třeba ty technické jako DB connection)
        _ => (StatusCodes.Status500InternalServerError, "Internal Server Error")
    };
}