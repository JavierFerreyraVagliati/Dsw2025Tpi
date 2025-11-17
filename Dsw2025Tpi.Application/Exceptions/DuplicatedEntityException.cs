using Dsw2025Tpi.Application.Common.Errors;
using System;

namespace Dsw2025Tpi.Application.Exceptions;

public class DuplicatedEntityException : AppException
{
    public DuplicatedEntityException(string message, int errorCode)
         : base(message, errorCode, 404)
    {
    }
}
