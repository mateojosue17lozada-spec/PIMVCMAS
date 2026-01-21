using MVCMASCOTAS.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace MVCMASCOTAS.Services
{
    public class TiendaService : IDisposable
    {
        private readonly RefugioMascotasDBEntities db;

        public TiendaService()
        {
            db = new RefugioMascotasDBEntities();
        }

        // Obtener todos los productos activos
        public List<Productos> ObtenerProductosActivos()
        {
            return db.Productos
                .Where(p => p.Activo == true)
                .OrderBy(p => p.NombreProducto)
                .ToList();
        }

        // Obtener productos por categoría
        public List<Productos> ObtenerProductosPorCategoria(int categoriaId)
        {
            return db.Productos
                .Where(p => p.CategoriaId == categoriaId && p.Activo == true)
                .OrderBy(p => p.NombreProducto)
                .ToList();
        }

        // Obtener productos destacados
        public List<Productos> ObtenerProductosDestacados(int cantidad = 8)
        {
            return db.Productos
                .Where(p => p.Activo == true && p.Destacado == true)
                .OrderByDescending(p => p.FechaCreacion)
                .Take(cantidad)
                .ToList();
        }

        // Obtener producto por ID
        public Productos ObtenerProductoPorId(int productoId)
        {
            return db.Productos.Find(productoId);
        }

        // Verificar stock disponible
        public bool VerificarStockDisponible(int productoId, int cantidad)
        {
            var producto = db.Productos.Find(productoId);
            return producto != null && producto.Stock >= cantidad;
        }

        // Reducir stock
        public bool ReducirStock(int productoId, int cantidad)
        {
            var producto = db.Productos.Find(productoId);
            if (producto != null && producto.Stock >= cantidad)
            {
                producto.Stock -= cantidad;
                db.SaveChanges();
                return true;
            }
            return false;
        }

        // Aumentar stock
        public void AumentarStock(int productoId, int cantidad)
        {
            var producto = db.Productos.Find(productoId);
            if (producto != null)
            {
                producto.Stock += cantidad;
                db.SaveChanges();
            }
        }

        // Crear pedido (versión completa según estructura DB)
        public Pedidos CrearPedido(int usuarioId, string direccionEntrega, string ciudadEntrega,
                                  string telefonoEntrega, string metodoPago, decimal total,
                                  string referenciaEntrega = null, decimal descuento = 0)
        {
            var numeroPedido = GenerarNumeroPedido();

            var pedido = new Pedidos
            {
                UsuarioId = usuarioId,
                NumeroPedido = numeroPedido,
                FechaPedido = DateTime.Now,
                Estado = "Pendiente",
                EstadoPago = "Pendiente",
                DireccionEntrega = direccionEntrega, // Nombre correcto
                CiudadEntrega = ciudadEntrega,
                TelefonoEntrega = telefonoEntrega,
                ReferenciaEntrega = referenciaEntrega,
                MetodoPago = metodoPago,
                SubTotal = total + descuento, // SubTotal antes de descuento
                Descuento = descuento,
                Total = total, // Total después de descuento
                FechaEntregaEstimada = DateTime.Now.AddDays(3) // Por defecto 3 días
            };

            db.Pedidos.Add(pedido);
            db.SaveChanges();

            return pedido;
        }

        // Método simplificado para compatibilidad
        public Pedidos CrearPedidoSimple(int usuarioId, string direccionEntrega, string metodoPago, decimal total)
        {
            return CrearPedido(usuarioId, direccionEntrega, "Quito", "0999999999", metodoPago, total);
        }

        // Agregar detalle al pedido
        public void AgregarDetallePedido(int pedidoId, int productoId, int cantidad, decimal precioUnitario)
        {
            var detalle = new PedidoDetalle
            {
                PedidoId = pedidoId,
                ProductoId = productoId,
                Cantidad = cantidad,
                PrecioUnitario = precioUnitario,
                Subtotal = cantidad * precioUnitario
            };

            db.PedidoDetalle.Add(detalle);
            db.SaveChanges();

            // Reducir stock del producto
            ReducirStock(productoId, cantidad);
        }

        // Obtener pedidos de un usuario
        public List<Pedidos> ObtenerPedidosUsuario(int usuarioId)
        {
            return db.Pedidos
                .Where(p => p.UsuarioId == usuarioId)
                .OrderByDescending(p => p.FechaPedido)
                .ToList();
        }

        // Obtener detalle del pedido
        public List<PedidoDetalle> ObtenerDetallePedido(int pedidoId)
        {
            return db.PedidoDetalle
                .Where(d => d.PedidoId == pedidoId)
                .Include(d => d.Productos) // Incluir información del producto
                .ToList();
        }

        // Actualizar estado del pedido
        public void ActualizarEstadoPedido(int pedidoId, string nuevoEstado)
        {
            var pedido = db.Pedidos.Find(pedidoId);
            if (pedido != null)
            {
                pedido.Estado = nuevoEstado;
                if (nuevoEstado == "Entregado")
                {
                    pedido.FechaEntregaReal = DateTime.Now; // Nombre correcto
                    pedido.EstadoPago = "Pagado"; // Asumir que se pagó al entregar
                }
                else if (nuevoEstado == "Cancelado")
                {
                    // Restaurar stock de productos si se cancela
                    RestaurarStockPorCancelacion(pedidoId);
                }
                db.SaveChanges();
            }
        }

        // Actualizar estado de pago
        public void ActualizarEstadoPago(int pedidoId, string nuevoEstadoPago)
        {
            var pedido = db.Pedidos.Find(pedidoId);
            if (pedido != null)
            {
                pedido.EstadoPago = nuevoEstadoPago;
                db.SaveChanges();
            }
        }

        // Obtener productos con bajo stock
        public List<Productos> ObtenerProductosBajoStock()
        {
            return db.Productos
                .Where(p => p.Activo == true && p.Stock <= p.StockMinimo)
                .OrderBy(p => p.Stock)
                .ToList();
        }

        // Obtener todas las categorías
        public List<CategoriasProducto> ObtenerCategorias()
        {
            return db.CategoriasProducto
                .Where(c => c.Activo == true)
                .OrderBy(c => c.NombreCategoria)
                .ToList();
        }

        // Buscar productos
        public List<Productos> BuscarProductos(string termino)
        {
            return db.Productos
                .Where(p => p.Activo == true &&
                           (p.NombreProducto.Contains(termino) ||
                            p.Descripcion.Contains(termino) ||
                            p.SKU.Contains(termino)))
                .OrderBy(p => p.NombreProducto)
                .ToList();
        }

        // Obtener estadísticas de ventas
        public decimal ObtenerVentasMes(int mes, int anio)
        {
            return db.Pedidos
                .Where(p => p.Estado == "Entregado" &&
                           p.FechaPedido.HasValue &&
                           p.FechaPedido.Value.Month == mes &&
                           p.FechaPedido.Value.Year == anio)
                .Sum(p => (decimal?)p.Total) ?? 0;
        }

        public int ObtenerPedidosPendientes()
        {
            return db.Pedidos.Count(p => p.Estado == "Pendiente");
        }

        public int ObtenerPedidosEnProceso()
        {
            return db.Pedidos.Count(p => p.Estado == "En preparación" ||
                                         p.Estado == "Confirmado" ||
                                         p.Estado == "Enviado");
        }

        // Generar número de pedido único
        private string GenerarNumeroPedido()
        {
            var fecha = DateTime.Now.ToString("yyyyMMdd");
            var consecutivo = db.Pedidos
                .Count(p => p.FechaPedido.HasValue &&
                           DbFunctions.TruncateTime(p.FechaPedido) == DbFunctions.TruncateTime(DateTime.Now)) + 1;

            return $"PED-{fecha}-{consecutivo:D4}";
        }

        // Restaurar stock cuando se cancela un pedido
        private void RestaurarStockPorCancelacion(int pedidoId)
        {
            var detalles = db.PedidoDetalle
                .Where(d => d.PedidoId == pedidoId)
                .ToList();

            foreach (var detalle in detalles)
            {
                AumentarStock(detalle.ProductoId, detalle.Cantidad);
            }
        }

        // Obtener pedido por número de pedido
        public Pedidos ObtenerPedidoPorNumero(string numeroPedido)
        {
            return db.Pedidos
                .FirstOrDefault(p => p.NumeroPedido == numeroPedido);
        }

        // Obtener resumen de productos más vendidos
        public List<Productos> ObtenerProductosMasVendidos(int cantidad = 10)
        {
            return db.PedidoDetalle
                .Where(d => d.Pedidos.Estado == "Entregado")
                .GroupBy(d => d.ProductoId)
                .Select(g => new
                {
                    ProductoId = g.Key,
                    TotalVendido = g.Sum(d => d.Cantidad)
                })
                .OrderByDescending(x => x.TotalVendido)
                .Take(cantidad)
                .Join(db.Productos,
                      venta => venta.ProductoId,
                      producto => producto.ProductoId,
                      (venta, producto) => producto)
                .ToList();
        }

        public void Dispose()
        {
            db?.Dispose();
        }
    }
}