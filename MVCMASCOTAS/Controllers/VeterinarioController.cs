using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using MVCMASCOTAS.Helpers;
using MVCMASCOTAS.Models;

namespace MVCMASCOTAS.Controllers
{
    [AuthorizeRoles("Veterinario", "Administrador")]
    public class VeterinarioController : Controller
    {
        private RefugioMascotasEntities db = new RefugioMascotasEntities();

        // GET: Veterinario/Dashboard
        public ActionResult Dashboard()
        {
            // Estadísticas
            ViewBag.MascotasEnTratamiento = db.Mascotas.Count(m => m.Estado == "En tratamiento" && m.Activo);
            ViewBag.TratamientosActivos = db.Tratamientos.Count(t => t.Estado == "En curso");
            ViewBag.CitasHoy = db.HistorialMedico
                .Count(h => h.FechaRegistro.Year == DateTime.Now.Year &&
                           h.FechaRegistro.Month == DateTime.Now.Month &&
                           h.FechaRegistro.Day == DateTime.Now.Day);

            // Mascotas que requieren atención
            var mascotasAtencion = db.Mascotas
                .Where(m => m.Estado == "En tratamiento" && m.Activo)
                .OrderByDescending(m => m.FechaIngreso)
                .Take(10)
                .ToList();

            ViewBag.MascotasAtencion = mascotasAtencion;

            // Tratamientos pendientes
            var tratamientosPendientes = db.Tratamientos
                .Where(t => t.Estado == "En curso")
                .OrderBy(t => t.FechaInicio)
                .Take(10)
                .ToList();

            ViewBag.TratamientosPendientes = tratamientosPendientes;

            return View();
        }

        // GET: Veterinario/Mascotas
        public ActionResult Mascotas(string estado, int page = 1)
        {
            int pageSize = 20;
            var query = db.Mascotas.Where(m => m.Activo);

            if (!string.IsNullOrEmpty(estado) && estado != "Todos")
            {
                query = query.Where(m => m.Estado == estado);
            }

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

        // GET: Veterinario/HistorialMedico/5
        public ActionResult HistorialMedico(int mascotaId)
        {
            var mascota = db.Mascotas.Find(mascotaId);

            if (mascota == null)
            {
                return HttpNotFound();
            }

            ViewBag.Mascota = mascota;
            ViewBag.ImagenBase64 = mascota.ImagenPrincipal != null
                ? ImageHelper.GetImageDataUri(mascota.ImagenPrincipal)
                : null;

            // Historial médico
            var historial = db.HistorialMedico
                .Where(h => h.MascotaId == mascotaId)
                .OrderByDescending(h => h.FechaRegistro)
                .ToList();

            ViewBag.Historial = historial;

            // Vacunas
            var vacunas = db.MascotaVacunas
                .Where(v => v.MascotaId == mascotaId)
                .Include(v => v.Vacunas)
                .OrderByDescending(v => v.FechaAplicacion)
                .ToList();

            ViewBag.Vacunas = vacunas;

            // Tratamientos
            var tratamientos = db.Tratamientos
                .Where(t => t.MascotaId == mascotaId)
                .OrderByDescending(t => t.FechaInicio)
                .ToList();

            ViewBag.Tratamientos = tratamientos;

            return View();
        }

        // POST: Veterinario/RegistrarConsulta
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RegistrarConsulta(int mascotaId, string diagnostico, string tratamientoRecetado,
            decimal? peso, decimal? temperatura, string observaciones)
        {
            if (string.IsNullOrEmpty(diagnostico))
            {
                TempData["ErrorMessage"] = "El diagnóstico es requerido";
                return RedirectToAction("HistorialMedico", new { mascotaId });
            }

            var usuario = db.Usuarios.FirstOrDefault(u => u.Email == User.Identity.Name);

            var historial = new HistorialMedico
            {
                MascotaId = mascotaId,
                VeterinarioId = usuario.UsuarioId,
                TipoRegistro = "Consulta",
                Diagnostico = diagnostico,
                TratamientoRecetado = tratamientoRecetado,
                Peso = peso,
                Temperatura = temperatura,
                Observaciones = observaciones,
                FechaRegistro = DateTime.Now
            };

            db.HistorialMedico.Add(historial);
            db.SaveChanges();

            // Auditoría
            AuditoriaHelper.RegistrarAccion("Registrar Consulta", "Veterinario",
                $"Mascota ID: {mascotaId}, Diagnóstico: {diagnostico}", usuario.UsuarioId);

            TempData["SuccessMessage"] = "Consulta registrada exitosamente";
            return RedirectToAction("HistorialMedico", new { mascotaId });
        }

        // POST: Veterinario/RegistrarVacuna
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RegistrarVacuna(int mascotaId, int vacunaId, DateTime? fechaProximaDosis)
        {
            var usuario = db.Usuarios.FirstOrDefault(u => u.Email == User.Identity.Name);

            var mascotaVacuna = new MascotaVacunas
            {
                MascotaId = mascotaId,
                VacunaId = vacunaId,
                FechaAplicacion = DateTime.Now,
                VeterinarioId = usuario.UsuarioId,
                FechaProximaDosis = fechaProximaDosis
            };

            db.MascotaVacunas.Add(mascotaVacuna);
            db.SaveChanges();

            // Registrar en historial médico
            var vacuna = db.Vacunas.Find(vacunaId);
            var historial = new HistorialMedico
            {
                MascotaId = mascotaId,
                VeterinarioId = usuario.UsuarioId,
                TipoRegistro = "Vacunación",
                Diagnostico = $"Vacuna aplicada: {vacuna.NombreVacuna}",
                FechaRegistro = DateTime.Now
            };

            db.HistorialMedico.Add(historial);
            db.SaveChanges();

            // Auditoría
            AuditoriaHelper.RegistrarAccion("Registrar Vacuna", "Veterinario",
                $"Mascota ID: {mascotaId}, Vacuna: {vacuna.NombreVacuna}", usuario.UsuarioId);

            TempData["SuccessMessage"] = "Vacuna registrada exitosamente";
            return RedirectToAction("HistorialMedico", new { mascotaId });
        }

        // GET: Veterinario/IniciarTratamiento/5
        public ActionResult IniciarTratamiento(int mascotaId)
        {
            var mascota = db.Mascotas.Find(mascotaId);

            if (mascota == null)
            {
                return HttpNotFound();
            }

            ViewBag.Mascota = mascota;
            ViewBag.MascotaId = mascotaId;

            return View();
        }

        // POST: Veterinario/IniciarTratamiento
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult IniciarTratamiento(int mascotaId, string tipoTratamiento, string descripcion,
            string medicamentos, int duracionEstimadaDias, decimal? costoEstimado)
        {
            if (string.IsNullOrEmpty(descripcion))
            {
                TempData["ErrorMessage"] = "La descripción es requerida";
                return RedirectToAction("IniciarTratamiento", new { mascotaId });
            }

            var usuario = db.Usuarios.FirstOrDefault(u => u.Email == User.Identity.Name);

            var tratamiento = new Tratamientos
            {
                MascotaId = mascotaId,
                TipoTratamiento = tipoTratamiento,
                Descripcion = descripcion,
                Medicamentos = medicamentos,
                FechaInicio = DateTime.Now,
                DuracionEstimadaDias = duracionEstimadaDias,
                CostoEstimado = costoEstimado,
                VeterinarioResponsableId = usuario.UsuarioId,
                Estado = "En curso"
            };

            db.Tratamientos.Add(tratamiento);

            // Actualizar estado de la mascota
            var mascota = db.Mascotas.Find(mascotaId);
            mascota.Estado = "En tratamiento";

            // Registrar en historial
            var historial = new HistorialMedico
            {
                MascotaId = mascotaId,
                VeterinarioId = usuario.UsuarioId,
                TipoRegistro = "Inicio Tratamiento",
                Diagnostico = $"{tipoTratamiento}: {descripcion}",
                TratamientoRecetado = medicamentos,
                FechaRegistro = DateTime.Now
            };

            db.HistorialMedico.Add(historial);
            db.SaveChanges();

            // Auditoría
            AuditoriaHelper.RegistrarAccion("Iniciar Tratamiento", "Veterinario",
                $"Mascota ID: {mascotaId}, Tipo: {tipoTratamiento}", usuario.UsuarioId);

            TempData["SuccessMessage"] = "Tratamiento iniciado exitosamente";
            return RedirectToAction("HistorialMedico", new { mascotaId });
        }

        // POST: Veterinario/FinalizarTratamiento
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult FinalizarTratamiento(int tratamientoId, string resultados)
        {
            var tratamiento = db.Tratamientos.Find(tratamientoId);

            if (tratamiento == null)
            {
                return HttpNotFound();
            }

            tratamiento.FechaFin = DateTime.Now;
            tratamiento.Estado = "Completado";
            tratamiento.Resultados = resultados;

            // Verificar si hay más tratamientos activos para esta mascota
            var otrosTratamientos = db.Tratamientos
                .Any(t => t.MascotaId == tratamiento.MascotaId &&
                         t.Estado == "En curso" &&
                         t.TratamientoId != tratamientoId);

            if (!otrosTratamientos)
            {
                // Si no hay más tratamientos, cambiar estado de mascota
                var mascota = db.Mascotas.Find(tratamiento.MascotaId);
                mascota.Estado = "Disponible para adopción";
            }

            var usuario = db.Usuarios.FirstOrDefault(u => u.Email == User.Identity.Name);

            // Registrar en historial
            var historial = new HistorialMedico
            {
                MascotaId = tratamiento.MascotaId,
                VeterinarioId = usuario.UsuarioId,
                TipoRegistro = "Fin Tratamiento",
                Diagnostico = $"Tratamiento completado: {tratamiento.TipoTratamiento}",
                Observaciones = resultados,
                FechaRegistro = DateTime.Now
            };

            db.HistorialMedico.Add(historial);
            db.SaveChanges();

            // Auditoría
            AuditoriaHelper.RegistrarAccion("Finalizar Tratamiento", "Veterinario",
                $"Tratamiento ID: {tratamientoId}", usuario.UsuarioId);

            TempData["SuccessMessage"] = "Tratamiento finalizado exitosamente";
            return RedirectToAction("HistorialMedico", new { mascotaId = tratamiento.MascotaId });
        }

        // GET: Veterinario/ListaVacunas
        public ActionResult ListaVacunas()
        {
            var vacunas = db.Vacunas.OrderBy(v => v.NombreVacuna).ToList();
            return View(vacunas);
        }

        // POST: Veterinario/CambiarEstadoMascota
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CambiarEstadoMascota(int mascotaId, string nuevoEstado, string observaciones)
        {
            var mascota = db.Mascotas.Find(mascotaId);

            if (mascota == null)
            {
                return HttpNotFound();
            }

            string estadoAnterior = mascota.Estado;
            mascota.Estado = nuevoEstado;

            var usuario = db.Usuarios.FirstOrDefault(u => u.Email == User.Identity.Name);

            // Registrar en historial
            var historial = new HistorialMedico
            {
                MascotaId = mascotaId,
                VeterinarioId = usuario.UsuarioId,
                TipoRegistro = "Cambio Estado",
                Diagnostico = $"Estado cambiado de '{estadoAnterior}' a '{nuevoEstado}'",
                Observaciones = observaciones,
                FechaRegistro = DateTime.Now
            };

            db.HistorialMedico.Add(historial);
            db.SaveChanges();

            // Auditoría
            AuditoriaHelper.RegistrarAccion("Cambiar Estado Mascota", "Veterinario",
                $"Mascota ID: {mascotaId}, Nuevo estado: {nuevoEstado}", usuario.UsuarioId);

            TempData["SuccessMessage"] = "Estado actualizado exitosamente";
            return RedirectToAction("HistorialMedico", new { mascotaId });
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
