using MVCMASCOTAS.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MVCMASCOTAS.Services
{
    public class TiendaService
    {
        private readonly RefugioMascotasEntities db;

        public TiendaService()
        {
            db = new RefugioMascotasEntities();
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

        // Crear pedido
        public Pedidos CrearPedido(int usuarioId, string direccionEnvio, string metodoPago, decimal total)
        {
            var pedido = new Pedidos
            {
                UsuarioId = usuarioId,
                FechaPedido = DateTime.Now,
                Estado = "Pendiente",
                DireccionEnvio = direccionEnvio,
                MetodoPago = metodoPago,
                Total = total
            };

            db.Pedidos.Add(pedido);
            db.SaveChanges();

            return pedido;
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
                    pedido.FechaEntrega = DateTime.Now;
                }
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
                            p.Descripcion.Contains(termino)))
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

        public void Dispose()
        {
            db?.Dispose();
        }
    }
}