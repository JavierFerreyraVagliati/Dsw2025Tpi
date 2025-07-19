using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dsw2025Tpi.Application.Dtos
{
    public record OrderItemModel
    {
        public record Request(int? Quantity, Guid ProductId, string Name, string Description, decimal? CurrentUnitPrice);

        public record Response(int? Quantity, Guid? ProductId, string Name, string Description,decimal? CurrentUnitPrice);
    }
}
