using Dsw2025Tpi.Application.Common.Errors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dsw2025Tpi.Application.Exceptions
{
    public class ConflictException :AppException
    {
        public ConflictException(string message, int errorCode)
            : base(message, errorCode, 409)
        {
        }
    }
}
