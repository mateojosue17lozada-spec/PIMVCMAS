using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using MVCMASCOTAS.Helpers;
using MVCMASCOTAS.Models;
using MVCMASCOTAS.Models.ViewModels;

namespace MVCMASCOTAS.Controllers
{
    public class MascotasController : Controller
    {
        private RefugioMascotasEntities db = new RefugioMascotasEntities();

        // GET: Mascotas (Vista pública)
        [AllowAnonymous]
        public ActionResult Index(string especie, string tamanio, string sexo, int page = 1)
        {
            int pageSize = 12;
            var query = db.Mascotas.Where(m => m.Estado == "Disponible para adopción" && m.Activo);

            // Filtros
            if (!string.IsNullOrEmpty(especie) && especie != "Todos")
            {
                query = query.Where(m => m.Especie == especie);
            }

            if (!string.IsNullOrEmpty(tamanio) && tamanio != "Todos")
            {
                query = query.Where(m => m.Tamanio == tamanio);
            }

            if (!string.IsNullOrEmpty(sexo) && sexo != "Todos")
            {
                query = query.Where(m => m.Sexo == sexo);
            }

            // Paginación
            int totalItems = query.Count();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var mascotas = query
                .OrderByDescending(m => m.FechaIngreso)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // ViewBag para paginación
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;

            // ViewBag para filtros
            ViewBag.FiltroEspecie = especie;
            ViewBag.FiltroTamanio = tamanio;
            ViewBag.FiltroSexo = sexo;

            return View(mascotas);
        }

        // GET: Mascotas/Detalle/5
        [AllowAnonymous]
        public ActionResult Detalle(int id)
        {
            var mascota = db.Mascotas.Find(id);

            if (mascota == null || !mascota.Activo)
            {
                return HttpNotFound();
            }

            // Obtener imágenes adicionales
            var imagenesAdicionales = db.ImagenesAdicionales
                .Where(i => i.EntidadTipo == "Mascota" && i.EntidadId == id)
                .OrderBy(i => i.Orden)
                .ToList();

            ViewBag.ImagenesAdicionales = imagenesAdicionales;

            // Obtener información veterinaria (solo si está disponible)
            if (mascota.Estado == "Disponible para adopción")
            {
                var vacunas = db.MascotaVacunas
                    .Where(mv => mv.MascotaId == id)
                    .Include(mv => mv.Vacunas)
                    .OrderByDescending(mv => mv.FechaAplicacion)
                    .ToList();

                ViewBag.Vacunas = vacunas;
            }

            // Imagen principal
            ViewBag.ImagenBase64 = mascota.ImagenPrincipal != null
                ? ImageHelper.GetImageDataUri(mascota.ImagenPrincipal)
                : null;

            // Verificar si el usuario ya solicitó adopción
            if (User.Identity.IsAuthenticated)
            {
                var usuario = db.Usuarios.FirstOrDefault(u => u.Email == User.Identity.Name);
                if (usuario != null)
                {
                    var solicitudExistente = db.SolicitudAdopcion
                        .Any(s => s.MascotaId == id && s.UsuarioId == usuario.UsuarioId &&
                                  (s.Estado == "Pendiente" || s.Estado == "En evaluación" || s.Estado == "Aprobada"));

                    ViewBag.YaSolicito = solicitudExistente;
                }
            }

            return View(mascota);
        }

        // GET: Mascotas/Crear
        [AuthorizeRoles("Administrador", "Rescatista")]
        public ActionResult Crear()
        {
            var model = new MascotaViewModel
            {
                FechaIngreso = DateTime.Now,
                Estado = "Rescatada",
                Esterilizado = false
            };

            return View(model);
        }

        // POST: Mascotas/Crear
        [HttpPost]
        [AuthorizeRoles("Administrador", "Rescatista")]
        [ValidateAntiForgeryToken]
        public ActionResult Crear(MascotaViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Validar imagen
            byte[] imagenBytes = null;
            if (model.ImagenPrincipalFile != null)
            {
                if (!ImageHelper.IsValidImage(model.ImagenPrincipalFile))
                {
                    ModelState.AddModelError("ImagenPrincipalFile", "Archivo de imagen inválido.");
                    return View(model);
                }

                if (!ImageHelper.IsFileSizeValid(model.ImagenPrincipalFile, 5))
                {
                    ModelState.AddModelError("ImagenPrincipalFile", "La imagen no debe exceder 5 MB.");
                    return View(model);
                }

                imagenBytes = ImageHelper.ConvertImageToByteArray(model.ImagenPrincipalFile);
                imagenBytes = ImageHelper.ResizeImage(imagenBytes, 800, 800);
            }

            // Obtener usuario actual
            var usuarioActual = db.Usuarios.FirstOrDefault(u => u.Email == User.Identity.Name);

            // Crear mascota
            var mascota = new Mascotas
            {
                Nombre = model.Nombre,
                Especie = model.Especie,
                Raza = model.Raza,
                Sexo = model.Sexo,
                EdadAproximada = model.EdadAproximada,
                Tamanio = model.Tamanio,
                Color = model.Color,
                Categoria = model.Categoria,
                TipoEspecial = model.TipoEspecial,
                Estado = "Rescatada",
                DescripcionGeneral = model.DescripcionGeneral,
                CaracteristicasComportamiento = model.CaracteristicasComportamiento,
                HistoriaRescate = model.HistoriaRescate,
                ImagenPrincipal = imagenBytes,
                FechaIngreso = DateTime.Now,
                RescatistaId = usuarioActual?.UsuarioId,
                Esterilizado = model.Esterilizado,
                Microchip = model.Microchip,
                Activo = true
            };

            db.Mascotas.Add(mascota);
            db.SaveChanges();

            // Auditoría
            AuditoriaHelper.RegistrarAccion("Crear Mascota", "Mascotas",
                $"Mascota creada: {mascota.Nombre} (ID: {mascota.MascotaId})", usuarioActual?.UsuarioId);

            TempData["SuccessMessage"] = $"Mascota '{mascota.Nombre}' creada exitosamente.";
            return RedirectToAction("Gestionar");
        }

        // GET: Mascotas/Gestionar
        [AuthorizeRoles("Administrador", "Rescatista", "Veterinario")]
        public ActionResult Gestionar(string estado, int page = 1)
        {
            int pageSize = 20;
            var query = db.Mascotas.Where(m => m.Activo);

            // Filtro por estado
            if (!string.IsNullOrEmpty(estado) && estado != "Todos")
            {
                query = query.Where(m => m.Estado == estado);
            }

            // Paginación
            int totalItems = query.Count();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var mascotas = query
                .OrderByDescending(m => m.FechaIngreso)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.FiltroEstado = estado;

            return View(mascotas);
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