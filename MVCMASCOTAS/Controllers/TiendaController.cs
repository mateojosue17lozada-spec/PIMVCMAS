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
            var query = db.Productos.Where(p => p.Stock > 0 && p.Activo == true); // CORRECTO: Activo == true

            // Filtro por categoría
            if (categoriaId.HasValue && categoriaId.Value > 0)
            {
                query = query.Where(p => p.CategoriaId == categoriaId.Value);
            }

            // Búsqueda
            if (!string.IsNullOrEmpty(buscar))
            {
                query = query.Where(p => p.NombreProducto.Contains(buscar) ||
                                         (p.Descripcion != null && p.Descripcion.Contains(buscar))); // CORREGIDO: verificar null
            }

            // Paginación
            int totalItems = query.Count();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var productos = query
                .Include(p => p.CategoriasProducto)
                .OrderBy(p => p.NombreProducto)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // ViewBag para categorías
            ViewBag.Categorias = db.CategoriasProducto.Where(c => c.Activo == true).OrderBy(c => c.NombreCategoria).ToList();
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
                .FirstOrDefault(p => p.ProductoId == id && p.Activo == true); // CORRECTO: Activo == true

            if (producto == null)
            {
                return HttpNotFound();
            }

            ViewBag.ImagenBase64 = producto.ImagenPrincipal != null // CORRECTO: es ImagenPrincipal
                ? ImageHelper.GetImageDataUri(producto.ImagenPrincipal)
                : null;

            return View(producto);
        }

        // POST: Tienda/AgregarAlCarrito
        [HttpPost]
        [Authorize]
        public ActionResult AgregarAlCarrito(int productoId, int cantidad = 1)
        {
            var producto = db.Productos.Find(productoId);

            if (producto == null || producto.Activo != true)
            {
                return Json(new { success = false, message = "Producto no encontrado" });
            }

            if (cantidad <= 0 || cantidad > producto.Stock)
            {
                return Json(new { success = false, message = "Cantidad no válida" });
            }

            var usuario = db.Usuarios.FirstOrDefault(u => u.Email == User.Identity.Name && u.Activo == true);

            if (usuario == null)
            {
                return Json(new { success = false, message = "Usuario no encontrado" });
            }

            // Buscar o crear pedido pendiente
            var pedidoPendiente = db.Pedidos
                .FirstOrDefault(p => p.UsuarioId == usuario.UsuarioId && p.Estado == "Pendiente");

            if (pedidoPendiente == null)
            {
                pedidoPendiente = new Pedidos
                {
                    UsuarioId = usuario.UsuarioId,
                    NumeroPedido = GenerarNumeroPedido(),
                    FechaPedido = DateTime.Now,
                    Estado = "Pendiente",
                    SubTotal = 0,
                    Descuento = 0,
                    Total = 0, // CORRECTO: es Total, no MontoTotal
                    EstadoPago = "Pendiente",
                    DireccionEntrega = usuario.Direccion ?? "",
                    CiudadEntrega = usuario.Ciudad ?? "Quito",
                    TelefonoEntrega = usuario.Telefono ?? ""
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
            pedidoPendiente.SubTotal = db.PedidoDetalle
                .Where(pd => pd.PedidoId == pedidoPendiente.PedidoId)
                .Sum(pd => (decimal?)pd.Subtotal) ?? 0;

            pedidoPendiente.Total = pedidoPendiente.SubTotal - (pedidoPendiente.Descuento ?? 0);

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
            var usuario = db.Usuarios.FirstOrDefault(u => u.Email == User.Identity.Name && u.Activo == true);

            if (usuario == null)
            {
                return RedirectToAction("Logout", "Account");
            }

            var pedidoPendiente = db.Pedidos
                .Include(p => p.PedidoDetalle.Select(pd => pd.Productos))
                .FirstOrDefault(p => p.UsuarioId == usuario.UsuarioId && p.Estado == "Pendiente");

            if (pedidoPendiente == null || !pedidoPendiente.PedidoDetalle.Any())
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
            pedido.SubTotal = db.PedidoDetalle
                .Where(pd => pd.PedidoId == pedido.PedidoId)
                .Sum(pd => (decimal?)pd.Subtotal) ?? 0;

            pedido.Total = pedido.SubTotal - (pedido.Descuento ?? 0);

            db.SaveChanges();

            return Json(new
            {
                success = true,
                nuevoSubtotal = detalle.Subtotal,
                nuevoSubTotal = pedido.SubTotal,
                nuevoTotal = pedido.Total
            });
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
                pedido.SubTotal = detallesRestantes.Sum(pd => pd.Subtotal);
                pedido.Total = pedido.SubTotal - (pedido.Descuento ?? 0);
            }
            else
            {
                pedido.SubTotal = 0;
                pedido.Total = 0;
            }

            db.SaveChanges();

            return Json(new { success = true, nuevoTotal = pedido.Total });
        }

        // GET: Tienda/Checkout
        [Authorize]
        public ActionResult Checkout()
        {
            var usuario = db.Usuarios.FirstOrDefault(u => u.Email == User.Identity.Name && u.Activo == true);

            if (usuario == null)
            {
                return RedirectToAction("Logout", "Account");
            }

            var pedido = db.Pedidos
                .Include(p => p.PedidoDetalle.Select(pd => pd.Productos))
                .FirstOrDefault(p => p.UsuarioId == usuario.UsuarioId && p.Estado == "Pendiente");

            if (pedido == null || !pedido.PedidoDetalle.Any())
            {
                TempData["ErrorMessage"] = "No hay productos en el carrito";
                return RedirectToAction("Carrito");
            }

            // Prellenar dirección
            ViewBag.Direccion = usuario.Direccion ?? "";
            ViewBag.Ciudad = usuario.Ciudad ?? "Quito";
            ViewBag.Telefono = usuario.Telefono ?? "";

            ViewBag.MetodosPago = new SelectList(new[] {
                "Efectivo", "Transferencia Bancaria", "Tarjeta de Crédito",
                "Tarjeta de Débito", "Cheque"
            });

            return View(pedido);
        }

        // POST: Tienda/ConfirmarPedido
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult ConfirmarPedido(string direccionEntrega, string ciudadEntrega,
            string telefonoEntrega, string referenciaEntrega, string metodoPago, string observaciones)
        {
            var usuario = db.Usuarios.FirstOrDefault(u => u.Email == User.Identity.Name && u.Activo == true);

            if (usuario == null)
            {
                return RedirectToAction("Logout", "Account");
            }

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

            // Validar datos de envío
            if (string.IsNullOrEmpty(direccionEntrega))
            {
                ModelState.AddModelError("direccionEntrega", "La dirección de entrega es requerida");
                return RedirectToAction("Checkout");
            }

            if (string.IsNullOrEmpty(ciudadEntrega))
            {
                ModelState.AddModelError("ciudadEntrega", "La ciudad de entrega es requerida");
                return RedirectToAction("Checkout");
            }

            if (string.IsNullOrEmpty(telefonoEntrega))
            {
                ModelState.AddModelError("telefonoEntrega", "El teléfono de entrega es requerido");
                return RedirectToAction("Checkout");
            }

            // Actualizar pedido - CORREGIDO según estructura real
            pedido.Estado = "Confirmado";
            pedido.DireccionEntrega = direccionEntrega;
            pedido.CiudadEntrega = ciudadEntrega;
            pedido.TelefonoEntrega = telefonoEntrega;
            pedido.ReferenciaEntrega = referenciaEntrega;
            pedido.MetodoPago = metodoPago;
            pedido.EstadoPago = "Pendiente";
            pedido.Observaciones = observaciones;
            pedido.FechaEntregaEstimada = DateTime.Now.AddDays(3);

            // Descontar stock
            foreach (var detalle in pedido.PedidoDetalle)
            {
                detalle.Productos.Stock -= detalle.Cantidad;
            }

            db.SaveChanges();

            // Auditoría
            AuditoriaHelper.RegistrarAccion("Confirmar Pedido", "Tienda",
                $"Pedido ID: {pedido.PedidoId}, Total: ${pedido.Total}", usuario.UsuarioId);

            // Enviar email de confirmación
            string subject = $"Confirmación de Pedido #{pedido.NumeroPedido}";
            string body = $@"
                <h2>¡Hola {usuario.NombreCompleto}!</h2>
                <p>Tu pedido ha sido confirmado exitosamente.</p>
                
                <h3>Detalles del pedido:</h3>
                <ul>
                    <li><strong>Número de pedido:</strong> {pedido.NumeroPedido}</li>
                    <li><strong>Fecha:</strong> {pedido.FechaPedido:dd/MM/yyyy}</li>
                    <li><strong>Total:</strong> ${pedido.Total:N2}</li>
                    <li><strong>Método de pago:</strong> {pedido.MetodoPago}</li>
                    <li><strong>Estado:</strong> {pedido.Estado}</li>
                </ul>
                
                <h3>Dirección de entrega:</h3>
                <p>{pedido.DireccionEntrega}<br/>
                {pedido.CiudadEntrega}<br/>
                Teléfono: {pedido.TelefonoEntrega}</p>
                
                <h3>Productos:</h3>
                <table border='1' cellpadding='5' cellspacing='0' style='border-collapse: collapse; width: 100%;'>
                    <tr>
                        <th>Producto</th>
                        <th>Cantidad</th>
                        <th>Precio Unitario</th>
                        <th>Subtotal</th>
                    </tr>";

            foreach (var detalle in pedido.PedidoDetalle)
            {
                body += $@"
                    <tr>
                        <td>{detalle.Productos.NombreProducto}</td>
                        <td>{detalle.Cantidad}</td>
                        <td>${detalle.PrecioUnitario:N2}</td>
                        <td>${detalle.Subtotal:N2}</td>
                    </tr>";
            }

            body += $@"
                </table>
                
                <p><strong>Subtotal:</strong> ${pedido.SubTotal:N2}</p>
                {(pedido.Descuento > 0 ? $"<p><strong>Descuento:</strong> ${pedido.Descuento:N2}</p>" : "")}
                <p><strong>Total:</strong> ${pedido.Total:N2}</p>
                
                <p>Te contactaremos pronto para coordinar la entrega.</p>
                <br/>
                <p>¡Gracias por tu compra!<br/>Equipo de la Tienda del Refugio</p>
            ";

            _ = EmailHelper.SendEmailAsync(usuario.Email, subject, body);

            TempData["SuccessMessage"] = $"¡Pedido confirmado! Número de pedido: {pedido.NumeroPedido}";
            return RedirectToAction("MisPedidos");
        }

        // GET: Tienda/MisPedidos
        [Authorize]
        public ActionResult MisPedidos()
        {
            var usuario = db.Usuarios.FirstOrDefault(u => u.Email == User.Identity.Name && u.Activo == true);

            if (usuario == null)
            {
                return RedirectToAction("Logout", "Account");
            }

            var pedidos = db.Pedidos
                .Include(p => p.PedidoDetalle.Select(pd => pd.Productos))
                .Where(p => p.UsuarioId == usuario.UsuarioId && p.Estado != "Pendiente")
                .OrderByDescending(p => p.FechaPedido)
                .ToList();

            return View(pedidos);
        }

        // GET: Tienda/DetallePedido/5
        [Authorize]
        public ActionResult DetallePedido(int id)
        {
            var usuario = db.Usuarios.FirstOrDefault(u => u.Email == User.Identity.Name && u.Activo == true);

            if (usuario == null)
            {
                return RedirectToAction("Logout", "Account");
            }

            var pedido = db.Pedidos
                .Include(p => p.PedidoDetalle.Select(pd => pd.Productos))
                .FirstOrDefault(p => p.PedidoId == id && p.UsuarioId == usuario.UsuarioId);

            if (pedido == null)
            {
                return HttpNotFound();
            }

            return View(pedido);
        }

        // POST: Tienda/CancelarPedido/5
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult CancelarPedido(int id)
        {
            var usuario = db.Usuarios.FirstOrDefault(u => u.Email == User.Identity.Name && u.Activo == true);

            if (usuario == null)
            {
                return RedirectToAction("Logout", "Account");
            }

            var pedido = db.Pedidos
                .Include(p => p.PedidoDetalle.Select(pd => pd.Productos))
                .FirstOrDefault(p => p.PedidoId == id && p.UsuarioId == usuario.UsuarioId);

            if (pedido == null)
            {
                return HttpNotFound();
            }

            // Solo se pueden cancelar pedidos en estado "Confirmado"
            if (pedido.Estado != "Confirmado")
            {
                TempData["ErrorMessage"] = "Solo se pueden cancelar pedidos en estado 'Confirmado'";
                return RedirectToAction("DetallePedido", new { id = id });
            }

            // Devolver stock
            foreach (var detalle in pedido.PedidoDetalle)
            {
                detalle.Productos.Stock += detalle.Cantidad;
            }

            pedido.Estado = "Cancelado";
            db.SaveChanges();

            // Auditoría
            AuditoriaHelper.RegistrarAccion("Cancelar Pedido", "Tienda",
                $"Pedido ID: {id} cancelado", usuario.UsuarioId);

            TempData["SuccessMessage"] = "Pedido cancelado exitosamente";
            return RedirectToAction("MisPedidos");
        }

        // Método auxiliar para generar número de pedido
        private string GenerarNumeroPedido()
        {
            string fecha = DateTime.Now.ToString("yyyyMMdd");
            int secuencia = 1;

            var ultimoPedido = db.Pedidos
                .Where(p => p.NumeroPedido.StartsWith($"PED-{fecha}"))
                .OrderByDescending(p => p.NumeroPedido)
                .FirstOrDefault();

            if (ultimoPedido != null)
            {
                if (ultimoPedido.NumeroPedido.Length >= 13)
                {
                    string secuenciaStr = ultimoPedido.NumeroPedido.Substring(ultimoPedido.NumeroPedido.Length - 3);
                    int.TryParse(secuenciaStr, out secuencia);
                    secuencia++;
                }
            }

            return $"PED-{fecha}-{secuencia:D3}";
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