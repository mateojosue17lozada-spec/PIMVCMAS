using MVCMASCOTAS.Helpers;
using MVCMASCOTAS.Models;
using MVCMASCOTAS.Models.ViewModels;
using MVCMASCOTAS.Services;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MVCMASCOTAS.Controllers
{
    [Authorize]
    public class MascotasController : Controller
    {
        private readonly RefugioMascotasDBEntities db;
        private readonly MascotaService mascotaService;

        public MascotasController()
        {
            db = new RefugioMascotasDBEntities();
            mascotaService = new MascotaService();
        }

        #region VISTAS PÚBLICAS - CORREGIDO

        [AllowAnonymous]
        public ActionResult Index(string especie = "Todos", string tamanio = "Todos", string sexo = "Todos", string buscar = "", int page = 1)
        {
            try
            {
                var query = db.Mascotas.Where(m => m.Estado == "Disponible para adopción" && (m.Activo ?? false));

                if (especie != "Todos" && !string.IsNullOrEmpty(especie))
                    query = query.Where(m => m.Especie == especie);

                if (tamanio != "Todos" && !string.IsNullOrEmpty(tamanio))
                    query = query.Where(m => m.Tamanio == tamanio);

                if (sexo != "Todos" && !string.IsNullOrEmpty(sexo))
                {
                    string sexoCodigo = sexo == "Macho" ? "M" : "F";
                    query = query.Where(m => m.Sexo == sexoCodigo);
                }

                if (!string.IsNullOrWhiteSpace(buscar))
                {
                    string busquedaSegura = SanitizarBusqueda(buscar);
                    query = query.Where(m =>
                        m.Nombre.Contains(busquedaSegura) ||
                        (m.Raza != null && m.Raza.Contains(busquedaSegura)) ||
                        (m.DescripcionGeneral != null && m.DescripcionGeneral.Contains(busquedaSegura))
                    );
                }

                int pageSize = 12;
                int totalItems = query.Count();
                int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

                if (page < 1) page = 1;
                if (totalPages > 0 && page > totalPages) page = totalPages;

                var mascotasPaginadas = query
                    .OrderByDescending(m => m.FechaIngreso)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                ViewBag.TotalPerros = db.Mascotas.Count(m => m.Estado == "Disponible para adopción" && (m.Activo ?? false) && m.Especie == "Perro");
                ViewBag.TotalGatos = db.Mascotas.Count(m => m.Estado == "Disponible para adopción" && (m.Activo ?? false) && m.Especie == "Gato");

                ViewBag.FiltroEspecie = especie;
                ViewBag.FiltroTamanio = tamanio;
                ViewBag.FiltroSexo = sexo;
                ViewBag.FiltroBuscar = buscar;
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                ViewBag.TotalItems = totalItems;

                return View(mascotasPaginadas);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en Index: {ex.Message}");
                AuditoriaHelper.RegistrarError("Mascotas", "Error al cargar lista", ex, null);
                TempData["ErrorMessage"] = "Error al cargar las mascotas";
                return View(new List<Mascotas>());
            }
        }

        [AllowAnonymous]
        public ActionResult Detalle(int? id)
        {
            if (id == null)
            {
                TempData["ErrorMessage"] = "ID no especificado";
                return RedirectToAction("Index");
            }

            try
            {
                var mascota = mascotaService.ObtenerMascotaPorId(id.Value);

                if (mascota == null || !(mascota.Activo ?? false))
                {
                    TempData["ErrorMessage"] = "Mascota no encontrada";
                    return RedirectToAction("Index");
                }

                ViewBag.SexoTexto = mascota.Sexo == "M" ? "Macho" : "Hembra";

                if (mascota.ImagenPrincipal != null && mascota.ImagenPrincipal.Length > 0)
                {
                    ViewBag.ImagenBase64 = ImageHelper.GetImageDataUri(mascota.ImagenPrincipal);
                }

                // ✅ CORRECCIÓN: JOIN con tabla Vacunas para obtener el nombre
                var vacunas = db.MascotaVacunas
                    .Where(mv => mv.MascotaId == id)
                    .Include(mv => mv.Vacunas) // ← IMPORTANTE: Include para hacer JOIN
                    .OrderByDescending(mv => mv.FechaAplicacion)
                    .ToList();
                ViewBag.Vacunas = vacunas;

                // ✅ CORRECCIÓN: Cargar historial médico
                var historialMedico = db.HistorialMedico
                    .Where(h => h.MascotaId == id)
                    .OrderByDescending(h => h.FechaConsulta)
                    .Take(5) // Solo los últimos 5
                    .ToList();
                ViewBag.HistorialMedico = historialMedico;

                if (mascota.VeterinarioAsignado.HasValue)
                {
                    var vet = db.Usuarios.Find(mascota.VeterinarioAsignado.Value);
                    ViewBag.VeterinarioNombre = vet?.NombreCompleto;
                }

                if (mascota.RescatistaId.HasValue)
                {
                    var rescatista = db.Usuarios.Find(mascota.RescatistaId.Value);
                    ViewBag.RescatistaNombre = rescatista?.NombreCompleto;
                }

                if (User.Identity.IsAuthenticated)
                {
                    var userId = UserHelper.GetCurrentUserId();
                    if (userId.HasValue)
                    {
                        var yaSolicito = db.SolicitudAdopcion
                            .Any(s => s.MascotaId == id && s.UsuarioId == userId.Value &&
                                    (s.Estado == "Pendiente" || s.Estado == "En evaluación" || s.Estado == "Aprobada"));
                        ViewBag.YaSolicito = yaSolicito;
                    }
                }

                var historialEstados = mascotaService.ObtenerHistorialEstados(id.Value);
                ViewBag.HistorialEstados = historialEstados;

                var similares = db.Mascotas
                    .Where(m => m.MascotaId != id &&
                               m.Especie == mascota.Especie &&
                               m.Estado == "Disponible para adopción" &&
                               (m.Activo ?? false))
                    .OrderByDescending(m => m.FechaIngreso)
                    .Take(4)
                    .ToList();

                ViewBag.MascotasSimilares = similares;

                return View(mascota);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en Detalle: {ex.Message}");
                AuditoriaHelper.RegistrarError("Mascotas", $"Error en detalle ID:{id}", ex, null);
                TempData["ErrorMessage"] = "Error al cargar detalles";
                return RedirectToAction("Index");
            }
        }

        #endregion

        #region GESTIÓN ADMINISTRATIVA - CORREGIDO

        [AuthorizeRoles("Administrador", "Veterinario", "Rescatista")]
        public ActionResult Gestionar(string estado = "", string especie = "", string buscar = "", int page = 1)
        {
            try
            {
                int pageSize = 20;
                var query = db.Mascotas.Where(m => m.Activo == true);

                if (!string.IsNullOrEmpty(estado) && estado != "Todos")
                    query = query.Where(m => m.Estado == estado);

                if (!string.IsNullOrEmpty(especie) && especie != "Todos")
                    query = query.Where(m => m.Especie == especie);

                if (!string.IsNullOrEmpty(buscar))
                {
                    string busquedaSegura = SanitizarBusqueda(buscar);
                    query = query.Where(m => m.Nombre.Contains(busquedaSegura) ||
                                            (m.Microchip != null && m.Microchip.Contains(busquedaSegura)));
                }

                ViewBag.TotalMascotas = query.Count();
                ViewBag.MascotasDisponibles = query.Count(m => m.Estado == "Disponible para adopción");
                ViewBag.MascotasEnTratamiento = query.Count(m => m.Estado == "En tratamiento");
                ViewBag.MascotasAdoptadas = query.Count(m => m.Estado == "Adoptada");

                int totalItems = query.Count();
                int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                var mascotas = query
                    .OrderByDescending(m => m.FechaIngreso)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                ViewBag.FiltroEstado = estado;
                ViewBag.FiltroEspecie = especie;
                ViewBag.FiltroBuscar = buscar;
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                ViewBag.TotalItems = totalItems;

                return View(mascotas);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en Gestionar: {ex.Message}");
                AuditoriaHelper.RegistrarError("Mascotas", "Error en Gestionar", ex, UserHelper.GetCurrentUserId());
                TempData["ErrorMessage"] = "Error al cargar mascotas";
                return View(new List<Mascotas>());
            }
        }

        [AuthorizeRoles("Administrador", "Veterinario", "Rescatista")]
        public ActionResult Listado(string tipo = "Disponibles")
        {
            try
            {
                IQueryable<Mascotas> query = db.Mascotas;

                if (tipo == "Adoptadas")
                {
                    query = query.Where(m => m.Estado == "Adoptada");
                    ViewBag.Titulo = "Mascotas Adoptadas";
                }
                else
                {
                    query = query.Where(m => m.Estado == "Disponible para adopción");
                    ViewBag.Titulo = "Mascotas Disponibles";
                }

                ViewBag.Tipo = tipo;
                ViewBag.TotalAdoptadas = db.Mascotas.Count(m => m.Estado == "Adoptada");
                ViewBag.TotalDisponibles = db.Mascotas.Count(m => m.Estado == "Disponible para adopción");
                ViewBag.TotalEnTratamiento = db.Mascotas.Count(m => m.Estado == "En tratamiento");

                var mascotas = query.OrderByDescending(m => m.FechaIngreso).ToList();

                return View(mascotas);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en Listado: {ex.Message}");
                AuditoriaHelper.RegistrarError("Mascotas", "Error en Listado", ex, UserHelper.GetCurrentUserId());
                TempData["ErrorMessage"] = "Error al cargar listado";
                return View(new List<Mascotas>());
            }
        }

        #endregion

        #region CREAR MASCOTA - MODIFICADO CON EL NUEVO MÉTODO

        private void CargarListasParaViewModel(MascotaViewModel model)
        {
            try
            {
                // Veterinarios
                var veterinarios = db.Usuarios
                    .Where(u => u.UsuariosRoles.Any(ur => ur.RolId == 2) && (u.Activo == null || u.Activo == true))
                    .OrderBy(u => u.NombreCompleto)
                    .Select(u => new SelectListItem
                    {
                        Value = u.UsuarioId.ToString(),
                        Text = u.NombreCompleto ?? "Sin nombre"
                    })
                    .ToList();

                model.VeterinariosList = new SelectList(veterinarios, "Value", "Text");
                model.Veterinarios = veterinarios;

                // Rescatistas
                var rescatistas = db.Usuarios
                    .Where(u => u.UsuariosRoles.Any(ur => ur.RolId == 3) && (u.Activo == null || u.Activo == true))
                    .OrderBy(u => u.NombreCompleto)
                    .Select(u => new SelectListItem
                    {
                        Value = u.UsuarioId.ToString(),
                        Text = u.NombreCompleto ?? "Sin nombre"
                    })
                    .ToList();

                model.RescatistasList = new SelectList(rescatistas, "Value", "Text");
                model.Rescatistas = rescatistas;

                // Estados
                var estados = new List<SelectListItem>
                {
                    new SelectListItem { Value = "Rescatada", Text = "Rescatada" },
                    new SelectListItem { Value = "En revisión veterinaria", Text = "En revisión veterinaria" },
                    new SelectListItem { Value = "En tratamiento", Text = "En tratamiento" },
                    new SelectListItem { Value = "En cuarentena", Text = "En cuarentena" },
                    new SelectListItem { Value = "Disponible para adopción", Text = "Disponible para adopción" },
                    new SelectListItem { Value = "Adoptada", Text = "Adoptada" },
                    new SelectListItem { Value = "Fallecida", Text = "Fallecida" },
                    new SelectListItem { Value = "Archivada", Text = "Archivada" }
                };
                model.EstadosList = new SelectList(estados, "Value", "Text");
                model.Estados = estados;

                // Tamaños
                var tamanios = new List<SelectListItem>
                {
                    new SelectListItem { Value = "Pequeño", Text = "Pequeño" },
                    new SelectListItem { Value = "Mediano", Text = "Mediano" },
                    new SelectListItem { Value = "Grande", Text = "Grande" },
                    new SelectListItem { Value = "Muy Grande", Text = "Muy Grande" }
                };
                model.TamaniosList = new SelectList(tamanios, "Value", "Text");
                model.Tamanios = tamanios;

                // Categorías
                var categorias = new List<SelectListItem>
                {
                    new SelectListItem { Value = "Normal", Text = "Normal" },
                    new SelectListItem { Value = "Especial", Text = "Especial" },
                    new SelectListItem { Value = "Urgente", Text = "Urgente" },
                    new SelectListItem { Value = "Discapacitada", Text = "Discapacitada" },
                    new SelectListItem { Value = "Senior", Text = "Senior" }
                };
                model.CategoriasList = new SelectList(categorias, "Value", "Text");
                model.Categorias = categorias;

                // Especies
                var especies = new List<SelectListItem>
                {
                    new SelectListItem { Value = "Perro", Text = "Perro" },
                    new SelectListItem { Value = "Gato", Text = "Gato" }
                };
                model.EspeciesList = new SelectList(especies, "Value", "Text");
                model.Especies = especies;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en CargarListasParaViewModel: {ex.Message}");
                // Inicializar listas vacías para evitar null reference
                model.VeterinariosList = new SelectList(new List<SelectListItem>());
                model.RescatistasList = new SelectList(new List<SelectListItem>());
                model.EstadosList = new SelectList(new List<SelectListItem>());
                model.TamaniosList = new SelectList(new List<SelectListItem>());
                model.CategoriasList = new SelectList(new List<SelectListItem>());
                model.EspeciesList = new SelectList(new List<SelectListItem>());

                model.Veterinarios = new List<SelectListItem>();
                model.Rescatistas = new List<SelectListItem>();
                model.Estados = new List<SelectListItem>();
                model.Tamanios = new List<SelectListItem>();
                model.Categorias = new List<SelectListItem>();
                model.Especies = new List<SelectListItem>();
            }
        }

        [AuthorizeRoles("Administrador", "Veterinario", "Rescatista")]
        public ActionResult Crear()
        {
            try
            {
                var model = new MascotaViewModel();
                CargarListasParaViewModel(model);
                return View(model);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en Crear GET: {ex.Message}");
                AuditoriaHelper.RegistrarError("Mascotas", "Error en Crear GET", ex, UserHelper.GetCurrentUserId());
                TempData["ErrorMessage"] = "Error al cargar formulario";
                return RedirectToAction("Gestionar");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRoles("Administrador", "Veterinario", "Rescatista")]
        public ActionResult Crear(MascotaViewModel model, HttpPostedFileBase ImagenPrincipalFile)
        {
            // Cargar listas si el modelo no es válido
            if (!ModelState.IsValid)
            {
                CargarListasParaViewModel(model);
                return View(model);
            }

            try
            {
                string sexoCodigo = model.Sexo == "Macho" ? "M" : "F";

                if (sexoCodigo != "M" && sexoCodigo != "F")
                {
                    ModelState.AddModelError("Sexo", "Sexo inválido.");
                    CargarListasParaViewModel(model);
                    return View(model);
                }

                if (model.VeterinarioAsignado.HasValue && model.VeterinarioAsignado > 0)
                {
                    var veterinarioExiste = db.Usuarios.Any(u => u.UsuarioId == model.VeterinarioAsignado);
                    if (!veterinarioExiste)
                    {
                        ModelState.AddModelError("VeterinarioAsignado", "Veterinario no encontrado");
                        CargarListasParaViewModel(model);
                        return View(model);
                    }
                }

                if (model.RescatistaId.HasValue && model.RescatistaId > 0)
                {
                    var rescatistaExiste = db.Usuarios.Any(u => u.UsuarioId == model.RescatistaId);
                    if (!rescatistaExiste)
                    {
                        ModelState.AddModelError("RescatistaId", "Rescatista no encontrado");
                        CargarListasParaViewModel(model);
                        return View(model);
                    }
                }

                byte[] imagenBytes = null;
                if (ImagenPrincipalFile != null && ImagenPrincipalFile.ContentLength > 0)
                {
                    var validacion = ValidarImagenSegura(ImagenPrincipalFile);
                    if (!validacion.EsValida)
                    {
                        ModelState.AddModelError("ImagenPrincipalFile", validacion.MensajeError);
                        CargarListasParaViewModel(model);
                        return View(model);
                    }

                    imagenBytes = ImageHelper.ConvertImageToByteArray(ImagenPrincipalFile);
                    imagenBytes = ImageHelper.ResizeImage(imagenBytes, 800, 600);
                }

                var userId = UserHelper.GetCurrentUserId() ?? 1;

                var mascota = new Mascotas
                {
                    Nombre = model.Nombre?.Trim(),
                    Especie = model.Especie,
                    Raza = model.Raza?.Trim() ?? "Mestizo",
                    Sexo = sexoCodigo,
                    EdadAproximada = model.EdadAproximada,
                    Tamanio = model.Tamanio,
                    Color = model.Color?.Trim(),
                    Categoria = model.Categoria ?? "Normal",
                    TipoEspecial = model.TipoEspecial?.Trim(),
                    Estado = model.Estado ?? "Rescatada",
                    DescripcionGeneral = model.DescripcionGeneral?.Trim(),
                    CaracteristicasComportamiento = model.CaracteristicasComportamiento?.Trim(),
                    HistoriaRescate = model.HistoriaRescate?.Trim(),
                    ImagenPrincipal = imagenBytes,
                    FechaIngreso = model.FechaIngreso,
                    Esterilizado = model.Esterilizado,
                    Microchip = model.Microchip?.Trim(),
                    VeterinarioAsignado = model.VeterinarioAsignado,
                    RescatistaId = model.RescatistaId,
                    Activo = true
                };

                mascotaService.CrearMascota(mascota, userId);

                TempData["SuccessMessage"] = $"Mascota '{mascota.Nombre}' creada exitosamente";
                return RedirectToAction("Detalle", new { id = mascota.MascotaId });
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                CargarListasParaViewModel(model);
                return View(model);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en Crear POST: {ex.Message}");
                AuditoriaHelper.RegistrarError("Mascotas", "Error al crear mascota", ex, UserHelper.GetCurrentUserId());
                ModelState.AddModelError("", "Error al crear mascota. Intente nuevamente.");
                CargarListasParaViewModel(model);
                return View(model);
            }
        }

        #endregion

        #region EDITAR MASCOTA - MODIFICADO PARA USAR EL NUEVO MÉTODO

        [AuthorizeRoles("Administrador", "Veterinario", "Rescatista")]
        public ActionResult Editar(int? id)
        {
            if (id == null)
            {
                TempData["ErrorMessage"] = "ID no especificado";
                return RedirectToAction("Gestionar");
            }

            try
            {
                var mascota = mascotaService.ObtenerMascotaPorId(id.Value);

                if (mascota == null)
                {
                    TempData["ErrorMessage"] = "Mascota no encontrada";
                    return RedirectToAction("Gestionar");
                }

                string sexoTexto = (mascota.Sexo == "M") ? "Macho" : "Hembra";

                var model = new MascotaViewModel
                {
                    MascotaId = mascota.MascotaId,
                    Nombre = mascota.Nombre,
                    Especie = mascota.Especie,
                    Raza = mascota.Raza,
                    Sexo = sexoTexto,
                    EdadAproximada = mascota.EdadAproximada,
                    Tamanio = mascota.Tamanio,
                    Color = mascota.Color,
                    Categoria = mascota.Categoria,
                    TipoEspecial = mascota.TipoEspecial,
                    DescripcionGeneral = mascota.DescripcionGeneral,
                    CaracteristicasComportamiento = mascota.CaracteristicasComportamiento,
                    HistoriaRescate = mascota.HistoriaRescate,
                    Esterilizado = mascota.Esterilizado ?? false,
                    Microchip = mascota.Microchip,
                    Estado = mascota.Estado,
                    FechaIngreso = mascota.FechaIngreso ?? DateTime.Now,
                    ImagenPrincipal = mascota.ImagenPrincipal,
                    VeterinarioAsignado = mascota.VeterinarioAsignado,
                    RescatistaId = mascota.RescatistaId
                };

                if (mascota.ImagenPrincipal != null && mascota.ImagenPrincipal.Length > 0)
                {
                    model.ImagenBase64 = ImageHelper.GetImageDataUri(mascota.ImagenPrincipal);
                }

                CargarListasParaViewModel(model);
                return View(model);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en Editar GET: {ex.Message}");
                AuditoriaHelper.RegistrarError("Mascotas", $"Error en Editar GET ID:{id}", ex, UserHelper.GetCurrentUserId());
                TempData["ErrorMessage"] = "Error al cargar formulario";
                return RedirectToAction("Gestionar");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRoles("Administrador", "Veterinario", "Rescatista")]
        public ActionResult Editar(MascotaViewModel model, HttpPostedFileBase ImagenPrincipalFile)
        {
            if (!ModelState.IsValid)
            {
                CargarListasParaViewModel(model);
                return View(model);
            }

            try
            {
                var mascota = mascotaService.ObtenerMascotaPorId(model.MascotaId);

                if (mascota == null)
                {
                    TempData["ErrorMessage"] = "Mascota no encontrada";
                    return RedirectToAction("Gestionar");
                }

                string sexoCodigo = model.Sexo == "Macho" ? "M" : "F";

                if (sexoCodigo != "M" && sexoCodigo != "F")
                {
                    ModelState.AddModelError("Sexo", "Sexo inválido.");
                    CargarListasParaViewModel(model);
                    return View(model);
                }

                mascota.Nombre = model.Nombre?.Trim();
                mascota.Especie = model.Especie;
                mascota.Raza = model.Raza?.Trim();
                mascota.Sexo = sexoCodigo;
                mascota.EdadAproximada = model.EdadAproximada;
                mascota.Tamanio = model.Tamanio;
                mascota.Color = model.Color?.Trim();
                mascota.Categoria = model.Categoria;
                mascota.TipoEspecial = model.TipoEspecial?.Trim();
                mascota.DescripcionGeneral = model.DescripcionGeneral?.Trim();
                mascota.CaracteristicasComportamiento = model.CaracteristicasComportamiento?.Trim();
                mascota.HistoriaRescate = model.HistoriaRescate?.Trim();
                mascota.Esterilizado = model.Esterilizado;
                mascota.Microchip = model.Microchip?.Trim();
                mascota.Estado = model.Estado;
                mascota.VeterinarioAsignado = model.VeterinarioAsignado;
                mascota.RescatistaId = model.RescatistaId;

                if (ImagenPrincipalFile != null && ImagenPrincipalFile.ContentLength > 0)
                {
                    var validacion = ValidarImagenSegura(ImagenPrincipalFile);
                    if (!validacion.EsValida)
                    {
                        ModelState.AddModelError("ImagenPrincipalFile", validacion.MensajeError);
                        CargarListasParaViewModel(model);
                        return View(model);
                    }

                    byte[] imagenBytes = ImageHelper.ConvertImageToByteArray(ImagenPrincipalFile);
                    imagenBytes = ImageHelper.ResizeImage(imagenBytes, 800, 600);
                    mascota.ImagenPrincipal = imagenBytes;
                }

                var userId = UserHelper.GetCurrentUserId() ?? 1;
                mascotaService.ActualizarMascota(mascota, userId);

                TempData["SuccessMessage"] = $"Mascota '{mascota.Nombre}' actualizada exitosamente";
                return RedirectToAction("Detalle", new { id = mascota.MascotaId });
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                CargarListasParaViewModel(model);
                return View(model);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en Editar POST: {ex.Message}");
                AuditoriaHelper.RegistrarError("Mascotas", $"Error al editar ID:{model.MascotaId}", ex, UserHelper.GetCurrentUserId());
                ModelState.AddModelError("", "Error al actualizar. Intente nuevamente.");
                CargarListasParaViewModel(model);
                return View(model);
            }
        }

        #endregion

        #region ARCHIVAR Y CAMBIAR ESTADO - SIN MODIFICAR

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRoles("Administrador", "Veterinario")]
        public ActionResult Archivar(int id, string motivo)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(motivo))
                {
                    TempData["ErrorMessage"] = "Debe proporcionar un motivo para archivar";
                    return RedirectToAction("Gestionar");
                }

                var userId = UserHelper.GetCurrentUserId() ?? 1;
                mascotaService.ArchivarMascota(id, userId, motivo);

                TempData["SuccessMessage"] = "Mascota archivada exitosamente";
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al archivar: {ex.Message}");
                AuditoriaHelper.RegistrarError("Mascotas", $"Error al archivar ID:{id}", ex, UserHelper.GetCurrentUserId());
                TempData["ErrorMessage"] = "Error al archivar mascota.";
            }

            return RedirectToAction("Gestionar");
        }

        [AuthorizeRoles("Administrador", "Veterinario")]
        public ActionResult ValidarArchivado(int id)
        {
            try
            {
                var validacion = mascotaService.ValidarArchivado(id);

                return Json(new
                {
                    success = validacion.PuedeArchivar,
                    message = validacion.Razon
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = $"Error: {ex.Message}"
                }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRoles("Administrador", "Veterinario")]
        public JsonResult CambiarEstado(int id, string nuevoEstado, string motivo = "")
        {
            try
            {
                var userId = UserHelper.GetCurrentUserId() ?? 1;
                mascotaService.CambiarEstado(id, nuevoEstado, userId, motivo);

                return Json(new
                {
                    success = true,
                    message = "Estado actualizado correctamente",
                    nuevoEstado = nuevoEstado
                });
            }
            catch (InvalidOperationException ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en CambiarEstado: {ex.Message}");
                AuditoriaHelper.RegistrarError("Mascotas", $"Error cambio estado ID:{id}", ex, UserHelper.GetCurrentUserId());
                return Json(new { success = false, message = "Error al cambiar estado" });
            }
        }

        [AuthorizeRoles("Administrador", "Veterinario", "Rescatista")]
        public ActionResult HistorialEstados(int? id)
        {
            if (!id.HasValue)
            {
                TempData["ErrorMessage"] = "ID de mascota no especificado";
                return RedirectToAction("Gestionar");
            }

            try
            {
                var mascota = mascotaService.ObtenerMascotaPorId(id.Value);
                if (mascota == null)
                {
                    TempData["ErrorMessage"] = "Mascota no encontrada";
                    return RedirectToAction("Gestionar");
                }

                var historial = mascotaService.ObtenerHistorialEstados(id.Value);
                ViewBag.Mascota = mascota;
                ViewBag.SexoTexto = mascota.Sexo == "M" ? "Macho" : "Hembra";
                ViewBag.ImagenBase64 = mascota.ImagenPrincipal != null ?
                    ImageHelper.GetImageDataUri(mascota.ImagenPrincipal) : null;

                return View(historial);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en HistorialEstados: {ex.Message}");
                AuditoriaHelper.RegistrarError("Mascotas", $"Error en HistorialEstados ID:{id.Value}", ex, UserHelper.GetCurrentUserId());
                TempData["ErrorMessage"] = "Error al cargar historial";
                return RedirectToAction("Gestionar");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRoles("Administrador", "Veterinario")]
        public JsonResult CambiarEstadoConConfirmacion(int id, string nuevoEstado, string motivo = "", bool confirmarAdvertencia = false)
        {
            try
            {
                var mascota = mascotaService.ObtenerMascotaPorId(id);
                if (mascota == null)
                {
                    return Json(new { success = false, message = "Mascota no encontrada" });
                }

                // Verificar si es una transición que requiere confirmación
                bool requiereConfirmacion = EsTransicionCritica(mascota.Estado, nuevoEstado);

                if (requiereConfirmacion && !confirmarAdvertencia)
                {
                    string mensajeAdvertencia = GetMensajeAdvertencia(mascota.Estado, nuevoEstado);
                    return Json(new
                    {
                        success = false,
                        requiereConfirmacion = true,
                        mensaje = mensajeAdvertencia
                    });
                }

                var userId = UserHelper.GetCurrentUserId() ?? 1;
                mascotaService.CambiarEstado(id, nuevoEstado, userId, motivo);

                return Json(new
                {
                    success = true,
                    message = $"Estado cambiado a: {nuevoEstado}",
                    nuevoEstado = nuevoEstado
                });
            }
            catch (InvalidOperationException ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en CambiarEstadoConConfirmacion: {ex.Message}");
                AuditoriaHelper.RegistrarError("Mascotas", $"Error cambio estado ID:{id}", ex, UserHelper.GetCurrentUserId());
                return Json(new { success = false, message = "Error al cambiar estado" });
            }
        }

        // Métodos auxiliares para advertencias
        private bool EsTransicionCritica(string estadoActual, string estadoNuevo)
        {
            var transicionesCriticas = new[]
            {
                "En tratamiento->Disponible para adopción",
                "En cuarentena->Disponible para adopción",
                "Rescatada->Disponible para adopción",
                "No disponible->Adoptada",
                "En tratamiento->Adoptada"
            };

            return transicionesCriticas.Contains($"{estadoActual}->{estadoNuevo}");
        }

        private string GetMensajeAdvertencia(string estadoActual, string estadoNuevo)
        {
            if (estadoActual == "En tratamiento")
            {
                return "⚠️ ADVERTENCIA: Esta mascota está en TRATAMIENTO MÉDICO.<br/>" +
                       "¿Está seguro que puede cambiar a '" + estadoNuevo + "'?<br/>" +
                       "<strong>Recomendación:</strong> Primero debe tener aprobación veterinaria.";
            }
            else if (estadoActual == "En cuarentena")
            {
                return "⚠️ ADVERTENCIA: Esta mascota está en CUARENTENA.<br/>" +
                       "¿Está seguro que puede cambiar a '" + estadoNuevo + "'?<br/>" +
                       "<strong>Recomendación:</strong> Primero debe completar período de cuarentena.";
            }
            else if (estadoActual == "Rescatada" && estadoNuevo == "Disponible para adopción")
            {
                return "⚠️ ADVERTENCIA: Esta mascota fue RECIÉN RESCATADA.<br/>" +
                       "¿Está seguro que puede cambiar a '" + estadoNuevo + "' sin revisión veterinaria?<br/>" +
                       "<strong>Recomendación:</strong> Primero debe pasar por 'En revisión veterinaria'.";
            }
            else if (estadoActual == "No disponible" && estadoNuevo == "Adoptada")
            {
                return "⚠️ ADVERTENCIA: Esta mascota NO ESTÁ DISPONIBLE.<br/>" +
                       "¿Está seguro que puede saltar directamente a '" + estadoNuevo + "'?<br/>" +
                       "<strong>Recomendación:</strong> Primero debe pasar por 'Disponible para adopción' y proceso de adopción.";
            }
            else
            {
                return "⚠️ ADVERTENCIA: Cambiar de '" + estadoActual + "' a '" + estadoNuevo + "' no es una transición normal.<br/>" +
                       "¿Está seguro de continuar?";
            }
        }

        #endregion

        // =====================================================================
        // REEMPLAZA la sección #region SEGUIMIENTO DE ADOPCIONES completa
        // en MascotasController.cs
        // =====================================================================

        #region SEGUIMIENTO DE ADOPCIONES

        [AuthorizeRoles("Administrador", "Veterinario")]
        public ActionResult SeguimientoAdopciones()
        {
            try
            {
                var mascotasConSeguimiento = mascotaService.ObtenerMascotasNecesitanSeguimiento();
                return View(mascotasConSeguimiento);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en SeguimientoAdopciones: {ex.Message}");
                AuditoriaHelper.RegistrarError("Mascotas", "Error en SeguimientoAdopciones", ex, UserHelper.GetCurrentUserId());
                TempData["ErrorMessage"] = "Error al cargar seguimientos";
                return View(new List<dynamic>());
            }
        }

        [AuthorizeRoles("Administrador", "Veterinario")]
        public ActionResult SeguimientoMascota(int id)
        {
            try
            {
                var mascota = db.Mascotas.Find(id);
                if (mascota == null)
                {
                    TempData["ErrorMessage"] = "Mascota no encontrada";
                    return RedirectToAction("SeguimientoAdopciones");
                }

                var seguimientos = mascotaService.ObtenerSeguimientosDeMascota(id);
                var contrato = db.ContratoAdopcion
                    .FirstOrDefault(c => c.SolicitudAdopcion.MascotaId == id);

                ViewBag.Mascota = mascota;
                ViewBag.Contrato = contrato;
                ViewBag.TotalSeguimientos = seguimientos.Count;
                ViewBag.SeguimientosCompletados = seguimientos.Count(s => s.FechaSeguimiento.HasValue);
                ViewBag.SeguimientosPendientes = seguimientos.Count(s => !s.FechaSeguimiento.HasValue);
                ViewBag.SexoTexto = mascota.Sexo == "M" ? "Macho" : "Hembra";
                ViewBag.ImagenBase64 = mascota.ImagenPrincipal != null ?
                    ImageHelper.GetImageDataUri(mascota.ImagenPrincipal) : null;

                return View(seguimientos);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en SeguimientoMascota: {ex.Message}");
                AuditoriaHelper.RegistrarError("Mascotas", $"Error en SeguimientoMascota ID:{id}", ex, UserHelper.GetCurrentUserId());
                TempData["ErrorMessage"] = "Error al cargar seguimientos";
                return RedirectToAction("SeguimientoAdopciones");
            }
        }

        // ✅ GET: Acepta tanto ?mascotaId=5 como /CrearSeguimientoManual/5
        [AuthorizeRoles("Administrador", "Veterinario")]
        public ActionResult CrearSeguimientoManual(int? mascotaId, int? id)
        {
            // Resolver el ID desde cualquiera de los dos parámetros
            int resolvedId = mascotaId ?? id ?? 0;

            System.Diagnostics.Debug.WriteLine($"[SEGUIMIENTO] GET CrearSeguimientoManual - mascotaId={mascotaId}, id={id}, resolved={resolvedId}");

            if (resolvedId == 0)
            {
                TempData["ErrorMessage"] = "ID de mascota no especificado.";
                return RedirectToAction("SeguimientoAdopciones");
            }

            try
            {
                var mascota = db.Mascotas.Find(resolvedId);
                if (mascota == null)
                {
                    TempData["ErrorMessage"] = $"Mascota con ID {resolvedId} no encontrada.";
                    return RedirectToAction("SeguimientoAdopciones");
                }

                System.Diagnostics.Debug.WriteLine($"[SEGUIMIENTO] Mascota encontrada: {mascota.Nombre}");

                // Buscar contrato existente
                var contrato = db.ContratoAdopcion
                    .FirstOrDefault(c => c.SolicitudAdopcion.MascotaId == resolvedId);

                System.Diagnostics.Debug.WriteLine($"[SEGUIMIENTO] Contrato existente: {contrato?.ContratoId.ToString() ?? "null"}");

                if (contrato == null)
                {
                    System.Diagnostics.Debug.WriteLine("[SEGUIMIENTO] Creando contrato ficticio...");
                    contrato = CrearContratoFicticio(mascota);

                    if (contrato == null)
                    {
                        // Si falla CrearContratoFicticio, mostrar el error en SeguimientoMascota
                        TempData["ErrorMessage"] = "No se pudo crear el contrato automático. Verifique que la mascota tenga estado 'Adoptada' y que existan usuarios en el sistema.";
                        return RedirectToAction("SeguimientoMascota", new { id = resolvedId });
                    }

                    System.Diagnostics.Debug.WriteLine($"[SEGUIMIENTO] Contrato ficticio creado: {contrato.ContratoId}");
                }

                var model = new SeguimientoAdopcion
                {
                    ContratoId = contrato.ContratoId,
                    FechaSeguimiento = DateTime.Now,
                    ResponsableSeguimiento = UserHelper.GetCurrentUserId() ?? 1,
                    TipoSeguimiento = "Manual",
                    EstadoMascota = "Bueno",
                    CondicionesVivienda = "Buenas",
                    RelacionConAdoptante = "Buena",
                    RequiereIntervencion = false,
                    ProximoSeguimiento = DateTime.Now.AddDays(30)
                };

                ViewBag.Mascota = mascota;
                ViewBag.Contrato = contrato;
                CargarListasViewBagSeguimiento();

                System.Diagnostics.Debug.WriteLine($"[SEGUIMIENTO] Retornando View con ContratoId={contrato.ContratoId}");
                return View(model);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SEGUIMIENTO] ERROR CRITICO: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[SEGUIMIENTO] Inner: {ex.InnerException?.Message}");
                System.Diagnostics.Debug.WriteLine($"[SEGUIMIENTO] Inner2: {ex.InnerException?.InnerException?.Message}");

                AuditoriaHelper.RegistrarError("Mascotas", "Error en CrearSeguimientoManual GET", ex, UserHelper.GetCurrentUserId());

                // Mostrar error detallado en pantalla para diagnosticar
                TempData["ErrorMessage"] = $"Error: {ex.Message} | {ex.InnerException?.Message} | {ex.InnerException?.InnerException?.Message}";
                return RedirectToAction("SeguimientoAdopciones");
            }
        }

        // Método auxiliar para cargar listas del formulario de seguimiento
        private void CargarListasViewBagSeguimiento()
        {
            ViewBag.TiposSeguimiento = new[]
            {
        "Inicial", "Primer Mes", "Tercer Mes", "Sexto Mes", "Anual",
        "Manual", "Extraordinario", "Queja", "Seguimiento Especial"
    };
            ViewBag.EstadosMascota = new[]
            {
        "Excelente", "Bueno", "Regular", "Malo", "Enfermo",
        "Recuperándose", "Por evaluar"
    };
            ViewBag.CondicionesVivienda = new[]
            {
        "Excelentes", "Buenas", "Regulares", "Inadecuadas",
        "Temporales", "Por evaluar"
    };
            ViewBag.Relaciones = new[]
            {
        "Excelente", "Buena", "Regular", "Mala", "Conflictiva",
        "Por evaluar"
    };
        }

        // ✅ POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRoles("Administrador", "Veterinario")]
        public ActionResult CrearSeguimientoManual(SeguimientoAdopcion model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var contrato = db.ContratoAdopcion.Find(model.ContratoId);
                    if (contrato == null)
                    {
                        ModelState.AddModelError("", "Contrato no encontrado. Intente nuevamente.");
                        CargarListasViewBagSeguimiento();
                        return View(model);
                    }

                    if (model.ResponsableSeguimiento == 0)
                        model.ResponsableSeguimiento = UserHelper.GetCurrentUserId() ?? 1;

                    db.SeguimientoAdopcion.Add(model);
                    db.SaveChanges();

                    AuditoriaHelper.RegistrarAccion("Mascotas", "CrearSeguimientoManual",
                        $"Seguimiento manual creado para contrato ID: {model.ContratoId}",
                        UserHelper.GetCurrentUserId() ?? 1);

                    TempData["SuccessMessage"] = "Seguimiento registrado exitosamente";

                    // Obtener mascotaId para redirigir correctamente
                    var mascotaId = contrato.SolicitudAdopcion?.MascotaId
                        ?? db.SolicitudAdopcion
                            .Where(s => s.SolicitudId == contrato.SolicitudId)
                            .Select(s => s.MascotaId)
                            .FirstOrDefault();

                    return RedirectToAction("SeguimientoMascota", new { id = mascotaId });
                }

                // ModelState inválido — recargar vista
                var contratoReload = db.ContratoAdopcion.Find(model.ContratoId);
                if (contratoReload != null)
                {
                    var mascotaReload = db.Mascotas.Find(contratoReload.SolicitudAdopcion?.MascotaId ?? 0);
                    ViewBag.Mascota = mascotaReload;
                    ViewBag.Contrato = contratoReload;
                }
                CargarListasViewBagSeguimiento();
                return View(model);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en CrearSeguimientoManual POST: {ex.Message}");
                AuditoriaHelper.RegistrarError("Mascotas", "Error en CrearSeguimientoManual POST", ex, UserHelper.GetCurrentUserId());
                TempData["ErrorMessage"] = "Error al crear seguimiento: " + ex.Message;
                return View(model);
            }
        }

        // =====================================================================
        // REEMPLAZA los métodos EditarSeguimiento GET y POST en MascotasController.cs
        // =====================================================================

        [AuthorizeRoles("Administrador", "Veterinario")]
        public ActionResult EditarSeguimiento(int id)
        {
            try
            {
                // ✅ Incluir ContratoAdopcion y SolicitudAdopcion explícitamente
                var seguimiento = db.SeguimientoAdopcion
                    .Include(s => s.ContratoAdopcion)
                    .Include(s => s.ContratoAdopcion.SolicitudAdopcion)
                    .Include(s => s.ContratoAdopcion.SolicitudAdopcion.Mascotas)
                    .FirstOrDefault(s => s.SeguimientoId == id);

                if (seguimiento == null)
                {
                    TempData["ErrorMessage"] = "Seguimiento no encontrado";
                    return RedirectToAction("SeguimientoAdopciones");
                }

                // ✅ Obtener mascota desde la cadena de navegación
                var mascota = seguimiento.ContratoAdopcion?.SolicitudAdopcion?.Mascotas;

                // ✅ Si la navegación falló (EF lazy loading issue), buscar directamente
                if (mascota == null && seguimiento.ContratoAdopcion?.SolicitudAdopcion?.MascotaId != null)
                {
                    mascota = db.Mascotas.Find(seguimiento.ContratoAdopcion.SolicitudAdopcion.MascotaId);
                }

                System.Diagnostics.Debug.WriteLine($"[EDITAR-SEG] SeguimientoId={id}, ContratoId={seguimiento.ContratoId}, Mascota={mascota?.Nombre ?? "null"}");

                ViewBag.Mascota = mascota;
                ViewBag.Contrato = seguimiento.ContratoAdopcion;
                CargarListasViewBagSeguimiento();

                return View(seguimiento);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[EDITAR-SEG] ERROR GET: {ex.Message} | {ex.InnerException?.Message}");
                AuditoriaHelper.RegistrarError("Mascotas", $"Error en EditarSeguimiento GET ID:{id}", ex, UserHelper.GetCurrentUserId());
                TempData["ErrorMessage"] = "Error al cargar el seguimiento: " + ex.Message;
                return RedirectToAction("SeguimientoAdopciones");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRoles("Administrador", "Veterinario")]
        public ActionResult EditarSeguimiento(SeguimientoAdopcion model)
        {
            System.Diagnostics.Debug.WriteLine($"[EDITAR-SEG] POST - SeguimientoId={model.SeguimientoId}, ContratoId={model.ContratoId}");

            try
            {
                // Quitar validación de campos que no se editan desde este form
                ModelState.Remove("TipoSeguimiento");
                ModelState.Remove("EstadoMascota");
                ModelState.Remove("CondicionesVivienda");
                ModelState.Remove("RelacionConAdoptante");

                if (ModelState.IsValid)
                {
                    // ✅ Buscar el seguimiento existente en BD y actualizar solo los campos editables
                    var seguimientoExistente = db.SeguimientoAdopcion.Find(model.SeguimientoId);
                    if (seguimientoExistente == null)
                    {
                        TempData["ErrorMessage"] = "Seguimiento no encontrado.";
                        return RedirectToAction("SeguimientoAdopciones");
                    }

                    // Actualizar solo los campos que el formulario envía
                    seguimientoExistente.FechaSeguimiento = model.FechaSeguimiento;
                    seguimientoExistente.TipoSeguimiento = model.TipoSeguimiento;
                    seguimientoExistente.EstadoMascota = model.EstadoMascota;
                    seguimientoExistente.CondicionesVivienda = model.CondicionesVivienda;
                    seguimientoExistente.RelacionConAdoptante = model.RelacionConAdoptante;
                    seguimientoExistente.FotosEvidencia = model.FotosEvidencia;
                    seguimientoExistente.Observaciones = model.Observaciones;
                    seguimientoExistente.Recomendaciones = model.Recomendaciones;
                    seguimientoExistente.RequiereIntervencion = model.RequiereIntervencion;
                    seguimientoExistente.ProximoSeguimiento = model.ProximoSeguimiento;

                    // Mantener el responsable original si no se envió
                    if (model.ResponsableSeguimiento == 0)
                        seguimientoExistente.ResponsableSeguimiento = UserHelper.GetCurrentUserId() ?? seguimientoExistente.ResponsableSeguimiento;
                    else
                        seguimientoExistente.ResponsableSeguimiento = model.ResponsableSeguimiento;

                    db.SaveChanges();

                    AuditoriaHelper.RegistrarAccion(
                        accion: "Mascotas",
                        controlador: "EditarSeguimiento",
                        detalles: $"Seguimiento editado ID: {model.SeguimientoId}",
                        usuarioId: UserHelper.GetCurrentUserId() ?? 1
                    );

                    TempData["SuccessMessage"] = "Seguimiento actualizado exitosamente";

                    // ✅ Redirigir correctamente obteniendo MascotaId desde BD
                    var contrato = db.ContratoAdopcion
                        .Include(c => c.SolicitudAdopcion)
                        .FirstOrDefault(c => c.ContratoId == seguimientoExistente.ContratoId);

                    int? mascotaId = contrato?.SolicitudAdopcion?.MascotaId;

                    if (mascotaId.HasValue)
                        return RedirectToAction("SeguimientoMascota", new { id = mascotaId.Value });

                    return RedirectToAction("SeguimientoAdopciones");
                }

                // ModelState inválido — registrar errores para debug
                foreach (var key in ModelState.Keys)
                {
                    var errors = ModelState[key].Errors;
                    foreach (var error in errors)
                        System.Diagnostics.Debug.WriteLine($"[EDITAR-SEG] ModelState error - {key}: {error.ErrorMessage}");
                }

                // Recargar ViewBag para mostrar el formulario con errores
                var contratoReload = db.ContratoAdopcion
                    .Include(c => c.SolicitudAdopcion)
                    .Include(c => c.SolicitudAdopcion.Mascotas)
                    .FirstOrDefault(c => c.ContratoId == model.ContratoId);

                ViewBag.Mascota = contratoReload?.SolicitudAdopcion?.Mascotas
                                  ?? (contratoReload?.SolicitudAdopcion?.MascotaId != null
                                      ? db.Mascotas.Find(contratoReload.SolicitudAdopcion.MascotaId)
                                      : null);
                ViewBag.Contrato = contratoReload;
                CargarListasViewBagSeguimiento();

                return View(model);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[EDITAR-SEG] ERROR POST: {ex.Message} | {ex.InnerException?.Message}");
                AuditoriaHelper.RegistrarError("Mascotas", $"Error en EditarSeguimiento POST ID:{model.SeguimientoId}", ex, UserHelper.GetCurrentUserId());
                TempData["ErrorMessage"] = "Error al actualizar: " + ex.Message + " | " + ex.InnerException?.Message;

                // Intentar recargar vista con datos
                try
                {
                    var contratoFallback = db.ContratoAdopcion
                        .Include(c => c.SolicitudAdopcion)
                        .Include(c => c.SolicitudAdopcion.Mascotas)
                        .FirstOrDefault(c => c.ContratoId == model.ContratoId);
                    ViewBag.Mascota = contratoFallback?.SolicitudAdopcion?.Mascotas;
                    ViewBag.Contrato = contratoFallback;
                }
                catch { }

                CargarListasViewBagSeguimiento();
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRoles("Administrador", "Veterinario")]
        public ActionResult EliminarSeguimiento(int id)
        {
            try
            {
                var seguimiento = db.SeguimientoAdopcion.Find(id);
                if (seguimiento == null)
                    return Json(new { success = false, message = "Seguimiento no encontrado" });

                db.SeguimientoAdopcion.Remove(seguimiento);
                db.SaveChanges();

                AuditoriaHelper.RegistrarAccion("Mascotas", "EliminarSeguimiento",
                    $"Seguimiento eliminado ID: {id}", UserHelper.GetCurrentUserId() ?? 1);

                return Json(new { success = true, message = "Seguimiento eliminado" });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en EliminarSeguimiento: {ex.Message}");
                AuditoriaHelper.RegistrarError("Mascotas", $"Error en EliminarSeguimiento ID:{id}", ex, UserHelper.GetCurrentUserId());
                return Json(new { success = false, message = "Error al eliminar seguimiento" });
            }
        }

        [AuthorizeRoles("Administrador", "Veterinario")]
        public ActionResult CrearSeguimientosAutomaticos(int contratoId)
        {
            try
            {
                var userId = UserHelper.GetCurrentUserId() ?? 1;
                mascotaService.CrearSeguimientosParaContrato(contratoId, userId);

                TempData["SuccessMessage"] = "Seguimientos automáticos creados exitosamente";

                var contrato = db.ContratoAdopcion.Find(contratoId);
                var mascotaId = contrato?.SolicitudAdopcion?.MascotaId;
                if (mascotaId.HasValue)
                    return RedirectToAction("SeguimientoMascota", new { id = mascotaId.Value });

                return RedirectToAction("SeguimientoAdopciones");
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction("SeguimientoAdopciones");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en CrearSeguimientosAutomaticos: {ex.Message}");
                AuditoriaHelper.RegistrarError("Mascotas", "Error en CrearSeguimientosAutomaticos", ex, UserHelper.GetCurrentUserId());
                TempData["ErrorMessage"] = "Error al crear seguimientos automáticos";
                return RedirectToAction("SeguimientoAdopciones");
            }
        }

        private ContratoAdopcion CrearContratoFicticio(Mascotas mascota)
        {
            try
            {
                // Verificar si ya existe contrato para esta mascota
                var contratoExistente = db.ContratoAdopcion
                    .FirstOrDefault(c => c.SolicitudAdopcion.MascotaId == mascota.MascotaId);
                if (contratoExistente != null)
                    return contratoExistente;

                // Obtener UsuarioId válido
                int responsableId = UserHelper.GetCurrentUserId() ?? 0;

                if (responsableId == 0 || !db.Usuarios.Any(u => u.UsuarioId == responsableId))
                {
                    var primerUsuario = db.Usuarios
                        .Where(u => u.Activo == true || u.Activo == null)
                        .OrderBy(u => u.UsuarioId)
                        .Select(u => u.UsuarioId)
                        .FirstOrDefault();

                    if (primerUsuario == 0)
                        primerUsuario = db.Usuarios.OrderBy(u => u.UsuarioId).Select(u => u.UsuarioId).FirstOrDefault();

                    if (primerUsuario == 0)
                    {
                        System.Diagnostics.Debug.WriteLine("[CTR-AUTO] No hay usuarios en la BD");
                        return null;
                    }

                    responsableId = primerUsuario;
                }

                System.Diagnostics.Debug.WriteLine($"[CTR-AUTO] Usando responsableId={responsableId}");

                var solicitud = new SolicitudAdopcion
                {
                    MascotaId = mascota.MascotaId,
                    UsuarioId = responsableId,
                    FechaSolicitud = mascota.FechaAdopcion ?? DateTime.Now,
                    Estado = "Completada",
                    EstadoAdopcion = "No iniciada",
                    Observaciones = "Generado automaticamente - mascota adoptada directamente."
                };

                db.SolicitudAdopcion.Add(solicitud);
                db.SaveChanges();

                System.Diagnostics.Debug.WriteLine($"[CTR-AUTO] Solicitud creada: {solicitud.SolicitudId}");

                var numeroContrato = string.Format("CTR-AUTO-{0:D4}-{1:yyyyMMddHHmmss}", mascota.MascotaId, DateTime.Now);

                var contrato = new ContratoAdopcion
                {
                    SolicitudId = solicitud.SolicitudId,
                    NumeroContrato = numeroContrato,
                    FechaContrato = mascota.FechaAdopcion ?? DateTime.Now,
                    AdoptanteNombre = "Adoptante Directo",
                    AdoptanteCedula = "0000000000",
                    AdoptanteDireccion = "No especificada",
                    AdoptanteTelefono = "0000000000",
                    RepresentanteRefugioNombre = "Refugio ADOPTAMANIA",
                    RepresentanteRefugioCedula = "0000000000",
                    MascotaNombre = mascota.Nombre ?? "Sin nombre",
                    MascotaEspecie = mascota.Especie ?? "No especificada",
                    MascotaMicrochip = mascota.Microchip ?? "",
                    TerminosCondiciones = "Contrato generado automaticamente para mascota adoptada directamente.",
                    Estado = "Activo"
                };

                db.ContratoAdopcion.Add(contrato);
                db.SaveChanges();

                System.Diagnostics.Debug.WriteLine($"[CTR-AUTO] Contrato creado: {contrato.ContratoId}");
                return contrato;
            }
            catch (System.Data.Entity.Validation.DbEntityValidationException dbEx)
            {
                foreach (var error in dbEx.EntityValidationErrors)
                    foreach (var ve in error.ValidationErrors)
                        System.Diagnostics.Debug.WriteLine($"[CTR-AUTO] Validacion - Campo: {ve.PropertyName} | Error: {ve.ErrorMessage}");
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CTR-AUTO] ERROR: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[CTR-AUTO] Inner: {ex.InnerException?.Message}");
                System.Diagnostics.Debug.WriteLine($"[CTR-AUTO] Inner2: {ex.InnerException?.InnerException?.Message}");
                return null;
            }
        }

        #endregion

        #region CONTRATOS DE ADOPCIÓN - SIN MODIFICAR

        [AuthorizeRoles("Administrador", "Veterinario")]
        public ActionResult ContratosAdopcion()
        {
            try
            {
                var contratos = db.ContratoAdopcion
                    .Include(c => c.SolicitudAdopcion)
                    .Include(c => c.SolicitudAdopcion.Mascotas)
                    .OrderByDescending(c => c.FechaContrato)
                    .ToList();

                return View(contratos);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en ContratosAdopcion: {ex.Message}");
                AuditoriaHelper.RegistrarError("Mascotas", "Error en ContratosAdopcion", ex, UserHelper.GetCurrentUserId());
                TempData["ErrorMessage"] = "Error al cargar contratos";
                return View(new List<ContratoAdopcion>());
            }
        }

        [AuthorizeRoles("Administrador", "Veterinario")]
        public ActionResult DetalleContrato(int id)
        {
            try
            {
                var contrato = db.ContratoAdopcion
                    .Include(c => c.SolicitudAdopcion)
                    .Include(c => c.SolicitudAdopcion.Mascotas)
                    .FirstOrDefault(c => c.ContratoId == id);

                if (contrato == null)
                {
                    TempData["ErrorMessage"] = "Contrato no encontrado";
                    return RedirectToAction("ContratosAdopcion");
                }

                var seguimientos = db.SeguimientoAdopcion
                    .Where(s => s.ContratoId == id)
                    .OrderByDescending(s => s.FechaSeguimiento)
                    .ToList();

                ViewBag.Seguimientos = seguimientos;
                ViewBag.TotalSeguimientos = seguimientos.Count;
                ViewBag.SeguimientosCompletados = seguimientos.Count(s => s.FechaSeguimiento.HasValue);

                return View(contrato);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en DetalleContrato: {ex.Message}");
                AuditoriaHelper.RegistrarError("Mascotas", $"Error en DetalleContrato ID:{id}", ex, UserHelper.GetCurrentUserId());
                TempData["ErrorMessage"] = "Error al cargar contrato";
                return RedirectToAction("ContratosAdopcion");
            }
        }

        [AuthorizeRoles("Administrador")]
        public ActionResult DescargarContrato(int id)
        {
            try
            {
                var contrato = db.ContratoAdopcion
                    .Include(c => c.SolicitudAdopcion)
                    .FirstOrDefault(c => c.ContratoId == id);

                if (contrato == null)
                {
                    TempData["ErrorMessage"] = "Contrato no encontrado";
                    return RedirectToAction("ContratosAdopcion");
                }

                // Si ya tiene PDF físico guardado, servirlo directamente
                if (!string.IsNullOrEmpty(contrato.DocumentoPDF))
                {
                    var rutaFisica = Server.MapPath(contrato.DocumentoPDF);
                    if (System.IO.File.Exists(rutaFisica))
                    {
                        return File(rutaFisica, "application/pdf",
                            string.Format("Contrato_{0}.pdf", contrato.NumeroContrato));
                    }
                }

                // Si no hay PDF, generar uno básico como HTML imprimible
                TempData["InfoMessage"] = "Este contrato no tiene PDF adjunto. Usa el botón Imprimir desde el detalle para generarlo.";
                return RedirectToAction("DetalleContrato", new { id = id });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(string.Format("Error en DescargarContrato: {0}", ex.Message));
                AuditoriaHelper.RegistrarError("Mascotas", string.Format("Error en DescargarContrato ID:{0}", id), ex, UserHelper.GetCurrentUserId());
                TempData["ErrorMessage"] = "Error al descargar el contrato";
                return RedirectToAction("ContratosAdopcion");
            }
        }

        #endregion

        #region RESTAURACIÓN Y ELIMINACIÓN FÍSICA - CORREGIDO

        [AuthorizeRoles("Administrador")]
        public ActionResult MascotasArchivadas()
        {
            try
            {
                var mascotasArchivadas = db.Mascotas
                    .Where(m => m.Estado == "Archivada" || m.Activo == false)
                    .OrderByDescending(m => m.FechaIngreso)
                    .ToList();

                return View(mascotasArchivadas);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en MascotasArchivadas: {ex.Message}");
                AuditoriaHelper.RegistrarError("Mascotas", "Error en MascotasArchivadas", ex, UserHelper.GetCurrentUserId());
                TempData["ErrorMessage"] = "Error al cargar mascotas archivadas";
                return View(new List<Mascotas>());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRoles("Administrador")]
        public ActionResult RestaurarMascota(int id, string motivo)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(motivo))
                {
                    TempData["ErrorMessage"] = "Debe proporcionar un motivo para restaurar";
                    return RedirectToAction("MascotasArchivadas");
                }

                var userId = UserHelper.GetCurrentUserId() ?? 1;
                mascotaService.RestaurarMascotaArchivada(id, userId, motivo);

                TempData["SuccessMessage"] = "Mascota restaurada exitosamente";
                return RedirectToAction("Gestionar");
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction("MascotasArchivadas");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en RestaurarMascota: {ex.Message}");
                AuditoriaHelper.RegistrarError("Mascotas", $"Error en RestaurarMascota ID:{id}", ex, UserHelper.GetCurrentUserId());
                TempData["ErrorMessage"] = "Error al restaurar mascota";
                return RedirectToAction("MascotasArchivadas");
            }
        }

        [AuthorizeRoles("Administrador")]
        public ActionResult ValidarEliminacionFisica(int id)
        {
            try
            {
                var validacion = mascotaService.ValidarEliminacionFisica(id);

                return Json(new
                {
                    success = validacion.PuedeEliminar,
                    message = validacion.Razon
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = $"Error: {ex.Message}"
                }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRoles("Administrador")]
        public ActionResult EliminarFisicamente(int id, string justificacion)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(justificacion) || justificacion.Length < 20)
                {
                    TempData["ErrorMessage"] = "Debe proporcionar una justificación detallada (mínimo 20 caracteres)";
                    return RedirectToAction("MascotasArchivadas");
                }

                var userId = UserHelper.GetCurrentUserId() ?? 1;
                mascotaService.EliminarFisicamente(id, userId, justificacion);

                TempData["SuccessMessage"] = "Mascota eliminada físicamente del sistema";
                return RedirectToAction("MascotasArchivadas");
            }
            catch (UnauthorizedAccessException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction("MascotasArchivadas");
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction("MascotasArchivadas");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en EliminarFisicamente: {ex.Message}");
                AuditoriaHelper.RegistrarError("Mascotas", $"Error en EliminarFisicamente ID:{id}", ex, UserHelper.GetCurrentUserId());
                TempData["ErrorMessage"] = "Error al eliminar mascota";
                return RedirectToAction("MascotasArchivadas");
            }
        }

        #endregion

        #region EXPORTAR DATOS - SIN MODIFICAR

        [AuthorizeRoles("Administrador", "Veterinario")]
        public ActionResult ExportarMascotas(string estado = "", string especie = "", string buscar = "")
        {
            try
            {
                IQueryable<Mascotas> query = db.Mascotas.Where(m => m.Activo == true);

                if (!string.IsNullOrEmpty(estado) && estado != "Todos")
                    query = query.Where(m => m.Estado == estado);

                if (!string.IsNullOrEmpty(especie) && especie != "Todos")
                    query = query.Where(m => m.Especie == especie);

                if (!string.IsNullOrEmpty(buscar))
                    query = query.Where(m => m.Nombre.Contains(buscar) || (m.Microchip != null && m.Microchip.Contains(buscar)));

                var mascotas = query
                    .OrderByDescending(m => m.FechaIngreso)
                    .Select(m => new
                    {
                        ID = m.MascotaId,
                        Nombre = m.Nombre,
                        Especie = m.Especie,
                        Raza = m.Raza ?? "Mestizo",
                        Sexo = m.Sexo == "M" ? "Macho" : "Hembra",
                        Edad = m.EdadAproximada,
                        Tamaño = m.Tamanio,
                        Estado = m.Estado,
                        Esterilizado = m.Esterilizado == true ? "Sí" : "No",
                        FechaIngreso = m.FechaIngreso,
                        Microchip = m.Microchip ?? "No registrado",
                        Color = m.Color ?? "No especificado"
                    })
                    .ToList();

                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.AppendLine("ID,Nombre,Especie,Raza,Sexo,Edad,Tamaño,Estado,Esterilizado,FechaIngreso,Microchip,Color");

                foreach (var m in mascotas)
                {
                    sb.AppendLine($"{m.ID},{EscapeCsv(m.Nombre)},{m.Especie},{EscapeCsv(m.Raza)},{m.Sexo},{EscapeCsv(m.Edad)},{m.Tamaño},{m.Estado},{m.Esterilizado},{m.FechaIngreso?.ToString("dd/MM/yyyy")},{m.Microchip},{EscapeCsv(m.Color)}");
                }

                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(sb.ToString());

                Response.Clear();
                Response.ContentType = "text/csv";
                Response.AddHeader("Content-Disposition", $"attachment; filename=Mascotas_Refugio_{DateTime.Now:yyyyMMdd}.csv");
                Response.BinaryWrite(buffer);
                Response.End();

                AuditoriaHelper.RegistrarAccion("Mascotas", "ExportarMascotas",
                    $"Exportadas {mascotas.Count} mascotas", UserHelper.GetCurrentUserId() ?? 0);

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en ExportarMascotas: {ex.Message}");
                TempData["ErrorMessage"] = "Error al exportar las mascotas";
                return RedirectToAction("Gestionar");
            }
        }

        private string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
                return "\"" + value.Replace("\"", "\"\"") + "\"";

            return value;
        }

        #endregion

        #region MÉTODOS AUXILIARES

        public class ResultadoValidacionImagenMascota
        {
            public bool EsValida { get; set; }
            public string MensajeError { get; set; }
        }

        private void CargarListasParaViewBag()
        {
            try
            {
                var veterinarios = db.Usuarios
                    .Where(u => u.UsuariosRoles.Any(ur => ur.RolId == 2) && (u.Activo == null || u.Activo == true))
                    .OrderBy(u => u.NombreCompleto)
                    .Select(u => new SelectListItem
                    {
                        Value = u.UsuarioId.ToString(),
                        Text = u.NombreCompleto ?? "Sin nombre"
                    })
                    .ToList();

                ViewBag.Veterinarios = new SelectList(veterinarios, "Value", "Text");

                var rescatistas = db.Usuarios
                    .Where(u => u.UsuariosRoles.Any(ur => ur.RolId == 3) && (u.Activo == null || u.Activo == true))
                    .OrderBy(u => u.NombreCompleto)
                    .Select(u => new SelectListItem
                    {
                        Value = u.UsuarioId.ToString(),
                        Text = u.NombreCompleto ?? "Sin nombre"
                    })
                    .ToList();

                ViewBag.Rescatistas = new SelectList(rescatistas, "Value", "Text");

                var especies = db.Mascotas
                    .Where(m => m.Activo == true)
                    .Select(m => m.Especie)
                    .Distinct()
                    .OrderBy(e => e)
                    .ToList();

                ViewBag.Especies = especies;

                ViewBag.Estados = new[]
                {
                    "Rescatada", "En revisión veterinaria", "Pendiente de exámenes",
                    "En cuarentena", "En tratamiento", "No disponible",
                    "Disponible para adopción", "Adoptada", "Fallecida", "Archivada"
                };

                ViewBag.Tamanios = new[] { "Pequeño", "Mediano", "Grande", "Muy Grande" };
                ViewBag.Categorias = new[] { "Normal", "Especial", "Urgente", "Discapacitada", "Senior" };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en CargarListasParaViewBag: {ex.Message}");
                AuditoriaHelper.RegistrarError("Mascotas", "Error en CargarListasParaViewBag", ex, UserHelper.GetCurrentUserId());
                ViewBag.Veterinarios = new SelectList(new List<SelectListItem>());
                ViewBag.Rescatistas = new SelectList(new List<SelectListItem>());
                ViewBag.Especies = new List<string>();
                ViewBag.Estados = new string[0];
                ViewBag.Tamanios = new string[0];
                ViewBag.Categorias = new string[0];
            }
        }

        private ResultadoValidacionImagenMascota ValidarImagenSegura(HttpPostedFileBase archivo)
        {
            if (archivo.ContentLength > 5 * 1024 * 1024)
            {
                return new ResultadoValidacionImagenMascota
                {
                    EsValida = false,
                    MensajeError = "La imagen no debe exceder 5 MB"
                };
            }

            var extensionesPermitidas = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var extension = System.IO.Path.GetExtension(archivo.FileName)?.ToLower();

            if (!extensionesPermitidas.Contains(extension))
            {
                return new ResultadoValidacionImagenMascota
                {
                    EsValida = false,
                    MensajeError = "Formato no permitido. Use JPG, PNG o GIF"
                };
            }

            try
            {
                using (var img = System.Drawing.Image.FromStream(archivo.InputStream))
                {
                    if (img.Width < 100 || img.Height < 100)
                    {
                        return new ResultadoValidacionImagenMascota
                        {
                            EsValida = false,
                            MensajeError = "La imagen debe tener al menos 100x100 píxeles"
                        };
                    }
                    archivo.InputStream.Position = 0;
                }
            }
            catch
            {
                return new ResultadoValidacionImagenMascota
                {
                    EsValida = false,
                    MensajeError = "El archivo no es una imagen válida"
                };
            }

            return new ResultadoValidacionImagenMascota { EsValida = true };
        }

        private string SanitizarBusqueda(string busqueda)
        {
            if (string.IsNullOrWhiteSpace(busqueda))
                return "";

            if (busqueda.Length > 100)
                busqueda = busqueda.Substring(0, 100);

            return System.Text.RegularExpressions.Regex.Replace(
                busqueda.Trim(),
                @"[<>'"";(){}[\]\\]",
                ""
            );
        }

        #endregion

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db?.Dispose();
                mascotaService?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}