using Dsw2025Tpi.Application.Common.Errors;
using System;


namespace Dsw2025Tpi.Application.Exceptions;

public class EntityNotFoundException : AppException
{

    
        public EntityNotFoundException(string message, int errorCode)
            : base(message, errorCode, 404) 
        {
        }
    


}
