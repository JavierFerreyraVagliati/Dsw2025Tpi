﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dsw2025Tpi.Application.Dtos
{
    public record UpdateOrderStatusModel
    {
        public record Request(string NewStatus);
        public record Response(Guid OrderId, string NewStatus);
    }
}
