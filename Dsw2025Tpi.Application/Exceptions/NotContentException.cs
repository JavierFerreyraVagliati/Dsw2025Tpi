using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dsw2025Tpi.Application.Exceptions
{
    public class NotContentException :AppException
    {
        public NotContentException(string message, int errorCode)
      : base(message, errorCode,204)
        {
        }
    }
}
