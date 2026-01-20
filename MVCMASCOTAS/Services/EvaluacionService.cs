using System;
using System.Collections.Generic;
using System.Linq;
using MVCMASCOTAS.Models;
using MVCMASCOTAS.Models.CustomModels;

namespace MVCMASCOTAS.Services
{
    /// <summary>
    /// Servicio para evaluación de solicitudes de adopción
    /// </summary>
    public class EvaluacionService
    {
        private RefugioMascotasEntities db;

        public EvaluacionService()
        {
            db = new RefugioMascotasEntities();
        }

        public EvaluacionService(RefugioMascotasEntities context)
        {
            db = context;
        }

        /// <summary>
        /// Evalúa una solicitud de adopción completa
        /// </summary>
        public ResultadoEvaluacionModel EvaluarSolicitud(int solicitudId)
        {
            var solicitud = db.SolicitudAdopcion.Find(solicitudId);

            if (solicitud == null)
            {
                return null;
            }

            var formulario = db.FormularioAdopcionDetalle
                .FirstOrDefault(f => f.SolicitudId == solicitudId);

            if (formulario == null)
            {
                return null;
            }

            return EvaluarFormulario(formulario);
        }

        /// <summary>
        /// Evalúa el formulario de adopción
        /// </summary>
        public ResultadoEvaluacionModel EvaluarFormulario(FormularioAdopcionDetalle formulario)
        {
            int puntaje = 0;
            var detalles = new List<string>();

            // 1. EVALUACIÓN DE VIVIENDA (20 puntos)
            int puntajeVivienda = EvaluarVivienda(formulario, detalles);
            puntaje += puntajeVivienda;

            // 2. EVALUACIÓN DE EXPERIENCIA (20 puntos)
            int puntajeExperiencia = EvaluarExperiencia(formulario, detalles);
            puntaje += puntajeExperiencia;

            // 3. EVALUACIÓN DE DISPONIBILIDAD (20 puntos)
            int puntajeDisponibilidad = EvaluarDisponibilidad(formulario, detalles);
            puntaje += puntajeDisponibilidad;

            // 4. EVALUACIÓN DE COMPROMISOS LEGALES (20 puntos)
            int puntajeCompromisos = EvaluarCompromisos(formulario, detalles);
            puntaje += puntajeCompromisos;

            // 5. EVALUACIÓN DE COMPROMISO Y MOTIVACIÓN (20 puntos)
            int puntajeMotivacion = EvaluarMotivacion(formulario, detalles);
            puntaje += puntajeMotivacion;

            // Determinar resultado
            string resultado = DeterminarResultado(puntaje);
            string recomendacion = GenerarRecomendacion(puntaje, formulario, detalles);

            return new ResultadoEvaluacionModel
            {
                Puntaje = puntaje,
                Resultado = resultado,
                Recomendacion = recomendacion,
                DetallesEvaluacion = detalles
            };
        }

        /// <summary>
        /// Evalúa las condiciones de vivienda
        /// </summary>
        private int EvaluarVivienda(FormularioAdopcionDetalle formulario, List<string> detalles)
        {
            int puntaje = 0;

            if (formulario.ViviendaPropia == true)
            {
                puntaje += 10;
                detalles.Add("✓ Vivienda propia (+10 pts)");
            }
            else
            {
                detalles.Add("○ Vivienda alquilada (0 pts)");
            }

            if (formulario.TieneJardin == true)
            {
                puntaje += 5;
                detalles.Add("✓ Tiene jardín (+5 pts)");
            }

            if (formulario.PermisoMascotas == true)
            {
                puntaje += 5;
                detalles.Add("✓ Permiso para mascotas (+5 pts)");
            }
            else
            {
                detalles.Add("✗ No tiene permiso para mascotas (0 pts) - CRÍTICO");
            }

            return puntaje;
        }

        /// <summary>
        /// Evalúa la experiencia con mascotas
        /// </summary>
        private int EvaluarExperiencia(FormularioAdopcionDetalle formulario, List<string> detalles)
        {
            int puntaje = 0;

            if (formulario.ExperienciaPreviaConMascotas == true)
            {
                puntaje += 15;
                detalles.Add("✓ Experiencia previa con mascotas (+15 pts)");
            }
            else
            {
                detalles.Add("○ Sin experiencia previa (0 pts)");
            }

            if (formulario.TieneMascotasActualmente == true)
            {
                if (formulario.MascotasEsterilizadas == true)
                {
                    puntaje += 5;
                    detalles.Add("✓ Mascotas actuales esterilizadas (+5 pts)");
                }
                else
                {
                    detalles.Add("○ Mascotas no esterilizadas (0 pts)");
                }
            }

            return puntaje;
        }

        /// <summary>
        /// Evalúa la disponibilidad de tiempo
        /// </summary>
        private int EvaluarDisponibilidad(FormularioAdopcionDetalle formulario, List<string> detalles)
        {
            int puntaje = 0;

            switch (formulario.TiempoDisponibleDiario)
            {
                case "4+ horas":
                    puntaje = 20;
                    detalles.Add("✓ Disponibilidad: 4+ horas diarias (+20 pts)");
                    break;
                case "2-4 horas":
                    puntaje = 15;
                    detalles.Add("✓ Disponibilidad: 2-4 horas diarias (+15 pts)");
                    break;
                case "1-2 horas":
                    puntaje = 10;
                    detalles.Add("○ Disponibilidad: 1-2 horas diarias (+10 pts)");
                    break;
                case "Menos de 1 hora":
                    puntaje = 5;
                    detalles.Add("✗ Disponibilidad: Menos de 1 hora (+5 pts) - INSUFICIENTE");
                    break;
            }

            return puntaje;
        }

        /// <summary>
        /// Evalúa los compromisos legales
        /// </summary>
        private int EvaluarCompromisos(FormularioAdopcionDetalle formulario, List<string> detalles)
        {
            int puntaje = 0;

            if (formulario.AceptaEsterilizacion == true)
            {
                puntaje += 5;
                detalles.Add("✓ Acepta esterilización (+5 pts)");
            }
            else
            {
                detalles.Add("✗ No acepta esterilización (0 pts) - CRÍTICO");
            }

            if (formulario.AceptaVisitasSeguimiento == true)
            {
                puntaje += 5;
                detalles.Add("✓ Acepta visitas de seguimiento (+5 pts)");
            }
            else
            {
                detalles.Add("✗ No acepta visitas de seguimiento (0 pts)");
            }

            if (formulario.AceptaCondicionesLOBA == true)
            {
                puntaje += 5;
                detalles.Add("✓ Acepta condiciones LOBA (+5 pts)");
            }
            else
            {
                detalles.Add("✗ No acepta condiciones LOBA (0 pts) - CRÍTICO");
            }

            if (formulario.AceptaDevolucionSiNoPuedeAtender == true)
            {
                puntaje += 5;
                detalles.Add("✓ Acepta devolución si no puede atender (+5 pts)");
            }
            else
            {
                detalles.Add("✗ No acepta devolución (0 pts)");
            }

            return puntaje;
        }

        /// <summary>
        /// Evalúa la motivación y compromiso
        /// </summary>
        private int EvaluarMotivacion(FormularioAdopcionDetalle formulario, List<string> detalles)
        {
            int puntaje = 0;

            if (!string.IsNullOrEmpty(formulario.MotivoAdopcion))
            {
                if (formulario.MotivoAdopcion.Length > 100)
                {
                    puntaje += 10;
                    detalles.Add("✓ Motivación bien explicada (+10 pts)");
                }
                else if (formulario.MotivoAdopcion.Length > 50)
                {
                    puntaje += 7;
                    detalles.Add("✓ Motivación explicada (+7 pts)");
                }
                else
                {
                    puntaje += 3;
                    detalles.Add("○ Motivación breve (+3 pts)");
                }
            }

            if (!string.IsNullOrEmpty(formulario.QuePasaSiCambiaResidencia))
            {
                puntaje += 5;
                detalles.Add("✓ Plan ante cambio de residencia (+5 pts)");
            }

            if (!string.IsNullOrEmpty(formulario.QuePasaSiProblemasComportamiento))
            {
                puntaje += 5;
                detalles.Add("✓ Plan ante problemas de comportamiento (+5 pts)");
            }

            return puntaje;
        }

        /// <summary>
        /// Determina el resultado según el puntaje
        /// </summary>
        private string DeterminarResultado(int puntaje)
        {
            if (puntaje >= 80)
                return "Apto";
            else if (puntaje >= 60)
                return "Revisión Manual";
            else if (puntaje >= 40)
                return "No Apto - Requiere Mejoras";
            else
                return "No Apto";
        }

        /// <summary>
        /// Genera recomendaciones basadas en la evaluación
        /// </summary>
        private string GenerarRecomendacion(int puntaje, FormularioAdopcionDetalle formulario,
            List<string> detalles)
        {
            if (puntaje >= 80)
            {
                return "APROBADO: El solicitante cumple con todos los requisitos necesarios para adoptar. " +
                       "Se recomienda proceder con la adopción.";
            }
            else if (puntaje >= 60)
            {
                var problemas = new List<string>();

                if (formulario.ViviendaPropia != true)
                    problemas.Add("verificar estabilidad de vivienda");

                if (formulario.ExperienciaPreviaConMascotas != true)
                    problemas.Add("proporcionar orientación sobre cuidado de mascotas");

                if (formulario.TiempoDisponibleDiario == "1-2 horas" ||
                    formulario.TiempoDisponibleDiario == "Menos de 1 hora")
                    problemas.Add("evaluar disponibilidad de tiempo");

                return $"REVISIÓN MANUAL REQUERIDA: El solicitante tiene potencial pero requiere evaluación " +
                       $"adicional. Recomendaciones: {string.Join(", ", problemas)}.";
            }
            else
            {
                return "NO APTO: El solicitante no cumple con los requisitos mínimos en este momento. " +
                       "Se recomienda rechazar la solicitud o solicitar que mejore las condiciones antes de reevaluar.";
            }
        }

        /// <summary>
        /// Registra la evaluación en la base de datos
        /// </summary>
        public EvaluacionAdopcion RegistrarEvaluacion(int solicitudId, int evaluadorId,
            ResultadoEvaluacionModel resultado)
        {
            var evaluacion = new EvaluacionAdopcion
            {
                SolicitudId = solicitudId,
                EvaluadorId = evaluadorId,
                FechaEvaluacion = DateTime.Now,
                PuntajeTotal = resultado.Puntaje,
                Resultado = resultado.Resultado,
                Observaciones = resultado.Recomendacion
            };

            db.EvaluacionAdopcion.Add(evaluacion);

            // Actualizar solicitud
            var solicitud = db.SolicitudAdopcion.Find(solicitudId);
            if (solicitud != null)
            {
                solicitud.PuntajeEvaluacion = resultado.Puntaje;
                solicitud.ResultadoEvaluacion = resultado.Resultado;
                solicitud.FechaEvaluacion = DateTime.Now;

                if (resultado.Resultado == "Apto")
                {
                    solicitud.Estado = "Aprobada";
                    solicitud.FechaAprobacion = DateTime.Now;
                }
                else if (resultado.Resultado == "Revisión Manual")
                {
                    solicitud.Estado = "En evaluación";
                }
                else
                {
                    solicitud.Estado = "Rechazada";
                    solicitud.FechaRespuesta = DateTime.Now;
                }
            }

            db.SaveChanges();

            return evaluacion;
        }

        /// <summary>
        /// Obtiene todas las evaluaciones de un solicitante
        /// </summary>
        public List<EvaluacionAdopcion> ObtenerEvaluacionesDeUsuario(int usuarioId)
        {
            return db.EvaluacionAdopcion
                .Where(e => e.SolicitudAdopcion.UsuarioId == usuarioId)
                .OrderByDescending(e => e.FechaEvaluacion)
                .ToList();
        }

        /// <summary>
        /// Calcula el promedio de puntajes de evaluación
        /// </summary>
        public decimal ObtenerPromedioPuntajes()
        {
            var evaluaciones = db.SolicitudAdopcion
                .Where(s => s.PuntajeEvaluacion.HasValue)
                .Select(s => s.PuntajeEvaluacion.Value)
                .ToList();

            if (!evaluaciones.Any())
                return 0;

            return (decimal)evaluaciones.Average();
        }

        public void Dispose()
        {
            db?.Dispose();
        }
    }
}
