using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dsw2025Tpi.Application.Common.Errors
{
    public static class ErrorCodes
    {
        // 1000 – Errores genéricos
        public const int ErrorDesconocido = 1000;
        public const int DatosInvalidos = 1001;

        // 2000 – Usuarios
        public const int UsuarioNoEncontrado = 2000;
        public const int UsuarioDeshabilitado = 2001;

        // 3000 – Productos
        public const int ProductoNoEncontrado = 3000;
        public const int StockInsuficiente = 3001;
        public const int ProductoConMismoSku = 3002;
        public const int SinProductosEncontrados = 3003;

        // 4000 – Pedidos
        public const int PedidoNoEncontrado = 4000;
        public const int PedidoEstadoInvalido = 4001;

        // 5000 – Autenticación / Autorización
        public const int NoAutorizado = 5000;
        public const int TokenInvalido = 5001;

        // 6000 – Base de datos
        public const int ErrorDeBaseDeDatos = 6000;
    }
}


