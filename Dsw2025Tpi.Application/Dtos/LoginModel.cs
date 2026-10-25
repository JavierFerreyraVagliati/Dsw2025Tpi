using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Dsw2025Tpi.Application.Dtos;
public record LoginModel{
public record Request(string Username, string Password);
public record Response(string? Token,string? Username,string? Email,string? Role,object? Profile);

}



