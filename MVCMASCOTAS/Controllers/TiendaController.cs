using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using MVCMASCOTAS.Helpers;
using MVCMASCOTAS.Models;
using MVCMASCOTAS.Models.ViewModels;

namespace MVCMASCOTAS.Controllers
{
    public class TiendaController : Controller
    {
        private RefugioMascotasDBEntities db = new RefugioMascotasDBEntities();

        // GET: Tienda
        public ActionResult Index(int? categoriaId, string buscar, int page = 1)
        {
            int pageSize = 12;
            var query = db.Productos.Where(p => p.Stock > 0 && p.Activo);

            // Filtro por categoría
            if (categoriaId.HasValue && categoriaId.Value > 0)
            {
                query = query.Where(p => p.CategoriaId == categoriaId.Value);
            }

            // Búsqueda
            if (!string.IsNullOrEmpty(buscar))
            {
                query = query.Where(p => p.NombreProducto.Contains(buscar) ||
                                         p.Descripcion.Contains(buscar));
            }

            // Paginación
            int totalItems = query.Count();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var productos = query
                .OrderBy(p => p.NombreProducto)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // ViewBag para categorías
            ViewBag.Categorias = db.CategoriasProducto.OrderBy(c => c.NombreCategoria).ToList();
            ViewBag.CategoriaSeleccionada = categoriaId;
            ViewBag.Buscar = buscar;

            // ViewBag para paginación
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;

            return View(productos);
        }

        // GET: Tienda/Detalle/5
        public ActionResult Detalle(int id)
        {
            var producto = db.Productos
                .Include(p => p.CategoriasProducto)
                .FirstOrDefault(p => p.ProductoId == id && p.Activo);

            if (producto == null)
            {
                return HttpNotFound();
            }

            ViewBag.ImagenBase64 = producto.ImagenProducto != null
                ? ImageHelper.GetImageDataUri(producto.ImagenProducto)
                : null;

            return View(producto);
        }

        // POST: Tienda/AgregarAlCarrito
        [HttpPost]
        [Authorize]
        public ActionResult AgregarAlCarrito(int productoId, int cantidad = 1)
        {
            var producto = db.Productos.Find(productoId);

            if (producto == null || !producto.Activo)
            {
                return Json(new { success = false, message = "Producto no encontrado" });
            }

            if (cantidad <= 0 || cantidad > producto.Stock)
            {
                return Json(new { success = false, message = "Cantidad no válida" });
            }

            var usuario = db.Usuarios.FirstOrDefault(u => u.Email == User.Identity.Name);

            // Buscar o crear pedido pendiente
            var pedidoPendiente = db.Pedidos
                .FirstOrDefault(p => p.UsuarioId == usuario.UsuarioId && p.Estado == "Pendiente");

            if (pedidoPendiente == null)
            {
                pedidoPendiente = new Pedidos
                {
                    UsuarioId = usuario.UsuarioId,
                    FechaPedido = DateTime.Now,
                    Estado = "Pendiente",
                    MontoTotal = 0
                };
                db.Pedidos.Add(pedidoPendiente);
                db.SaveChanges();
            }

            // Verificar si ya existe el producto en el carrito
            var detalleExistente = db.PedidoDetalle
                .FirstOrDefault(pd => pd.PedidoId == pedidoPendiente.PedidoId &&
                                     pd.ProductoId == productoId);

            if (detalleExistente != null)
            {
                // Actualizar cantidad
                detalleExistente.Cantidad += cantidad;
                detalleExistente.Subtotal = detalleExistente.Cantidad * detalleExistente.PrecioUnitario;
            }
            else
            {
                // Crear nuevo detalle
                var detalle = new PedidoDetalle
                {
                    PedidoId = pedidoPendiente.PedidoId,
                    ProductoId = productoId,
                    Cantidad = cantidad,
                    PrecioUnitario = producto.Precio,
                    Subtotal = cantidad * producto.Precio
                };
                db.PedidoDetalle.Add(detalle);
            }

            // Actualizar total del pedido
            pedidoPendiente.MontoTotal = db.PedidoDetalle
                .Where(pd => pd.PedidoId == pedidoPendiente.PedidoId)
                .Sum(pd => pd.Subtotal);

            db.SaveChanges();

            // Auditoría
            AuditoriaHelper.RegistrarAccion("Agregar al Carrito", "Tienda",
                $"Producto: {producto.NombreProducto}, Cantidad: {cantidad}", usuario.UsuarioId);

            return Json(new { success = true, message = "Producto agregado al carrito" });
        }

        // GET: Tienda/Carrito
        [Authorize]
        public ActionResult Carrito()
        {
            var usuario = db.Usuarios.FirstOrDefault(u => u.Email == User.Identity.Name);

            var pedidoPendiente = db.Pedidos
                .Include(p => p.PedidoDetalle.Select(pd => pd.Productos))
                .FirstOrDefault(p => p.UsuarioId == usuario.UsuarioId && p.Estado == "Pendiente");

            if (pedidoPendiente == null)
            {
                ViewBag.CarritoVacio = true;
                return View();
            }

            return View(pedidoPendiente);
        }

        // POST: Tienda/ActualizarCantidad
        [HttpPost]
        [Authorize]
        public ActionResult ActualizarCantidad(int detalleId, int cantidad)
        {
            var detalle = db.PedidoDetalle.Find(detalleId);

            if (detalle == null)
            {
                return Json(new { success = false, message = "Detalle no encontrado" });
            }

            var producto = db.Productos.Find(detalle.ProductoId);

            if (cantidad <= 0 || cantidad > producto.Stock)
            {
                return Json(new { success = false, message = "Cantidad no válida" });
            }

            detalle.Cantidad = cantidad;
            detalle.Subtotal = cantidad * detalle.PrecioUnitario;

            // Actualizar total
            var pedido = db.Pedidos.Find(detalle.PedidoId);
            pedido.MontoTotal = db.PedidoDetalle
                .Where(pd => pd.PedidoId == pedido.PedidoId)
                .Sum(pd => pd.Subtotal);

            db.SaveChanges();

            return Json(new { success = true, nuevoSubtotal = detalle.Subtotal, nuevoTotal = pedido.MontoTotal });
        }

        // POST: Tienda/EliminarDelCarrito
        [HttpPost]
        [Authorize]
        public ActionResult EliminarDelCarrito(int detalleId)
        {
            var detalle = db.PedidoDetalle.Find(detalleId);

            if (detalle == null)
            {
                return Json(new { success = false, message = "Detalle no encontrado" });
            }

            var pedidoId = detalle.PedidoId;
            db.PedidoDetalle.Remove(detalle);

            // Actualizar total
            var pedido = db.Pedidos.Find(pedidoId);
            var detallesRestantes = db.PedidoDetalle.Where(pd => pd.PedidoId == pedidoId).ToList();

            if (detallesRestantes.Any())
            {
                pedido.MontoTotal = detallesRestantes.Sum(pd => pd.Subtotal);
            }
            else
            {
                pedido.MontoTotal = 0;
            }

            db.SaveChanges();

            return Json(new { success = true, nuevoTotal = pedido.MontoTotal });
        }

        // POST: Tienda/ConfirmarPedido
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult ConfirmarPedido(string direccionEnvio, string metodoPago, string observaciones)
        {
            var usuario = db.Usuarios.FirstOrDefault(u => u.Email == User.Identity.Name);

            var pedido = db.Pedidos
                .Include(p => p.PedidoDetalle.Select(pd => pd.Productos))
                .FirstOrDefault(p => p.UsuarioId == usuario.UsuarioId && p.Estado == "Pendiente");

            if (pedido == null || !pedido.PedidoDetalle.Any())
            {
                TempData["ErrorMessage"] = "No hay productos en el carrito";
                return RedirectToAction("Carrito");
            }

            // Verificar stock
            foreach (var detalle in pedido.PedidoDetalle.ToList())
            {
                if (detalle.Cantidad > detalle.Productos.Stock)
                {
                    TempData["ErrorMessage"] = $"Stock insuficiente para {detalle.Productos.NombreProducto}";
                    return RedirectToAction("Carrito");
                }
            }

            // Actualizar pedido
            pedido.Estado = "Confirmado";
            pedido.DireccionEnvio = direccionEnvio ?? usuario.Direccion;
            pedido.MetodoPago = metodoPago;
            pedido.Observaciones = observaciones;
            pedido.FechaConfirmacion = DateTime.Now;

            // Descontar stock
            foreach (var detalle in pedido.PedidoDetalle)
            {
                detalle.Productos.Stock -= detalle.Cantidad;
            }

            db.SaveChanges();

            // Auditoría
            AuditoriaHelper.RegistrarAccion("Confirmar Pedido", "Tienda",
                $"Pedido ID: {pedido.PedidoId}, Total: ${pedido.MontoTotal}", usuario.UsuarioId);

            TempData["SuccessMessage"] = "¡Pedido confirmado! Nos pondremos en contacto pronto.";
            return RedirectToAction("MisPedidos");
        }

        // GET: Tienda/MisPedidos
        [Authorize]
        public ActionResult MisPedidos()
        {
            var usuario = db.Usuarios.FirstOrDefault(u => u.Email == User.Identity.Name);

            var pedidos = db.Pedidos
                .Where(p => p.UsuarioId == usuario.UsuarioId && p.Estado != "Pendiente")
                .OrderByDescending(p => p.FechaPedido)
                .ToList();

            return View(pedidos);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
