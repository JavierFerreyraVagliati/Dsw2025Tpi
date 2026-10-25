using Dsw2025Tpi.Application.Common.Errors;
using System;

namespace Dsw2025Tpi.Application.Exceptions;

public class InsufficientStockException : AppException
{
    public InsufficientStockException(string message, int errorCode)
        : base(message, errorCode, 400) 
    {
    }
}
