using MVCMASCOTAS.Helpers;
using MVCMASCOTAS.Models;
using MVCMASCOTAS.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace MVCMASCOTAS.Controllers
{
    public class HomeController : Controller
    {
        private RefugioMascotasDBEntities db = new RefugioMascotasDBEntities();

        [AllowAnonymous]
        public ActionResult Index()
        {
            try
            {
                // =============================================
                // 1. MASCOTAS DISPONIBLES PARA ADOPCIÓN
                // =============================================
                var mascotasDisponibles = db.Mascotas
                    .Where(m => m.Estado == "Disponible para adopción" && m.Activo == true)
                    .OrderByDescending(m => m.FechaIngreso)
                    .Take(6)
                    .ToList();

                ViewBag.MascotasDisponibles = mascotasDisponibles;

                // =============================================
                // 2. MASCOTAS PERDIDAS RECIENTES
                // =============================================
                var mascotasPerdidasRecientes = db.MascotasPerdidas
                    .Where(m => m.Estado == "Perdida")
                    .OrderByDescending(m => m.FechaPublicacion)
                    .Take(6)
                    .ToList();

                ViewBag.MascotasPerdidasRecientes = mascotasPerdidasRecientes;

                // =============================================
                // 3. 🆕 CAMPAÑAS DE ADOPCIÓN ACTIVAS
                // =============================================
                var campanasActivas = db.CampaniaAdopcion
                    .Where(c => c.Estado == true && c.Fecha >= DateTime.Now)
                    .OrderBy(c => c.Fecha)
                    .Take(3)
                    .ToList();

                ViewBag.CampanasActivas = campanasActivas;

                // =============================================
                // 4. ESTADÍSTICAS GENERALES
                // =============================================
                ViewBag.TotalMascotas = db.Mascotas.Count(m => m.Activo == true);
                ViewBag.MascotasAdoptadas = db.Mascotas.Count(m => m.Estado == "Adoptada");
                ViewBag.VoluntariosActivos = db.Usuarios.Count(u => u.Activo == true);
                ViewBag.TotalDonaciones = db.Donaciones.Count(d => d.Estado == "Completada");
                ViewBag.TotalMascotasDisponibles = db.Mascotas.Count(m => m.Estado == "Disponible para adopción" && m.Activo == true);
                ViewBag.TotalMascotasPerdidasActivas = db.MascotasPerdidas.Count(m => m.Estado == "Perdida");

                // =============================================
                // 5. ÚLTIMAS DONACIONES - CORREGIDO
                // =============================================
                var ultimasDonaciones = db.Donaciones
                    .Where(d => d.Estado == "Completada")
                    .OrderByDescending(d => d.FechaDonacion)
                    .Take(5)
                    .ToList()
                    .Select(d =>
                    {
                        string nombreDonante = "Donante Anónimo";

                        if (d.Anonima != true && d.UsuarioId.HasValue)
                        {
                            var usuario = db.Usuarios.Find(d.UsuarioId.Value);
                            if (usuario != null)
                            {
                                nombreDonante = usuario.NombreCompleto;
                            }
                        }

                        return new UltimaDonacionViewModel
                        {
                            Donante = nombreDonante,
                            TipoDonacion = d.TipoDonacion,
                            FechaDonacion = d.FechaDonacion ?? DateTime.Now,
                            Monto = d.Monto,
                            Mensaje = d.Mensaje ?? "Sin mensaje"
                        };
                    })
                    .ToList();

                ViewBag.UltimasDonaciones = ultimasDonaciones;

                // =============================================
                // 6. ÚLTIMOS RESCATES COMPLETADOS
                // =============================================
                var ultimosRescates = db.ReportesRescate
                    .Where(r => r.Estado == "Rescatado")
                    .OrderByDescending(r => r.FechaRescate)
                    .Take(3)
                    .Select(r => new
                    {
                        r.TipoAnimal,
                        r.UbicacionReporte,
                        FechaRescate = r.FechaRescate ?? r.FechaReporte,
                        r.DescripcionSituacion
                    })
                    .ToList();

                ViewBag.UltimosRescates = ultimosRescates;

                // =============================================
                // 7. TESTIMONIOS DE ADOPCIONES EXITOSAS
                // =============================================
                var adopcionesExitosas = db.SolicitudAdopcion
                    .Where(s => s.Estado == "Completada")
                    .OrderByDescending(s => s.FechaRespuesta)
                    .Take(3)
                    .Select(s => new
                    {
                        Mascota = db.Mascotas.FirstOrDefault(m => m.MascotaId == s.MascotaId),
                        Adoptante = db.Usuarios.FirstOrDefault(u => u.UsuarioId == s.UsuarioId),
                        FechaAdopcion = s.FechaRespuesta ?? s.FechaSolicitud
                    })
                    .Where(x => x.Mascota != null && x.Adoptante != null)
                    .ToList();

                ViewBag.AdopcionesExitosas = adopcionesExitosas;

                // =============================================
                // 8. ACTIVIDADES PRÓXIMAS DE VOLUNTARIADO
                // =============================================
                var proximasActividades = db.Actividades
                    .Where(a => a.Estado == "Programada" && a.FechaActividad >= DateTime.Now)
                    .OrderBy(a => a.FechaActividad)
                    .Take(3)
                    .ToList();

                ViewBag.ProximasActividades = proximasActividades;

                return View();
            }
            catch (Exception ex)
            {
                // Log del error
                AuditoriaHelper.RegistrarAccion("Error Home Index", "Home",
                    $"Error: {ex.Message}", null);

                // Si hay error con la base de datos, mostrar página básica
                ViewBag.Error = "Error cargando datos. Por favor intenta más tarde.";
                ViewBag.MascotasDisponibles = new List<Mascotas>();
                ViewBag.MascotasPerdidasRecientes = new List<MascotasPerdidas>();
                ViewBag.UltimasDonaciones = new List<UltimaDonacionViewModel>();
                ViewBag.CampanasActivas = new List<CampaniaAdopcion>();

                return View();
            }
        }

        // =============================================
        // VISTA DE TÉRMINOS Y CONDICIONES
        // =============================================
        [AllowAnonymous]
        public ActionResult Terminos()
        {
            ViewBag.Title = "Términos y Condiciones";

            // Cargar términos desde configuración si existen
            var terminos = db.ConfiguracionSistema
                .FirstOrDefault(c => c.Clave == "TerminosCondiciones");

            if (terminos != null)
            {
                ViewBag.TerminosContenido = terminos.Valor;
            }
            else
            {
                ViewBag.TerminosContenido = "<p>Contenido de términos y condiciones no configurado.</p>";
            }

            return View();
        }

        // =============================================
        // VISTA ACERCA DE NOSOTROS
        // =============================================
        [AllowAnonymous]
        public ActionResult About()
        {
            try
            {
                // Estadísticas para la página About
                ViewBag.FundacionAnio = 2015; // Año de fundación
                ViewBag.TotalVoluntarios = db.Usuarios
                    .Count(u => u.Activo == true);
                ViewBag.MascotasRescatadasAnio = db.Mascotas
                    .Count(m => m.FechaIngreso.HasValue &&
                                m.FechaIngreso.Value.Year == DateTime.Now.Year);
                ViewBag.ComunidadActiva = db.Usuarios
                    .Count(u => u.Activo == true && u.FechaRegistro.HasValue &&
                                u.FechaRegistro.Value > DateTime.Now.AddMonths(-6));

                return View();
            }
            catch (Exception ex)
            {
                AuditoriaHelper.RegistrarAccion("Error About", "Home",
                    $"Error: {ex.Message}", null);
                return View();
            }
        }

        // =============================================
        // VISTA DE CONTACTO
        // =============================================
        [AllowAnonymous]
        public ActionResult Contact()
        {
            // Obtener información de contacto desde configuración
            var telefonoContacto = db.ConfiguracionSistema
                .FirstOrDefault(c => c.Clave == "TelefonoContacto");
            var emailContacto = db.ConfiguracionSistema
                .FirstOrDefault(c => c.Clave == "EmailContacto");
            var direccionContacto = db.ConfiguracionSistema
                .FirstOrDefault(c => c.Clave == "DireccionContacto");
            var horarioAtencion = db.ConfiguracionSistema
                .FirstOrDefault(c => c.Clave == "HorarioAtencion");

            ViewBag.Telefono = telefonoContacto?.Valor ?? "099-123-4567";
            ViewBag.Email = emailContacto?.Valor ?? "contacto@refugiomascotas.org";
            ViewBag.Direccion = direccionContacto?.Valor ?? "Av. Amazonas N35-38, Quito";
            ViewBag.Horario = horarioAtencion?.Valor ?? "Lunes a Viernes: 8:00 AM - 6:00 PM";

            return View();
        }

        // =============================================
        // VISTA DE SERVICIOS - ✅ OPTIMIZADO
        // =============================================
        [AllowAnonymous]
        public ActionResult Services()
        {
            try
            {
                // Servicios disponibles con validaciones para evitar errores
                var servicios = new List<ServicioViewModel>
                {
                    new ServicioViewModel
                    {
                        Titulo = "Adopción Responsable",
                        Descripcion = "Conectamos mascotas rescatadas con familias comprometidas. Proceso de adopción supervisado y seguimiento post-adopción.",
                        Icono = "fas fa-paw",
                        Estadistica = GetAdopcionesCount() + " adopciones exitosas"
                    },
                    new ServicioViewModel
                    {
                        Titulo = "Rescate y Rehabilitación",
                        Descripcion = "Equipo de rescatistas y veterinarios dedicados a salvar, tratar y rehabilitar mascotas en situación de riesgo.",
                        Icono = "fas fa-ambulance",
                        Estadistica = GetMascotasCount() + " mascotas rescatadas"
                    },
                    new ServicioViewModel
                    {
                        Titulo = "Atención Veterinaria",
                        Descripcion = "Consultas, vacunación, esterilización y tratamientos médicos para mascotas del refugio y comunidad.",
                        Icono = "fas fa-stethoscope",
                        Estadistica = GetConsultasCount() + " consultas realizadas"
                    },
                    new ServicioViewModel
                    {
                        Titulo = "Programa de Voluntariado",
                        Descripcion = "Oportunidades para ayudar en cuidado, paseos, socialización y actividades del refugio.",
                        Icono = "fas fa-hands-helping",
                        Estadistica = GetVoluntariosCount() + " voluntarios activos"
                    },
                    new ServicioViewModel
                    {
                        Titulo = "Donaciones y Apadrinamiento",
                        Descripcion = "Sistema de donaciones recurrentes y apadrinamiento para apoyar el sostenimiento de las mascotas.",
                        Icono = "fas fa-hand-holding-heart",
                        Estadistica = "$" + GetDonacionesTotal() + " recaudados"
                    },
                    new ServicioViewModel
                    {
                        Titulo = "Educación Comunitaria",
                        Descripcion = "Talleres y charlas sobre tenencia responsable, cuidado animal y prevención del maltrato.",
                        Icono = "fas fa-graduation-cap",
                        Estadistica = "10+ talleres mensuales"
                    }
                };

                ViewBag.Servicios = servicios;
                ViewBag.Telefono = "(02) 245-6789";

                return View();
            }
            catch (Exception ex)
            {
                AuditoriaHelper.RegistrarAccion("Error Services", "Home",
                    $"Error: {ex.Message}", null);

                // En caso de error, devolver lista vacía
                ViewBag.Servicios = new List<ServicioViewModel>();
                ViewBag.Telefono = "(02) 245-6789";

                return View();
            }
        }


        // =============================================
        // ✅ MÉTODOS AUXILIARES PARA SERVICES
        // =============================================
        private int GetAdopcionesCount()
        {
            try
            {
                return db.Mascotas.Count(m => m.Estado == "Adoptada");
            }
            catch
            {
                return 0;
            }
        }

        private int GetMascotasCount()
        {
            try
            {
                return db.Mascotas.Count();
            }
            catch
            {
                return 0;
            }
        }

        private int GetConsultasCount()
        {
            try
            {
                return db.HistorialMedico.Count();
            }
            catch
            {
                return 0;
            }
        }

        private int GetVoluntariosCount()
        {
            try
            {
                return db.Usuarios.Count(u => u.Activo == true);
            }
            catch
            {
                return 0;
            }
        }

        private string GetDonacionesTotal()
        {
            try
            {
                var total = db.Donaciones
                    .Where(d => d.Estado == "Completada")
                    .Sum(d => (decimal?)d.Monto) ?? 0;

                return total.ToString("N0");
            }
            catch
            {
                return "0";
            }
        }

        // =============================================
        // MÉTODO PARA OBTENER ESTADÍSTICAS EN TIEMPO REAL (AJAX)
        // =============================================
        [AllowAnonymous]
        [HttpPost]
        public JsonResult ObtenerEstadisticas()
        {
            try
            {
                var estadisticas = new
                {
                    TotalMascotas = db.Mascotas.Count(m => m.Activo == true),
                    MascotasAdoptadas = db.Mascotas.Count(m => m.Estado == "Adoptada"),
                    MascotasDisponibles = db.Mascotas.Count(m => m.Estado == "Disponible para adopción" && m.Activo == true),
                    MascotasPerdidas = db.MascotasPerdidas.Count(m => m.Estado == "Perdida"),
                    TotalDonaciones = db.Donaciones.Count(d => d.Estado == "Completada"),
                    MontoTotalDonaciones = db.Donaciones.Where(d => d.Estado == "Completada").Sum(d => (decimal?)d.Monto) ?? 0,
                    VoluntariosActivos = db.Usuarios.Count(u => u.Activo == true),
                    SolicitudesPendientes = db.SolicitudAdopcion.Count(s => s.Estado == "Pendiente"),
                    FechaActualizacion = DateTime.Now.ToString("dd/MM/yyyy HH:mm")
                };

                return Json(new { success = true, data = estadisticas });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        // =============================================
        // MÉTODO PARA OBTENER MASCOTAS PERDIDAS (AJAX)
        // =============================================
        [AllowAnonymous]
        [HttpPost]
        public JsonResult ObtenerMascotasPerdidas()
        {
            try
            {
                var mascotas = db.MascotasPerdidas
                    .Where(m => m.Estado == "Perdida")
                    .OrderByDescending(m => m.FechaPublicacion)
                    .Take(12)
                    .Select(m => new
                    {
                        m.MascotaPerdidaId,
                        m.NombreMascota,
                        m.Especie,
                        m.Raza,
                        m.UbicacionPerdida,
                        m.FechaPerdida,
                        m.Recompensa,
                        ImagenUrl = m.ImagenMascota != null ?
                            "data:image/jpeg;base64," + Convert.ToBase64String(m.ImagenMascota) :
                            "/Content/images/default-pet.jpg"
                    })
                    .ToList();

                return Json(new { success = true, data = mascotas });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        // =============================================
        // MÉTODO PARA BUSCAR MASCOTAS (BÚSQUEDA HOME)
        // =============================================
        [AllowAnonymous]
        [HttpPost]
        public JsonResult BuscarMascotas(string termino, string tipo = "todos")
        {
            try
            {
                var resultados = new List<object>();

                if (tipo == "perdidas" || tipo == "todos")
                {
                    var mascotasPerdidas = db.MascotasPerdidas
                        .Where(m => m.Estado == "Perdida" &&
                                   (m.NombreMascota.Contains(termino) ||
                                    m.UbicacionPerdida.Contains(termino) ||
                                    m.CaracteristicasDistintivas.Contains(termino)))
                        .Select(m => new
                        {
                            Tipo = "Perdida",
                            Nombre = m.NombreMascota,
                            m.Especie,
                            Ubicacion = m.UbicacionPerdida,
                            Fecha = m.FechaPerdida,
                            Link = Url.Action("DetalleMascotaPerdida", "Rescate", new { id = m.MascotaPerdidaId })
                        })
                        .Take(5)
                        .ToList();

                    resultados.AddRange(mascotasPerdidas);
                }

                if (tipo == "adopcion" || tipo == "todos")
                {
                    var mascotasAdopcion = db.Mascotas
                        .Where(m => m.Estado == "Disponible para adopción" && m.Activo == true &&
                                   (m.Nombre.Contains(termino) ||
                                    m.Especie.Contains(termino) ||
                                    m.Raza.Contains(termino)))
                        .Select(m => new
                        {
                            Tipo = "Adopción",
                            Nombre = m.Nombre,
                            m.Especie,
                            m.Raza,
                            Edad = m.EdadAproximada,
                            Link = Url.Action("Detalle", "Mascotas", new { id = m.MascotaId })
                        })
                        .Take(5)
                        .ToList();

                    resultados.AddRange(mascotasAdopcion);
                }

                return Json(new { success = true, resultados = resultados.Take(10) });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        // POST: Home/EnviarContacto
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public JsonResult EnviarContacto(string nombre, string email, string telefono,
                                         string asunto, string mensaje, bool newsletter)
        {
            try
            {
                // Validar datos
                if (string.IsNullOrEmpty(nombre) || string.IsNullOrEmpty(email) ||
                    string.IsNullOrEmpty(asunto) || string.IsNullOrEmpty(mensaje))
                {
                    return Json(new { success = false, message = "Por favor completa todos los campos requeridos." });
                }

                // Registrar en auditoría
                AuditoriaHelper.RegistrarAccion("Formulario Contacto", "Home",
                    $"Nuevo contacto: {nombre} - {asunto}", null);

                // Enviar email de notificación (opcional)
                try
                {
                    string subject = $"Nuevo mensaje de contacto: {asunto}";
                    string body = $@"
                        <h3>Nuevo mensaje de contacto</h3>
                        <p><strong>Nombre:</strong> {nombre}</p>
                        <p><strong>Email:</strong> {email}</p>
                        <p><strong>Teléfono:</strong> {telefono}</p>
                        <p><strong>Asunto:</strong> {asunto}</p>
                        <p><strong>Mensaje:</strong></p>
                        <p>{mensaje}</p>
                        <p><strong>Desea newsletter:</strong> {(newsletter ? "Sí" : "No")}</p>
                    ";

                    // Obtener email del refugio
                    var emailRefugio = db.ConfiguracionSistema
                        .FirstOrDefault(c => c.Clave == "EmailContacto")?.Valor
                        ?? "contacto@refugiomascotas.org";

                    _ = EmailHelper.SendNotificationAsync(emailRefugio, subject, body);
                }
                catch
                {
                    // No bloquear si falla el email
                }

                return Json(new { success = true, message = "Mensaje enviado exitosamente. Te contactaremos pronto." });
            }
            catch (Exception ex)
            {
                AuditoriaHelper.RegistrarAccion("Error Contacto", "Home",
                    $"Error: {ex.Message}", null);
                return Json(new { success = false, message = "Error al enviar el mensaje. Intenta nuevamente." });
            }
        }

        // =============================================
        // VIEWMODELS INTERNOS
        // =============================================

        public class UltimaDonacionViewModel
        {
            public string Donante { get; set; }
            public string TipoDonacion { get; set; }
            public DateTime FechaDonacion { get; set; }
            public decimal Monto { get; set; }
            public string Mensaje { get; set; }
        }

        public class ServicioViewModel
        {
            public string Titulo { get; set; }
            public string Descripcion { get; set; }
            public string Icono { get; set; }
            public string Estadistica { get; set; }
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