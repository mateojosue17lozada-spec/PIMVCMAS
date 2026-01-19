using System;
using System.Linq;
using System.Web.Mvc;
using MVCMASCOTAS.Helpers;
using MVCMASCOTAS.Models;
using MVCMASCOTAS.Models.ViewModels;

namespace MVCMASCOTAS.Controllers
{
    [Authorize]
    public class AdopcionController : Controller
    {
        private RefugioMascotasEntities db = new RefugioMascotasEntities();

        // GET: Adopcion/Solicitar/5
        public ActionResult Solicitar(int mascotaId)
        {
            var mascota = db.Mascotas.Find(mascotaId);

            if (mascota == null || mascota.Estado != "Disponible para adopción")
            {
                TempData["ErrorMessage"] = "Esta mascota no está disponible para adopción.";
                return RedirectToAction("Index", "Mascotas");
            }

            // Obtener usuario actual
            var usuario = db.Usuarios.FirstOrDefault(u => u.Email == User.Identity.Name);

            // Verificar si ya tiene una solicitud pendiente
            var solicitudExistente = db.SolicitudAdopcion
                .Any(s => s.MascotaId == mascotaId && s.UsuarioId == usuario.UsuarioId &&
                          (s.Estado == "Pendiente" || s.Estado == "En evaluación" || s.Estado == "Aprobada"));

            if (solicitudExistente)
            {
                TempData["ErrorMessage"] = "Ya tienes una solicitud activa para esta mascota.";
                return RedirectToAction("MisSolicitudes");
            }

            // Preparar vista con datos de la mascota
            ViewBag.Mascota = mascota;
            ViewBag.MascotaId = mascotaId;
            ViewBag.ImagenBase64 = mascota.ImagenPrincipal != null
                ? ImageHelper.GetImageDataUri(mascota.ImagenPrincipal)
                : null;

            return View(new FormularioAdopcionViewModel());
        }

        // POST: Adopcion/Solicitar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Solicitar(int mascotaId, FormularioAdopcionViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var mascota = db.Mascotas.Find(mascotaId);
                ViewBag.Mascota = mascota;
                ViewBag.MascotaId = mascotaId;
                ViewBag.ImagenBase64 = mascota?.ImagenPrincipal != null
                    ? ImageHelper.GetImageDataUri(mascota.ImagenPrincipal)
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

            // Crear formulario de detalle
            var formulario = new FormularioAdopcionDetalle
            {
                SolicitudId = solicitud.SolicitudId,
                TipoVivienda = model.TipoVivienda,
                ViviendaPropia = model.ViviendaPropia,
                TieneJardin = model.TieneJardin,
                TamanioJardin = model.TamanioJardin,
                PermisoMascotas = model.PermisoMascotas,
                ExperienciaPreviaConMascotas = model.ExperienciaPreviaConMascotas,
                DetalleExperiencia = model.DetalleExperiencia,
                TieneMascotasActualmente = model.TieneMascotasActualmente,
                CantidadPerros = model.CantidadPerros,
                CantidadGatos = model.CantidadGatos,
                MascotasEsterilizadas = model.MascotasEsterilizadas,
                TiempoDisponibleDiario = model.TiempoDisponibleDiario,
                PersonasEnCasa = model.PersonasEnCasa,
                HayNinios = model.HayNinios,
                EdadesNinios = model.EdadesNinios,
                AceptaEsterilizacion = model.AceptaEsterilizacion,
                AceptaVisitasSeguimiento = model.AceptaVisitasSeguimiento,
                AceptaCondicionesLOBA = model.AceptaCondicionesLOBA,
                AceptaDevolucionSiNoPuedeAtender = model.AceptaDevolucionSiNoPuedeAtender,
                MotivoAdopcion = model.MotivoAdopcion,
                QuePasaSiCambiaResidencia = model.QuePasaSiCambiaResidencia,
                QuePasaSiProblemasComportamiento = model.QuePasaSiProblemasComportamiento,
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

            // Enviar email
            var mascotaInfo = db.Mascotas.Find(mascotaId);
            _ = EmailHelper.SendAdoptionRequestReceivedAsync(usuario.Email, usuario.NombreCompleto, mascotaInfo.Nombre);

            if (resultado == "Apto")
            {
                _ = EmailHelper.SendAdoptionApprovedAsync(usuario.Email, usuario.NombreCompleto, mascotaInfo.Nombre);
                TempData["SuccessMessage"] = "¡Felicitaciones! Tu solicitud ha sido aprobada automáticamente. Revisa tu email.";
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
            if (formulario.TiempoDisponibleDiario == "4+ horas") puntaje += 20;
            else if (formulario.TiempoDisponibleDiario == "2-4 horas") puntaje += 15;
            else if (formulario.TiempoDisponibleDiario == "1-2 horas") puntaje += 10;

            // Compromisos legales (20 puntos)
            if (formulario.AceptaEsterilizacion == true) puntaje += 5;
            if (formulario.AceptaVisitasSeguimiento == true) puntaje += 5;
            if (formulario.AceptaCondicionesLOBA == true) puntaje += 5;
            if (formulario.AceptaDevolucionSiNoPuedeAtender == true) puntaje += 5;

            // Compromiso (20 puntos)
            if (!string.IsNullOrEmpty(formulario.MotivoAdopcion) && formulario.MotivoAdopcion.Length > 50) puntaje += 10;
            if (!string.IsNullOrEmpty(formulario.QuePasaSiCambiaResidencia)) puntaje += 5;
            if (!string.IsNullOrEmpty(formulario.QuePasaSiProblemasComportamiento)) puntaje += 5;

            return puntaje;
        }

        // GET: Adopcion/MisSolicitudes
        public ActionResult MisSolicitudes()
        {
            var usuario = db.Usuarios.FirstOrDefault(u => u.Email == User.Identity.Name);

            var solicitudes = db.SolicitudAdopcion
                .Where(s => s.UsuarioId == usuario.UsuarioId)
                .OrderByDescending(s => s.FechaSolicitud)
                .ToList();

            return View(solicitudes);
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