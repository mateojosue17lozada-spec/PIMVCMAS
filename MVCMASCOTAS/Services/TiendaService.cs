using System;
using System.Collections.Generic;
using System.Linq;
using MVCMASCOTAS.Models;

namespace MVCMASCOTAS.Services
{
    /// <summary>
    /// Servicio para lógica de negocio de la tienda
    /// </summary>
    public class TiendaService
    {
        private RefugioMascotasEntities db;

        public TiendaService()
        {
            db = new RefugioMascotasEntities();
        }

        public TiendaService(RefugioMascotasEntities context)
        {
            db = context;
        }

        /// <summary>
        /// Obtiene productos disponibles con filtros
        /// </summary>
        public List<Productos> ObtenerProductosDisponibles(int? categoriaId = null, string busqueda = null)
        {
            var query = db.Productos.Where(p => p.Stock > 0 && p.Activo);

            if (categoriaId.HasValue)
            {
                query = query.Where(p => p.CategoriaId == categoriaId.Value);
            }

            if (!string.IsNullOrEmpty(busqueda))
            {
                query = query.Where(p => p.NombreProducto.Contains(busqueda) ||
                                        p.Descripcion.Contains(busqueda));
            }

            return query.OrderBy(p => p.NombreProducto).ToList();
        }

        /// <summary>
        /// Obtiene el carrito activo del usuario
        /// </summary>
        public Pedidos ObtenerCarritoActivo(int usuarioId)
        {
            return db.Pedidos.FirstOrDefault(p => p.UsuarioId == usuarioId && p.Estado == "Pendiente");
        }

        /// <summary>
        /// Crea un nuevo carrito para el usuario
        /// </summary>
        public Pedidos CrearCarrito(int usuarioId)
        {
            var carrito = new Pedidos
            {
                UsuarioId = usuarioId,
                FechaPedido = DateTime.Now,
                Estado = "Pendiente",
                MontoTotal = 0
            };

            db.Pedidos.Add(carrito);
            db.SaveChanges();

            return carrito;
        }

        /// <summary>
        /// Agrega un producto al carrito
        /// </summary>
        public bool AgregarProductoAlCarrito(int pedidoId, int productoId, int cantidad)
        {
            var producto = db.Productos.Find(productoId);

            if (producto == null || cantidad > producto.Stock || cantidad <= 0)
            {
                return false;
            }

            // Verificar si el producto ya está en el carrito
            var detalleExistente = db.PedidoDetalle
                .FirstOrDefault(pd => pd.PedidoId == pedidoId && pd.ProductoId == productoId);

            if (detalleExistente != null)
            {
                detalleExistente.Cantidad += cantidad;
                detalleExistente.Subtotal = detalleExistente.Cantidad * detalleExistente.PrecioUnitario;
            }
            else
            {
                var detalle = new PedidoDetalle
                {
                    PedidoId = pedidoId,
                    ProductoId = productoId,
                    Cantidad = cantidad,
                    PrecioUnitario = producto.Precio,
                    Subtotal = cantidad * producto.Precio
                };
                db.PedidoDetalle.Add(detalle);
            }

            ActualizarTotalPedido(pedidoId);
            db.SaveChanges();

            return true;
        }

        /// <summary>
        /// Actualiza la cantidad de un producto en el carrito
        /// </summary>
        public bool ActualizarCantidadEnCarrito(int detalleId, int nuevaCantidad)
        {
            var detalle = db.PedidoDetalle.Find(detalleId);

            if (detalle == null || nuevaCantidad <= 0)
            {
                return false;
            }

            var producto = db.Productos.Find(detalle.ProductoId);

            if (nuevaCantidad > producto.Stock)
            {
                return false;
            }

            detalle.Cantidad = nuevaCantidad;
            detalle.Subtotal = nuevaCantidad * detalle.PrecioUnitario;

            ActualizarTotalPedido(detalle.PedidoId);
            db.SaveChanges();

            return true;
        }

        /// <summary>
        /// Elimina un producto del carrito
        /// </summary>
        public bool EliminarProductoDelCarrito(int detalleId)
        {
            var detalle = db.PedidoDetalle.Find(detalleId);

            if (detalle == null)
            {
                return false;
            }

            int pedidoId = detalle.PedidoId;
            db.PedidoDetalle.Remove(detalle);

            ActualizarTotalPedido(pedidoId);
            db.SaveChanges();

            return true;
        }

        /// <summary>
        /// Actualiza el total del pedido
        /// </summary>
        private void ActualizarTotalPedido(int pedidoId)
        {
            var pedido = db.Pedidos.Find(pedidoId);

            if (pedido != null)
            {
                var detalles = db.PedidoDetalle.Where(pd => pd.PedidoId == pedidoId).ToList();
                pedido.MontoTotal = detalles.Sum(d => d.Subtotal);
            }
        }

        /// <summary>
        /// Confirma el pedido y descuenta el stock
        /// </summary>
        public bool ConfirmarPedido(int pedidoId, string direccionEnvio, string metodoPago, string observaciones)
        {
            var pedido = db.Pedidos.Find(pedidoId);

            if (pedido == null || pedido.Estado != "Pendiente")
            {
                return false;
            }

            var detalles = db.PedidoDetalle.Where(pd => pd.PedidoId == pedidoId).ToList();

            if (!detalles.Any())
            {
                return false;
            }

            // Verificar stock
            foreach (var detalle in detalles)
            {
                var producto = db.Productos.Find(detalle.ProductoId);

                if (detalle.Cantidad > producto.Stock)
                {
                    return false;
                }
            }

            // Descontar stock
            foreach (var detalle in detalles)
            {
                var producto = db.Productos.Find(detalle.ProductoId);
                producto.Stock -= detalle.Cantidad;
            }

            // Actualizar pedido
            pedido.Estado = "Confirmado";
            pedido.DireccionEnvio = direccionEnvio;
            pedido.MetodoPago = metodoPago;
            pedido.Observaciones = observaciones;
            pedido.FechaConfirmacion = DateTime.Now;

            db.SaveChanges();

            return true;
        }

        /// <summary>
        /// Obtiene los pedidos de un usuario
        /// </summary>
        public List<Pedidos> ObtenerPedidosUsuario(int usuarioId, bool soloConfirmados = true)
        {
            var query = db.Pedidos.Where(p => p.UsuarioId == usuarioId);

            if (soloConfirmados)
            {
                query = query.Where(p => p.Estado != "Pendiente");
            }

            return query.OrderByDescending(p => p.FechaPedido).ToList();
        }

        /// <summary>
        /// Calcula el total de items en el carrito
        /// </summary>
        public int ObtenerCantidadItemsCarrito(int pedidoId)
        {
            return db.PedidoDetalle
                .Where(pd => pd.PedidoId == pedidoId)
                .Sum(pd => (int?)pd.Cantidad) ?? 0;
        }

        public void Dispose()
        {
            db?.Dispose();
        }
    }
}
