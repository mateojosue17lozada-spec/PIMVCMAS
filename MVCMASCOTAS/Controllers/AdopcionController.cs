using System;
using System.Linq;
using System.Web.Mvc;
using MVCMASCOTAS.Helpers;
using MVCMASCOTAS.Models;
using MVCMASCOTAS.Models.ViewModels;
using System.Data.Entity;
using System.Collections.Generic;

namespace MVCMASCOTAS.Controllers
{
    [Authorize]
    public class AdopcionController : Controller
    {
        private RefugioMascotasDBEntities db = new RefugioMascotasDBEntities();

        // ============================================================================
        // ACCIÓN DETALLES
        // ============================================================================

        // GET: Adopcion/Detalles/5
        public ActionResult Detalles(int? id)
        {
            if (id == null)
            {
                TempData["ErrorMessage"] = "ID de solicitud no especificado.";
                return RedirectToAction("MisSolicitudes");
            }

            try
            {
                var solicitud = db.SolicitudAdopcion
                    .Include(s => s.Mascotas)
                    .Include(s => s.Usuarios)
                    .Include(s => s.Usuarios1)
                    .Include(s => s.FormularioAdopcionDetalle)
                    .Include(s => s.EvaluacionAdopcion)
                    .FirstOrDefault(s => s.SolicitudId == id);

                if (solicitud == null)
                {
                    TempData["ErrorMessage"] = "Solicitud no encontrada.";
                    return RedirectToAction("MisSolicitudes");
                }

                var usuarioActual = db.Usuarios.FirstOrDefault(u => u.Email == User.Identity.Name);
                if (solicitud.UsuarioId != usuarioActual?.UsuarioId &&
                    !User.IsInRole("Administrador") &&
                    !User.IsInRole("Veterinario"))
                {
                    TempData["ErrorMessage"] = "No tienes permiso para ver esta solicitud.";
                    return RedirectToAction("MisSolicitudes");
                }

                if (solicitud.Mascotas?.ImagenPrincipal != null)
                {
                    ViewBag.MascotaImagen = Convert.ToBase64String(solicitud.Mascotas.ImagenPrincipal);
                }

                if (solicitud.Usuarios?.ImagenPerfil != null)
                {
                    ViewBag.UsuarioImagen = Convert.ToBase64String(solicitud.Usuarios.ImagenPerfil);
                }

                return View(solicitud);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR en Adopcion/Detalles: {ex.Message}\n{ex.StackTrace}");

                try
                {
                    var usuario = db.Usuarios.FirstOrDefault(u => u.Email == User.Identity.Name);
                    if (usuario != null)
                    {
                        AuditoriaHelper.RegistrarError("Adopcion", "Error al cargar detalles de solicitud", ex, usuario.UsuarioId);
                    }
                }
                catch { }

                TempData["ErrorMessage"] = "Error al cargar los detalles de la solicitud.";
                return RedirectToAction("MisSolicitudes");
            }
        }

        // ============================================================================
        // ACCIONES EXISTENTES
        // ============================================================================

        // GET: Adopcion/Solicitar/5
        public ActionResult Solicitar(int mascotaId)
        {
            var mascota = db.Mascotas.Find(mascotaId);

            if (mascota == null || mascota.Estado != "Disponible para adopción")
            {
                TempData["ErrorMessage"] = "Esta mascota no está disponible para adopción.";
                return RedirectToAction("Index", "Mascotas");
            }

            var usuario = db.Usuarios.FirstOrDefault(u => u.Email == User.Identity.Name);

            var solicitudExistente = db.SolicitudAdopcion
                .Any(s => s.MascotaId == mascotaId && s.UsuarioId == usuario.UsuarioId &&
                          (s.Estado == "Pendiente" || s.Estado == "En evaluación" || s.Estado == "Aprobada"));

            if (solicitudExistente)
            {
                TempData["ErrorMessage"] = "Ya tienes una solicitud activa para esta mascota.";
                return RedirectToAction("MisSolicitudes");
            }

            ViewBag.NombreMascota = mascota.Nombre;
            ViewBag.MascotaId = mascotaId;
            ViewBag.ImagenBase64 = mascota.ImagenPrincipal != null
                ? Convert.ToBase64String(mascota.ImagenPrincipal)
                : null;

            var model = new FormularioAdopcionViewModel
            {
                MascotaId = mascotaId
            };

            return View(model);
        }

        // POST: Adopcion/Solicitar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Solicitar(int mascotaId, FormularioAdopcionViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var mascota = db.Mascotas.Find(mascotaId);
                ViewBag.NombreMascota = mascota.Nombre;
                ViewBag.MascotaId = mascotaId;
                ViewBag.ImagenBase64 = mascota?.ImagenPrincipal != null
                    ? Convert.ToBase64String(mascota.ImagenPrincipal)
                    : null;
                return View(model);
            }

            var usuario = db.Usuarios.FirstOrDefault(u => u.Email == User.Identity.Name);

            // Crear solicitud
            var solicitud = new SolicitudAdopcion
            {
                MascotaId = mascotaId,
                UsuarioId = usuario.UsuarioId,
                FechaSolicitud = DateTime.Now,
                Estado = "Pendiente"
            };

            db.SolicitudAdopcion.Add(solicitud);
            db.SaveChanges();

            // CORRECCIÓN: Como CantidadPerros y CantidadGatos son int (no nullable) en tu ViewModel,
            // simplemente los asignamos directamente
            var formulario = new FormularioAdopcionDetalle
            {
                SolicitudId = solicitud.SolicitudId,
                TipoVivienda = model.TipoVivienda,
                ViviendaPropia = model.ViviendaPropia,
                TieneJardin = model.TieneJardin,
                TamanioJardin = model.TamanioJardin,
                PermisoMascotas = model.PermisoMascotas,
                PersonasEnCasa = model.PersonasEnCasa,
                HayNinios = model.HayNinios,
                EdadesNinios = model.EdadesNinios,
                ExperienciaPreviaConMascotas = model.ExperienciaPreviaConMascotas,
                DetalleExperiencia = model.DetalleExperiencia,
                TieneMascotasActualmente = model.TieneMascotasActualmente,
                CantidadPerros = model.CantidadPerros,  // Directamente, son int
                CantidadGatos = model.CantidadGatos,    // Directamente, son int
                OtrasMascotas = model.OtrasMascotas,
                MascotasEsterilizadas = model.MascotasEsterilizadas,
                TiempoDisponibleDiario = model.TiempoDisponibleDiario,
                QuienCuidaraMascota = model.QuienCuidaraMascota,
                MotivoAdopcion = model.MotivoAdopcion,
                QuePasaSiCambiaResidencia = model.QuePasaSiCambiaResidencia,
                QuePasaSiProblemasComportamiento = model.QuePasaSiProblemasComportamiento,
                VeterinarioReferencia = model.VeterinarioReferencia,
                ReferenciaPersonal1 = model.ReferenciaPersonal1,
                TelefonoReferencia1 = model.TelefonoReferencia1,
                ReferenciaPersonal2 = model.ReferenciaPersonal2,
                TelefonoReferencia2 = model.TelefonoReferencia2,
                AceptaEsterilizacion = model.AceptaEsterilizacion,
                AceptaVisitasSeguimiento = model.AceptaVisitasSeguimiento,
                AceptaCondicionesLOBA = model.AceptaCondicionesLOBA,
                AceptaDevolucionSiNoPuedeAtender = model.AceptaDevolucionSiNoPuedeAtender,
                FechaLlenado = DateTime.Now
            };

            db.FormularioAdopcionDetalle.Add(formulario);
            db.SaveChanges();

            // Evaluar automáticamente
            int puntaje = EvaluarSolicitud(formulario);
            string resultado = puntaje >= 80 ? "Apto" : puntaje >= 60 ? "Revisión Manual" : "No Apto";

            // Actualizar solicitud
            solicitud.PuntajeEvaluacion = puntaje;
            solicitud.ResultadoEvaluacion = resultado;
            solicitud.FechaEvaluacion = DateTime.Now;
            solicitud.Estado = resultado == "Apto" ? "Aprobada" : "En evaluación";
            db.SaveChanges();

            // Auditoría
            AuditoriaHelper.RegistrarAccion("Solicitud Adopción", "Adopcion",
                $"Solicitud creada para mascota ID: {mascotaId}, Resultado: {resultado}", usuario.UsuarioId);

            if (resultado == "Apto")
            {
                TempData["SuccessMessage"] = "¡Felicitaciones! Tu solicitud ha sido aprobada automáticamente.";
            }
            else
            {
                TempData["SuccessMessage"] = "Tu solicitud ha sido recibida y será evaluada pronto.";
            }

            return RedirectToAction("MisSolicitudes");
        }

        // Método privado de evaluación
        private int EvaluarSolicitud(FormularioAdopcionDetalle formulario)
        {
            int puntaje = 0;

            // Vivienda (20 puntos)
            if (formulario.ViviendaPropia == true) puntaje += 10;
            if (formulario.TieneJardin == true) puntaje += 5;
            if (formulario.PermisoMascotas == true) puntaje += 5;

            // Experiencia (20 puntos)
            if (formulario.ExperienciaPreviaConMascotas == true) puntaje += 15;
            if (formulario.TieneMascotasActualmente == true && formulario.MascotasEsterilizadas == true) puntaje += 5;

            // Disponibilidad (20 puntos)
            if (formulario.TiempoDisponibleDiario == "4+ horas" || formulario.TiempoDisponibleDiario == "Todo el día") puntaje += 20;
            else if (formulario.TiempoDisponibleDiario == "2-4 horas") puntaje += 15;
            else if (formulario.TiempoDisponibleDiario == "1-2 horas") puntaje += 10;
            else if (formulario.TiempoDisponibleDiario == "Menos de 1 hora") puntaje += 5;

            // Quién cuidará (10 puntos)
            if (!string.IsNullOrEmpty(formulario.QuienCuidaraMascota) && formulario.QuienCuidaraMascota.Length > 20) puntaje += 10;

            // Compromisos legales (20 puntos)
            if (formulario.AceptaEsterilizacion == true) puntaje += 5;
            if (formulario.AceptaVisitasSeguimiento == true) puntaje += 5;
            if (formulario.AceptaCondicionesLOBA == true) puntaje += 5;
            if (formulario.AceptaDevolucionSiNoPuedeAtender == true) puntaje += 5;

            // Compromiso (20 puntos)
            if (!string.IsNullOrEmpty(formulario.MotivoAdopcion) && formulario.MotivoAdopcion.Length > 50) puntaje += 10;
            if (!string.IsNullOrEmpty(formulario.QuePasaSiCambiaResidencia)) puntaje += 5;
            if (!string.IsNullOrEmpty(formulario.QuePasaSiProblemasComportamiento)) puntaje += 5;

            // Referencias (10 puntos adicionales)
            if (!string.IsNullOrEmpty(formulario.VeterinarioReferencia)) puntaje += 5;
            if (!string.IsNullOrEmpty(formulario.ReferenciaPersonal1) && !string.IsNullOrEmpty(formulario.TelefonoReferencia1)) puntaje += 5;

            return Math.Min(puntaje, 100);
        }

        // GET: Adopcion/MisSolicitudes
        public ActionResult MisSolicitudes()
        {
            var usuario = db.Usuarios.FirstOrDefault(u => u.Email == User.Identity.Name);

            var solicitudes = db.SolicitudAdopcion
                .Include(s => s.Mascotas)
                .Where(s => s.UsuarioId == usuario.UsuarioId)
                .OrderByDescending(s => s.FechaSolicitud)
                .ToList();

            return View(solicitudes);
        }

        // POST: Adopcion/CancelarSolicitud
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult CancelarSolicitud(int solicitudId, string motivoCancelacion)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"========== CANCELAR SOLICITUD ==========");
                System.Diagnostics.Debug.WriteLine($"SolicitudId: {solicitudId}, Motivo: {motivoCancelacion}");

                var usuario = db.Usuarios.FirstOrDefault(u => u.Email == User.Identity.Name);
                if (usuario == null)
                {
                    TempData["ErrorMessage"] = "Usuario no encontrado.";
                    return RedirectToAction("MisSolicitudes");
                }

                var solicitud = db.SolicitudAdopcion
                    .Include(s => s.Mascotas)
                    .FirstOrDefault(s => s.SolicitudId == solicitudId);

                if (solicitud == null)
                {
                    TempData["ErrorMessage"] = "Solicitud no encontrada.";
                    return RedirectToAction("MisSolicitudes");
                }

                // Verificar que la solicitud pertenezca al usuario
                if (solicitud.UsuarioId != usuario.UsuarioId)
                {
                    TempData["ErrorMessage"] = "No tienes permiso para cancelar esta solicitud.";
                    return RedirectToAction("MisSolicitudes");
                }

                // Verificar que la solicitud esté en estado Pendiente o En evaluación
                if (solicitud.Estado != "Pendiente" && solicitud.Estado != "En evaluación")
                {
                    TempData["ErrorMessage"] = "Solo se pueden cancelar solicitudes en estado Pendiente o En evaluación.";
                    return RedirectToAction("MisSolicitudes");
                }

                // ✅ SANITIZAR MOTIVO
                if (string.IsNullOrWhiteSpace(motivoCancelacion))
                    motivoCancelacion = "Cancelada por el usuario";
                else
                    motivoCancelacion = SanitizarString(motivoCancelacion, 500);

                // Actualizar estado
                string estadoAnterior = solicitud.Estado;
                solicitud.Estado = "Cancelada";
                solicitud.EstadoAdopcion = "Cancelada";
                solicitud.MotivoRechazo = motivoCancelacion;
                solicitud.FechaRespuesta = DateTime.Now;
                solicitud.Observaciones = $"Cancelada por el usuario el {DateTime.Now:dd/MM/yyyy HH:mm}. Motivo: {motivoCancelacion}";

                db.SaveChanges();

                // Auditoría
                AuditoriaHelper.RegistrarAccion("Cancelar Solicitud", "Adopcion",
                    $"Solicitud #{solicitud.SolicitudId} cancelada por el usuario. Mascota: {solicitud.MascotaId}, Motivo: {motivoCancelacion}",
                    usuario.UsuarioId);

                TempData["SuccessMessage"] = "Solicitud cancelada exitosamente.";
                return RedirectToAction("MisSolicitudes");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ ERROR en CancelarSolicitud: {ex.Message}");
                TempData["ErrorMessage"] = "Error al cancelar la solicitud. Intente nuevamente.";
                return RedirectToAction("MisSolicitudes");
            }
        }

        // GET: Adopcion/Evaluar/{id}
        [Authorize(Roles = "Administrador,Veterinario")]
        public ActionResult Evaluar(int? id)
        {
            if (id == null)
            {
                TempData["ErrorMessage"] = "ID de solicitud no especificado.";
                return RedirectToAction("SolicitudAdopcion", "Admin");
            }

            var solicitud = db.SolicitudAdopcion
                .Include(s => s.Mascotas)
                .Include(s => s.Usuarios)
                .Include(s => s.FormularioAdopcionDetalle)
                .Include(s => s.EvaluacionAdopcion)
                .FirstOrDefault(s => s.SolicitudId == id);

            if (solicitud == null)
            {
                TempData["ErrorMessage"] = "Solicitud no encontrada.";
                return RedirectToAction("SolicitudAdopcion", "Admin");
            }

            return View(solicitud);
        }

        // POST: Adopcion/EvaluarSolicitud
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Veterinario")]
        public ActionResult EvaluarSolicitud(int SolicitudId, int? PuntajeEvaluacion,
                                             string ResultadoEvaluacion, string Estado,
                                             string Observaciones, string MotivoRechazo,
                                             string action = "")
        {
            try
            {
                var solicitud = db.SolicitudAdopcion.Find(SolicitudId);
                if (solicitud == null)
                {
                    TempData["ErrorMessage"] = "Solicitud no encontrada.";
                    return RedirectToAction("SolicitudAdopcion", "Admin");
                }

                // Procesar acción específica
                if (!string.IsNullOrEmpty(action))
                {
                    if (action == "aprobar")
                    {
                        Estado = "Aprobada";
                        ResultadoEvaluacion = "Aprobada";
                    }
                    else if (action == "rechazar")
                    {
                        Estado = "Rechazada";
                        ResultadoEvaluacion = "Rechazada";
                    }
                }

                // Asignar valores
                solicitud.PuntajeEvaluacion = PuntajeEvaluacion;
                solicitud.ResultadoEvaluacion = ResultadoEvaluacion;
                solicitud.Estado = Estado;
                solicitud.Observaciones = Observaciones;
                solicitud.MotivoRechazo = MotivoRechazo;
                solicitud.FechaEvaluacion = DateTime.Now;

                // Obtener usuario evaluador
                var usuarioEvaluador = db.Usuarios.FirstOrDefault(u => u.Email == User.Identity.Name);
                if (usuarioEvaluador != null)
                {
                    solicitud.EvaluadoPor = usuarioEvaluador.UsuarioId;
                }

                // Actualizar estado de mascota si se aprueba
                if (Estado == "Aprobada" && solicitud.MascotaId > 0)
                {
                    var mascota = db.Mascotas.Find(solicitud.MascotaId);
                    if (mascota != null)
                    {
                        mascota.Estado = "En proceso de adopción";
                    }
                }

                // Guardar cambios PRIMERO
                db.SaveChanges();

                // Crear evaluación detallada (opcional, si no existe)
                var evaluacionExistente = db.EvaluacionAdopcion
                    .FirstOrDefault(e => e.SolicitudId == SolicitudId);

                if (evaluacionExistente == null && usuarioEvaluador != null)
                {
                    var evaluacion = new EvaluacionAdopcion
                    {
                        SolicitudId = SolicitudId,
                        PuntajeTotal = PuntajeEvaluacion,
                        Resultado = ResultadoEvaluacion,
                        Observaciones = Observaciones,
                        FechaEvaluacion = DateTime.Now,
                        EvaluadorId = usuarioEvaluador.UsuarioId
                    };
                    db.EvaluacionAdopcion.Add(evaluacion);
                    db.SaveChanges();
                }

                // Auditoría
                if (usuarioEvaluador != null)
                {
                    AuditoriaHelper.RegistrarAccion("Evaluación Adopción", "Adopcion",
                        $"Solicitud #{SolicitudId} evaluada. Resultado: {ResultadoEvaluacion}, Puntaje: {PuntajeEvaluacion}",
                        usuarioEvaluador.UsuarioId);
                }

                TempData["SuccessMessage"] = $"Evaluación guardada exitosamente. Estado: {Estado}";
                return RedirectToAction("SolicitudAdopcion", "Admin");
            }
            catch (Exception ex)
            {
                // Manejar errores de validación específicos
                var errorMsg = ex.Message;
                if (ex is System.Data.Entity.Validation.DbEntityValidationException dbEx)
                {
                    var errorMessages = dbEx.EntityValidationErrors
                        .SelectMany(x => x.ValidationErrors)
                        .Select(x => x.PropertyName + ": " + x.ErrorMessage);
                    errorMsg = string.Join("; ", errorMessages);
                }

                TempData["ErrorMessage"] = $"Error al guardar la evaluación: {errorMsg}";
                return RedirectToAction("Evaluar", new { id = SolicitudId });
            }
        }

        // GET: Adopcion/Seguimiento/{id}
        [Authorize(Roles = "Administrador,Veterinario")]
        public ActionResult Seguimiento(int? id)
        {
            if (id == null)
            {
                TempData["ErrorMessage"] = "ID de solicitud no especificado.";
                return RedirectToAction("SolicitudAdopcion", "Admin");
            }

            var solicitud = db.SolicitudAdopcion
                .Include(s => s.Mascotas)
                .Include(s => s.Usuarios)
                .Include(s => s.ContratoAdopcion)
                .Include(s => s.EvaluacionAdopcion)
                .FirstOrDefault(s => s.SolicitudId == id);

            if (solicitud == null)
            {
                TempData["ErrorMessage"] = "Solicitud no encontrada.";
                return RedirectToAction("SolicitudAdopcion", "Admin");
            }

            ViewBag.DireccionRefugio = "Av. Principal #123, Quito, Ecuador";
            return View(solicitud);
        }

        // POST: Adopcion/RegistrarSeguimiento
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador,Veterinario")]
        public ActionResult RegistrarSeguimiento(int SolicitudId, DateTime FechaVisita,
                                                string Responsable, string CondicionesHogar,
                                                string EstadoMascota, string Observaciones,
                                                System.Web.HttpPostedFileBase[] Fotos,
                                                DateTime? ProximaVisita)
        {
            try
            {
                var contrato = db.ContratoAdopcion
                    .FirstOrDefault(c => c.SolicitudId == SolicitudId);

                if (contrato == null)
                {
                    TempData["ErrorMessage"] = "No se encontró contrato para esta solicitud.";
                    return RedirectToAction("Seguimiento", new { id = SolicitudId });
                }

                // Procesar fotos
                string fotosEvidencia = "";
                if (Fotos != null && Fotos.Length > 0)
                {
                    var fotosList = new List<string>();
                    foreach (var foto in Fotos)
                    {
                        if (foto != null && foto.ContentLength > 0)
                        {
                            string fileName = System.IO.Path.GetFileName(foto.FileName);
                            string path = System.IO.Path.Combine(Server.MapPath("~/Uploads/Seguimientos/"), fileName);
                            foto.SaveAs(path);
                            fotosList.Add($"~/Uploads/Seguimientos/{fileName}");
                        }
                    }
                    fotosEvidencia = string.Join(",", fotosList);
                }

                var usuarioResponsable = db.Usuarios.FirstOrDefault(u => u.Email == User.Identity.Name);
                int responsableId = 0;
                if (usuarioResponsable != null)
                {
                    responsableId = usuarioResponsable.UsuarioId;
                }

                bool requiereIntervencion = (CondicionesHogar == "Malo" || EstadoMascota == "Requiere atención");

                var seguimiento = new SeguimientoAdopcion
                {
                    ContratoId = contrato.ContratoId,
                    FechaSeguimiento = FechaVisita,
                    TipoSeguimiento = "Visita Post-Adopción",
                    ResponsableSeguimiento = responsableId,
                    CondicionesVivienda = CondicionesHogar,
                    EstadoMascota = EstadoMascota,
                    Observaciones = Observaciones,
                    FotosEvidencia = fotosEvidencia,
                    ProximoSeguimiento = ProximaVisita,
                    RequiereIntervencion = requiereIntervencion
                };

                db.SeguimientoAdopcion.Add(seguimiento);
                db.SaveChanges();

                var usuario = db.Usuarios.FirstOrDefault(u => u.Email == User.Identity.Name);
                if (usuario != null)
                {
                    AuditoriaHelper.RegistrarAccion("Seguimiento Adopción", "Adopcion",
                        $"Seguimiento registrado para contrato #{contrato.ContratoId}", usuario.UsuarioId);
                }

                TempData["SuccessMessage"] = "Seguimiento registrado exitosamente.";
                return RedirectToAction("Seguimiento", new { id = SolicitudId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error al registrar seguimiento: {ex.Message}";
                return RedirectToAction("Seguimiento", new { id = SolicitudId });
            }
        }

        // GET: Adopcion/Contrato/{id}
        [Authorize(Roles = "Administrador,Veterinario,Usuario")]
        public ActionResult Contrato(int? id)
        {
            if (id == null)
            {
                TempData["ErrorMessage"] = "ID de solicitud no especificado.";
                return RedirectToAction("MisSolicitudes");
            }

            var solicitud = db.SolicitudAdopcion
                .Include(s => s.Mascotas)
                .Include(s => s.Usuarios)
                .FirstOrDefault(s => s.SolicitudId == id);

            if (solicitud == null)
            {
                TempData["ErrorMessage"] = "Solicitud no encontrada.";
                return RedirectToAction("MisSolicitudes");
            }

            var usuario = db.Usuarios.FirstOrDefault(u => u.Email == User.Identity.Name);
            if (solicitud.UsuarioId != usuario.UsuarioId && !User.IsInRole("Administrador") && !User.IsInRole("Veterinario"))
            {
                TempData["ErrorMessage"] = "No tienes permiso para ver este contrato.";
                return RedirectToAction("MisSolicitudes");
            }

            ViewBag.DireccionRefugio = "Av. Principal #123, Quito, Ecuador";
            return View(solicitud);
        }

        // GET: Adopcion/GenerarContrato/{id}
        [Authorize(Roles = "Administrador,Veterinario")]
        public ActionResult GenerarContrato(int? id)
        {
            if (id == null)
            {
                TempData["ErrorMessage"] = "ID de solicitud no especificado.";
                return RedirectToAction("SolicitudAdopcion", "Admin");
            }

            var solicitud = db.SolicitudAdopcion.Find(id);
            if (solicitud == null)
            {
                TempData["ErrorMessage"] = "Solicitud no encontrada.";
                return RedirectToAction("SolicitudAdopcion", "Admin");
            }

            if (solicitud.Estado != "Aprobada")
            {
                TempData["ErrorMessage"] = "Solo se pueden generar contratos para solicitudes aprobadas.";
                return RedirectToAction("Seguimiento", new { id = id });
            }

            var contratoExistente = db.ContratoAdopcion.Any(c => c.SolicitudId == id);
            if (contratoExistente)
            {
                TempData["ErrorMessage"] = "Ya existe un contrato para esta solicitud.";
                return RedirectToAction("Seguimiento", new { id = id });
            }

            var usuario = db.Usuarios.Find(solicitud.UsuarioId);
            var mascota = db.Mascotas.Find(solicitud.MascotaId);

            var contrato = new ContratoAdopcion
            {
                SolicitudId = solicitud.SolicitudId,
                NumeroContrato = $"CON-{DateTime.Now:yyyyMMdd}-{solicitud.SolicitudId:0000}",
                FechaContrato = DateTime.Now,
                AdoptanteNombre = usuario?.NombreCompleto ?? "No disponible",
                AdoptanteCedula = usuario?.Cedula ?? "No disponible",
                AdoptanteDireccion = usuario?.Direccion ?? "No disponible",
                AdoptanteTelefono = usuario?.Telefono ?? "No disponible",
                RepresentanteRefugioNombre = "Representante Refugio",
                RepresentanteRefugioCedula = "9999999999",
                MascotaNombre = mascota?.Nombre ?? "No disponible",
                MascotaEspecie = mascota?.Especie ?? "No disponible",
                MascotaMicrochip = mascota?.Microchip ?? "No disponible",
                Estado = "Activo",
                FechaFirmaAdoptante = DateTime.Now,
                FechaFirmaRefugio = DateTime.Now
            };

            db.ContratoAdopcion.Add(contrato);
            db.SaveChanges();

            if (mascota != null)
            {
                mascota.Estado = "Adoptada";
            }

            solicitud.FechaRespuesta = DateTime.Now;
            db.SaveChanges();

            var usuarioAuditoria = db.Usuarios.FirstOrDefault(u => u.Email == User.Identity.Name);
            if (usuarioAuditoria != null)
            {
                AuditoriaHelper.RegistrarAccion("Generar Contrato", "Adopcion",
                    $"Contrato #{contrato.NumeroContrato} generado para solicitud #{solicitud.SolicitudId}",
                    usuarioAuditoria.UsuarioId);
            }

            TempData["SuccessMessage"] = $"Contrato generado exitosamente. Número: {contrato.NumeroContrato}";
            return RedirectToAction("Seguimiento", new { id = id });
        }

        // GET: Adopcion/DescargarContrato/{id}
        [Authorize(Roles = "Administrador,Veterinario,Usuario")]
        public ActionResult DescargarContrato(int? id)
        {
            if (id == null)
            {
                TempData["ErrorMessage"] = "ID de solicitud no especificado.";
                return RedirectToAction("MisSolicitudes");
            }

            TempData["InfoMessage"] = "Para descargar el contrato, use la opción 'Imprimir' en su navegador y seleccione 'Guardar como PDF'.";
            return RedirectToAction("Contrato", new { id = id });
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