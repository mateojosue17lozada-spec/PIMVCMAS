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
        private RefugioMascotasDBEntities db = new RefugioMascotasDBEntities();

        // GET: Veterinario/Dashboard
        public ActionResult Dashboard()
        {
            var usuario = db.Usuarios.FirstOrDefault(u => u.Email == User.Identity.Name && u.Activo == true);
            if (usuario == null)
            {
                return RedirectToAction("Logout", "Account");
            }

            // Estadísticas
            ViewBag.MascotasEnTratamiento = db.Mascotas.Count(m => m.Estado == "En tratamiento" && m.Activo == true);
            ViewBag.TratamientosActivos = db.Tratamientos.Count(t => t.Estado == "En curso");

            // CORRECTO: HistorialMedico no tiene FechaRegistro, usa FechaConsulta
            ViewBag.CitasHoy = db.HistorialMedico
                .Count(h => h.FechaConsulta.HasValue &&
                           h.FechaConsulta.Value.Year == DateTime.Now.Year &&
                           h.FechaConsulta.Value.Month == DateTime.Now.Month &&
                           h.FechaConsulta.Value.Day == DateTime.Now.Day);

            // Mascotas que requieren atención
            var mascotasAtencion = db.Mascotas
                .Where(m => m.Estado == "En tratamiento" && m.Activo == true)
                .OrderByDescending(m => m.FechaIngreso)
                .Take(10)
                .ToList();

            ViewBag.MascotasAtencion = mascotasAtencion;

            // Tratamientos pendientes
            var tratamientosPendientes = db.Tratamientos
                .Where(t => t.Estado == "En curso" && t.Mascotas.VeterinarioAsignado == usuario.UsuarioId)
                .OrderBy(t => t.FechaInicio)
                .Take(10)
                .ToList();

            ViewBag.TratamientosPendientes = tratamientosPendientes;

            return View();
        }

        // GET: Veterinario/Mascotas
        public ActionResult Mascotas(string estado, int page = 1)
        {
            var usuario = db.Usuarios.FirstOrDefault(u => u.Email == User.Identity.Name && u.Activo == true);
            if (usuario == null)
            {
                return RedirectToAction("Logout", "Account");
            }

            int pageSize = 20;
            var query = db.Mascotas.Where(m => m.Activo == true && m.VeterinarioAsignado == usuario.UsuarioId);

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
            var usuario = db.Usuarios.FirstOrDefault(u => u.Email == User.Identity.Name && u.Activo == true);
            if (usuario == null)
            {
                return RedirectToAction("Logout", "Account");
            }

            var mascota = db.Mascotas.Find(mascotaId);

            if (mascota == null || mascota.VeterinarioAsignado != usuario.UsuarioId)
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
                .OrderByDescending(h => h.FechaConsulta)
                .ToList();

            ViewBag.Historial = historial;

            // Vacunas
            var vacunas = db.MascotaVacunas
                .Where(v => v.MascotaId == mascotaId)
                .Include(v => v.Vacunas)
                .Include(v => v.Usuarios) // Veterinario
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
            decimal? peso, decimal? temperatura, string estadoGeneral, string observaciones)
        {
            var usuario = db.Usuarios.FirstOrDefault(u => u.Email == User.Identity.Name && u.Activo == true);
            if (usuario == null)
            {
                return RedirectToAction("Logout", "Account");
            }

            if (string.IsNullOrEmpty(diagnostico))
            {
                TempData["ErrorMessage"] = "El diagnóstico es requerido";
                return RedirectToAction("HistorialMedico", new { mascotaId });
            }

            var historial = new HistorialMedico
            {
                MascotaId = mascotaId,
                VeterinarioId = usuario.UsuarioId,
                TipoConsulta = "Consulta General", // CORRECTO: es TipoConsulta, no TipoRegistro
                Diagnostico = diagnostico,
                Tratamiento = tratamientoRecetado, // CORRECTO: es Tratamiento, no TratamientoRecetado
                Peso = peso,
                Temperatura = temperatura,
                EstadoGeneral = estadoGeneral,
                Observaciones = observaciones,
                FechaConsulta = DateTime.Now, // CORRECTO: es FechaConsulta, no FechaRegistro
                ProximaConsulta = DateTime.Now.AddDays(30)
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
        public ActionResult RegistrarVacuna(int mascotaId, int vacunaId, DateTime? proximaConsulta)
        {
            var usuario = db.Usuarios.FirstOrDefault(u => u.Email == User.Identity.Name && u.Activo == true);
            if (usuario == null)
            {
                return RedirectToAction("Logout", "Account");
            }

            var mascotaVacuna = new MascotaVacunas
            {
                MascotaId = mascotaId,
                VacunaId = vacunaId,
                FechaAplicacion = DateTime.Now,
                VeterinarioId = usuario.UsuarioId,
                ProximaDosis = proximaConsulta, // CORRECTO: es ProximaDosis, no FechaProximaDosis
                Lote = "LOTE-" + DateTime.Now.ToString("yyyyMMdd")
            };

            db.MascotaVacunas.Add(mascotaVacuna);
            db.SaveChanges();

            // Registrar en historial médico
            var vacuna = db.Vacunas.Find(vacunaId);
            var historial = new HistorialMedico
            {
                MascotaId = mascotaId,
                VeterinarioId = usuario.UsuarioId,
                TipoConsulta = "Vacunación", // CORRECTO: es TipoConsulta
                Diagnostico = $"Vacuna aplicada: {vacuna.NombreVacuna}",
                FechaConsulta = DateTime.Now,
                ProximaConsulta = proximaConsulta
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
            var usuario = db.Usuarios.FirstOrDefault(u => u.Email == User.Identity.Name && u.Activo == true);
            if (usuario == null)
            {
                return RedirectToAction("Logout", "Account");
            }

            var mascota = db.Mascotas.Find(mascotaId);

            if (mascota == null || mascota.VeterinarioAsignado != usuario.UsuarioId)
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
        public ActionResult IniciarTratamiento(int mascotaId, string nombreTratamiento, string descripcion,
            string medicamentos, DateTime? fechaFin, decimal? costo)
        {
            var usuario = db.Usuarios.FirstOrDefault(u => u.Email == User.Identity.Name && u.Activo == true);
            if (usuario == null)
            {
                return RedirectToAction("Logout", "Account");
            }

            if (string.IsNullOrEmpty(descripcion))
            {
                TempData["ErrorMessage"] = "La descripción es requerida";
                return RedirectToAction("IniciarTratamiento", new { mascotaId });
            }

            var tratamiento = new Tratamientos
            {
                MascotaId = mascotaId,
                VeterinarioId = usuario.UsuarioId, // CORRECTO: es VeterinarioId, no VeterinarioResponsableId
                NombreTratamiento = nombreTratamiento, // CORRECTO: es NombreTratamiento, no TipoTratamiento
                Descripcion = descripcion,
                Medicamentos = medicamentos,
                FechaInicio = DateTime.Now,
                FechaFin = fechaFin,
                Costo = costo, // CORRECTO: es Costo, no CostoEstimado
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
                TipoConsulta = "Inicio Tratamiento",
                Diagnostico = $"{nombreTratamiento}: {descripcion}",
                Tratamiento = medicamentos,
                FechaConsulta = DateTime.Now
            };

            db.HistorialMedico.Add(historial);
            db.SaveChanges();

            // Auditoría
            AuditoriaHelper.RegistrarAccion("Iniciar Tratamiento", "Veterinario",
                $"Mascota ID: {mascotaId}, Tratamiento: {nombreTratamiento}", usuario.UsuarioId);

            TempData["SuccessMessage"] = "Tratamiento iniciado exitosamente";
            return RedirectToAction("HistorialMedico", new { mascotaId });
        }

        // POST: Veterinario/FinalizarTratamiento
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult FinalizarTratamiento(int tratamientoId, string observaciones)
        {
            var usuario = db.Usuarios.FirstOrDefault(u => u.Email == User.Identity.Name && u.Activo == true);
            if (usuario == null)
            {
                return RedirectToAction("Logout", "Account");
            }

            var tratamiento = db.Tratamientos
                .Include(t => t.Mascotas)
                .FirstOrDefault(t => t.TratamientoId == tratamientoId);

            if (tratamiento == null || tratamiento.Mascotas.VeterinarioAsignado != usuario.UsuarioId)
            {
                return HttpNotFound();
            }

            tratamiento.FechaFin = DateTime.Now;
            tratamiento.Estado = "Completado";
            tratamiento.Observaciones = observaciones; // CORRECTO: es Observaciones, no Resultados

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

            // Registrar en historial
            var historial = new HistorialMedico
            {
                MascotaId = tratamiento.MascotaId,
                VeterinarioId = usuario.UsuarioId,
                TipoConsulta = "Fin Tratamiento",
                Diagnostico = $"Tratamiento completado: {tratamiento.NombreTratamiento}",
                Observaciones = observaciones,
                FechaConsulta = DateTime.Now
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
            var vacunas = db.Vacunas.Where(v => v.Activo == true).OrderBy(v => v.NombreVacuna).ToList();
            return View(vacunas);
        }

        // GET: Veterinario/AgregarVacuna
        public ActionResult AgregarVacuna()
        {
            return View();
        }

        // POST: Veterinario/AgregarVacuna
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AgregarVacuna(string nombreVacuna, string descripcion, string especieAplicable,
            string edadRecomendada, string frecuenciaRefuerzo)
        {
            var usuario = db.Usuarios.FirstOrDefault(u => u.Email == User.Identity.Name && u.Activo == true);
            if (usuario == null)
            {
                return RedirectToAction("Logout", "Account");
            }

            if (string.IsNullOrEmpty(nombreVacuna))
            {
                TempData["ErrorMessage"] = "El nombre de la vacuna es requerido";
                return View();
            }

            var vacuna = new Vacunas
            {
                NombreVacuna = nombreVacuna,
                Descripcion = descripcion,
                EspecieAplicable = especieAplicable,
                EdadRecomendada = edadRecomendada,
                FrecuenciaRefuerzo = frecuenciaRefuerzo,
                Activo = true
            };

            db.Vacunas.Add(vacuna);
            db.SaveChanges();

            // Auditoría
            AuditoriaHelper.RegistrarAccion("Agregar Vacuna", "Veterinario",
                $"Vacuna: {nombreVacuna}", usuario.UsuarioId);

            TempData["SuccessMessage"] = "Vacuna agregada exitosamente";
            return RedirectToAction("ListaVacunas");
        }

        // POST: Veterinario/CambiarEstadoMascota
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CambiarEstadoMascota(int mascotaId, string nuevoEstado, string observaciones)
        {
            var usuario = db.Usuarios.FirstOrDefault(u => u.Email == User.Identity.Name && u.Activo == true);
            if (usuario == null)
            {
                return RedirectToAction("Logout", "Account");
            }

            var mascota = db.Mascotas.Find(mascotaId);

            if (mascota == null || mascota.VeterinarioAsignado != usuario.UsuarioId)
            {
                return HttpNotFound();
            }

            string estadoAnterior = mascota.Estado;
            mascota.Estado = nuevoEstado;

            // Registrar en historial
            var historial = new HistorialMedico
            {
                MascotaId = mascotaId,
                VeterinarioId = usuario.UsuarioId,
                TipoConsulta = "Cambio Estado",
                Diagnostico = $"Estado cambiado de '{estadoAnterior}' a '{nuevoEstado}'",
                Observaciones = observaciones,
                FechaConsulta = DateTime.Now
            };

            db.HistorialMedico.Add(historial);
            db.SaveChanges();

            // Auditoría
            AuditoriaHelper.RegistrarAccion("Cambiar Estado Mascota", "Veterinario",
                $"Mascota ID: {mascotaId}, Nuevo estado: {nuevoEstado}", usuario.UsuarioId);

            TempData["SuccessMessage"] = "Estado actualizado exitosamente";
            return RedirectToAction("HistorialMedico", new { mascotaId });
        }

        // GET: Veterinario/MisMascotasAsignadas
        public ActionResult MisMascotasAsignadas()
        {
            var usuario = db.Usuarios.FirstOrDefault(u => u.Email == User.Identity.Name && u.Activo == true);
            if (usuario == null)
            {
                return RedirectToAction("Logout", "Account");
            }

            var mascotas = db.Mascotas
                .Where(m => m.VeterinarioAsignado == usuario.UsuarioId && m.Activo == true)
                .OrderBy(m => m.Estado)
                .ThenBy(m => m.Nombre)
                .ToList();

            ViewBag.MascotasEnTratamiento = mascotas.Count(m => m.Estado == "En tratamiento");
            ViewBag.MascotasDisponibles = mascotas.Count(m => m.Estado == "Disponible para adopción");
            ViewBag.MascotasRescatadas = mascotas.Count(m => m.Estado == "Rescatada");

            return View(mascotas);
        }

        // GET: Veterinario/ReporteMensual
        public ActionResult ReporteMensual(int? year, int? month)
        {
            var usuario = db.Usuarios.FirstOrDefault(u => u.Email == User.Identity.Name && u.Activo == true);
            if (usuario == null)
            {
                return RedirectToAction("Logout", "Account");
            }

            if (!year.HasValue) year = DateTime.Now.Year;
            if (!month.HasValue) month = DateTime.Now.Month;

            DateTime inicioMes = new DateTime(year.Value, month.Value, 1);
            DateTime finMes = inicioMes.AddMonths(1).AddDays(-1);

            // Consultas del mes
            var consultas = db.HistorialMedico
                .Where(h => h.VeterinarioId == usuario.UsuarioId &&
                           h.FechaConsulta >= inicioMes &&
                           h.FechaConsulta <= finMes)
                .ToList();

            // Tratamientos iniciados
            var tratamientosIniciados = db.Tratamientos
                .Where(t => t.VeterinarioId == usuario.UsuarioId &&
                           t.FechaInicio >= inicioMes &&
                           t.FechaInicio <= finMes)
                .ToList();

            // Vacunas aplicadas
            var vacunasAplicadas = db.MascotaVacunas
                .Where(v => v.VeterinarioId == usuario.UsuarioId &&
                           v.FechaAplicacion >= inicioMes &&
                           v.FechaAplicacion <= finMes)
                .ToList();

            ViewBag.Year = year;
            ViewBag.Month = month;
            ViewBag.MonthName = inicioMes.ToString("MMMM");
            ViewBag.Consultas = consultas;
            ViewBag.TratamientosIniciados = tratamientosIniciados;
            ViewBag.VacunasAplicadas = vacunasAplicadas;
            ViewBag.TotalConsultas = consultas.Count;
            ViewBag.TotalTratamientos = tratamientosIniciados.Count;
            ViewBag.TotalVacunas = vacunasAplicadas.Count;

            return View();
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