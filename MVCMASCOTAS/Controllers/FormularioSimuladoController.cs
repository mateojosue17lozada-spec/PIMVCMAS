using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using MVCMASCOTAS.Models.ViewModels; // Asegúrate de que este using apunte al lugar donde están definidos tus enums

namespace MVCMASCOTAS.Controllers
{
    public class FormularioSimuladoController : Controller
    {
        // Clase auxiliar para la simulación
        public class FormularioSimuladoItem
        {
            public int Id { get; set; }
            public string Solicitante { get; set; }
            public string Mascota { get; set; }
            public DateTime FechaSolicitud { get; set; }
            public string Estado { get; set; }
            public FormularioAdopcionViewModel Formulario { get; set; }
        }

        // Lista estática en memoria (simula la base de datos)
        private static List<FormularioSimuladoItem> _formularios = new List<FormularioSimuladoItem>();

        // Constructor estático para generar datos de ejemplo
        static FormularioSimuladoController()
        {
            GenerarDatosEjemplo();
        }

        private static void GenerarDatosEjemplo()
        {
            var rnd = new Random();
            string[] nombres = { "Juan Pérez", "María Gómez", "Carlos López", "Ana Martínez", "Luis Rodríguez" };
            string[] mascotas = { "Max (Perro)", "Luna (Gato)", "Rocky (Perro)", "Mimi (Gato)", "Piolín (Ave)" };
            string[] estados = { "Pendiente", "Aprobada", "Rechazada" };

            // Obtener todos los valores posibles de los enums
            var tipoViviendaValues = new[] { "Casa", "Departamento", "Finca/Quinta", "Otro" };
            var tamanioJardinValues = new[] { "Pequeño (< 50m²)", "Mediano (50-100m²)", "Grande (> 100m²)" };
            var tiempoDisponibleValues = new[] { "Menos de 1 hora", "1-2 horas", "2-4 horas", "4+ horas", "Todo el día" };

            var quienCuidaraValues = Enum.GetValues(typeof(QuienCuidaraEnum)).Cast<QuienCuidaraEnum>().ToList();
            var motivoAdopcionValues = Enum.GetValues(typeof(MotivoAdopcionEnum)).Cast<MotivoAdopcionEnum>().ToList();
            var planCambioResidenciaValues = Enum.GetValues(typeof(PlanCambioResidenciaEnum)).Cast<PlanCambioResidenciaEnum>().ToList();
            var planProblemasComportamientoValues = Enum.GetValues(typeof(PlanProblemasComportamientoEnum)).Cast<PlanProblemasComportamientoEnum>().ToList();
            var veterinarioReferenciaValues = Enum.GetValues(typeof(VeterinarioReferenciaEnum)).Cast<VeterinarioReferenciaEnum>().ToList();

            for (int i = 1; i <= 8; i++)
            {
                // Seleccionar valores aleatorios para los enums
                QuienCuidaraEnum quienCuidara = quienCuidaraValues[rnd.Next(quienCuidaraValues.Count)];
                MotivoAdopcionEnum motivo = motivoAdopcionValues[rnd.Next(motivoAdopcionValues.Count)];
                PlanCambioResidenciaEnum cambioResidencia = planCambioResidenciaValues[rnd.Next(planCambioResidenciaValues.Count)];
                PlanProblemasComportamientoEnum problemas = planProblemasComportamientoValues[rnd.Next(planProblemasComportamientoValues.Count)];
                VeterinarioReferenciaEnum veterinario = veterinarioReferenciaValues[rnd.Next(veterinarioReferenciaValues.Count)];

                // Crear el ViewModel con valores por defecto y algunos aleatorios
                var f = new FormularioAdopcionViewModel
                {
                    MascotaId = i,
                    TipoVivienda = tipoViviendaValues[rnd.Next(tipoViviendaValues.Length)],
                    ViviendaPropia = rnd.Next(0, 2) == 0,
                    TieneJardin = rnd.Next(0, 2) == 0,
                    TamanioJardin = rnd.Next(0, 2) == 0 ? tamanioJardinValues[rnd.Next(tamanioJardinValues.Length)] : null,
                    PermisoMascotas = true,
                    PersonasEnCasa = rnd.Next(1, 5),
                    HayNinios = rnd.Next(0, 2) == 0,
                    EdadesNinios = rnd.Next(0, 2) == 0 ? "5 y 8 años" : null,
                    ExperienciaPreviaConMascotas = rnd.Next(0, 2) == 0,
                    DetalleExperiencia = rnd.Next(0, 2) == 0 ? "He tenido perros toda mi vida" : null,
                    TieneMascotasActualmente = rnd.Next(0, 2) == 0,
                    CantidadPerros = rnd.Next(0, 3),
                    CantidadGatos = rnd.Next(0, 3),
                    OtrasMascotas = rnd.Next(0, 2) == 0 ? "Conejos" : "",
                    MascotasEsterilizadas = rnd.Next(0, 2) == 0,
                    TiempoDisponibleDiario = tiempoDisponibleValues[rnd.Next(tiempoDisponibleValues.Length)],
                    QuienCuidaraMascota = quienCuidara,          // ← Asignación directa del enum
                    OtroQuienCuidaraMascota = quienCuidara == QuienCuidaraEnum.Otro ? "Un vecino" : null,
                    MotivoAdopcion = motivo,                     // ← enum
                    OtroMotivoAdopcion = motivo == MotivoAdopcionEnum.Otro ? "Por cariño a los animales" : null,
                    QuePasaSiCambiaResidencia = cambioResidencia, // ← enum
                    QuePasaSiProblemasComportamiento = problemas, // ← enum
                    VeterinarioReferencia = veterinario,         // ← enum
                    ReferenciaPersonal1 = "Carlos Pérez",
                    TelefonoReferencia1 = "0991234567",
                    ReferenciaPersonal2 = rnd.Next(0, 2) == 0 ? "Ana López" : null,
                    TelefonoReferencia2 = rnd.Next(0, 2) == 0 ? "0997654321" : null,
                    AceptaEsterilizacion = true,
                    AceptaVisitasSeguimiento = true,
                    AceptaCondicionesLOBA = true,
                    AceptaDevolucionSiNoPuedeAtender = true
                };

                _formularios.Add(new FormularioSimuladoItem
                {
                    Id = i,
                    Solicitante = nombres[rnd.Next(nombres.Length)],
                    Mascota = mascotas[rnd.Next(mascotas.Length)],
                    FechaSolicitud = DateTime.Now.AddDays(-rnd.Next(1, 30)),
                    Estado = estados[rnd.Next(estados.Length)],
                    Formulario = f
                });
            }
        }

        // GET: FormularioSimulado
        public ActionResult Index()
        {
            return View(_formularios);
        }

        // GET: FormularioSimulado/Details/5
        public ActionResult Details(int id)
        {
            var item = _formularios.FirstOrDefault(f => f.Id == id);
            if (item == null)
                return HttpNotFound();
            return View(item);
        }

        // GET: FormularioSimulado/Delete/5
        public ActionResult Delete(int id)
        {
            var item = _formularios.FirstOrDefault(f => f.Id == id);
            if (item == null)
                return HttpNotFound();
            return View(item);
        }

        // POST: FormularioSimulado/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            var item = _formularios.FirstOrDefault(f => f.Id == id);
            if (item != null)
                _formularios.Remove(item);
            return RedirectToAction("Index");
        }
    }
}