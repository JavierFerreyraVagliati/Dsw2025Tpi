using Dsw2025Tpi.Application.Common.Errors;
using System;


namespace Dsw2025Tpi.Application.Exceptions
{

        public abstract class AppException : Exception
        {
            public int ErrorCode { get; }
            public int AppStatus { get; }

            protected AppException(string message, int errorCode, int appStatus)
                : base(message)
            {
                ErrorCode = errorCode;
                AppStatus = appStatus;
            }
        }
    

}
