using MVCMASCOTAS.Controllers;
using MVCMASCOTAS.Models;
using MVCMASCOTAS.Models.CustomModels;
using System;
using System.Linq;

namespace MVCMASCOTAS.Services
{
    /// <summary>
    /// Servicio de evaluación automática de solicitudes de adopción según LOBA
    /// Sistema de puntuación de 100 puntos dividido en 5 categorías
    /// </summary>
    public class EvaluacionService : IDisposable
    {
        private readonly RefugioMascotasDBEntities db;

        #region CONSTANTES DE EVALUACIÓN

        private const int PUNTAJE_MAX_VIVIENDA = 25;
        private const int PUNTAJE_MAX_EXPERIENCIA = 20;
        private const int PUNTAJE_MAX_COMPROMISO = 25;
        private const int PUNTAJE_MAX_DISPONIBILIDAD = 15;
        private const int PUNTAJE_MAX_REFERENCIAS = 15;

        private const int UMBRAL_APTO = 70;
        private const int UMBRAL_REVISION = 50;
        private const int PUNTAJE_MINIMO_COMPROMISOS = 20;

        #endregion

        #region CONSTRUCTOR

        public EvaluacionService()
        {
            db = new RefugioMascotasDBEntities();
        }

        #endregion

        #region EVALUACIÓN PRINCIPAL

        /// <summary>
        /// Evalúa una solicitud de adopción según criterios LOBA
        /// </summary>
        public ResultadoEvaluacionModel EvaluarSolicitud(FormularioAdopcionDetalle formulario)
        {
            if (formulario == null)
            {
                throw new ArgumentNullException(nameof(formulario), "El formulario no puede ser nulo");
            }

            var resultado = new ResultadoEvaluacionModel
            {
                FechaEvaluacion = DateTime.Now
            };

            try
            {
                resultado.PuntajeVivienda = EvaluarVivienda(formulario);
                resultado.PuntajeExperiencia = EvaluarExperiencia(formulario);
                resultado.PuntajeCompromiso = EvaluarCompromisosLegales(formulario);
                resultado.PuntajeDisponibilidad = EvaluarDisponibilidad(formulario);
                resultado.PuntajeReferencias = EvaluarMotivacionYReferencias(formulario);

                resultado.PuntajeTotal = resultado.PuntajeVivienda +
                                        resultado.PuntajeExperiencia +
                                        resultado.PuntajeCompromiso +
                                        resultado.PuntajeDisponibilidad +
                                        resultado.PuntajeReferencias;

                resultado.Resultado = DeterminarResultado(resultado);
                resultado.Observaciones = GenerarObservaciones(resultado, formulario);

                return resultado;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en EvaluarSolicitud: {ex.Message}");

                return new ResultadoEvaluacionModel
                {
                    Resultado = "Error",
                    PuntajeTotal = 0,
                    Observaciones = $"Error al evaluar solicitud: {ex.Message}",
                    FechaEvaluacion = DateTime.Now
                };
            }
        }

        #endregion

        #region EVALUACIONES POR CATEGORÍA

        private int EvaluarVivienda(FormularioAdopcionDetalle formulario)
        {
            int puntaje = 0;

            // Tipo de vivienda (7 puntos)
            if (formulario.TipoVivienda == "Casa")
                puntaje += 7;
            else if (formulario.TipoVivienda == "Departamento")
                puntaje += 5;
            else if (formulario.TipoVivienda == "Quinta" || formulario.TipoVivienda == "Finca")
                puntaje += 7;
            else
                puntaje += 2;

            // Propiedad de vivienda (6 puntos) - Nullable<bool>
            if (formulario.ViviendaPropia.HasValue && formulario.ViviendaPropia.Value)
            {
                puntaje += 6;
            }
            else if (formulario.PermisoMascotas.HasValue && formulario.PermisoMascotas.Value)
            {
                puntaje += 4;
            }

            // Jardín (6 puntos) - Nullable<bool>
            if (formulario.TieneJardin.HasValue && formulario.TieneJardin.Value)
            {
                puntaje += 4;

                if (formulario.TamanioJardin == "Grande")
                    puntaje += 2;
                else if (formulario.TamanioJardin == "Mediano")
                    puntaje += 1;
            }
            else
            {
                puntaje += 1;
            }

            // Permiso para mascotas (6 puntos) - Nullable<bool>
            if (formulario.PermisoMascotas.HasValue && formulario.PermisoMascotas.Value)
                puntaje += 6;

            return Math.Min(puntaje, PUNTAJE_MAX_VIVIENDA);
        }

        private int EvaluarExperiencia(FormularioAdopcionDetalle formulario)
        {
            int puntaje = 0;

            // Experiencia previa (10 puntos) - Nullable<bool>
            if (formulario.ExperienciaPreviaConMascotas.HasValue && formulario.ExperienciaPreviaConMascotas.Value)
            {
                puntaje += 8;

                if (!string.IsNullOrWhiteSpace(formulario.DetalleExperiencia))
                {
                    if (formulario.DetalleExperiencia.Length > 100)
                        puntaje += 2;
                    else if (formulario.DetalleExperiencia.Length > 50)
                        puntaje += 1;
                }
            }
            else
            {
                puntaje += 2;
            }

            // Mascotas actuales (6 puntos) - Nullable<bool>
            if (formulario.TieneMascotasActualmente.HasValue && formulario.TieneMascotasActualmente.Value)
            {
                puntaje += 3;

                // Nullable<bool>
                if (formulario.MascotasEsterilizadas.HasValue && formulario.MascotasEsterilizadas.Value)
                {
                    puntaje += 3;
                }

                int totalMascotas = (formulario.CantidadPerros ?? 0) + (formulario.CantidadGatos ?? 0);
                if (totalMascotas >= 3)
                {
                    puntaje += 2;
                }
            }
            else
            {
                puntaje += 4;
            }

            return Math.Min(puntaje, PUNTAJE_MAX_EXPERIENCIA);
        }

        private int EvaluarCompromisosLegales(FormularioAdopcionDetalle formulario)
        {
            int puntaje = 0;

            // Los compromisos son bool (no nullable)
            if (formulario.AceptaEsterilizacion)
                puntaje += 7;

            if (formulario.AceptaVisitasSeguimiento)
                puntaje += 6;

            if (formulario.AceptaCondicionesLOBA)
                puntaje += 6;

            if (formulario.AceptaDevolucionSiNoPuedeAtender)
                puntaje += 6;

            return Math.Min(puntaje, PUNTAJE_MAX_COMPROMISO);
        }

        private int EvaluarDisponibilidad(FormularioAdopcionDetalle formulario)
        {
            int puntaje = 0;

            // Tiempo disponible
            if (formulario.TiempoDisponibleDiario == "Más de 4 horas" ||
                formulario.TiempoDisponibleDiario == "4+ horas")
                puntaje += 8;
            else if (formulario.TiempoDisponibleDiario == "2-4 horas")
                puntaje += 6;
            else if (formulario.TiempoDisponibleDiario == "1-2 horas")
                puntaje += 4;
            else if (formulario.TiempoDisponibleDiario == "Menos de 1 hora")
                puntaje += 1;
            else
                puntaje += 3;

            // Personas en casa - Nullable<int>
            int personas = formulario.PersonasEnCasa ?? 1;
            if (personas >= 3)
                puntaje += 4;
            else if (personas == 2)
                puntaje += 3;
            else
                puntaje += 2;

            // Niños en casa - Nullable<bool>
            if (formulario.HayNinios.HasValue)
            {
                if (!formulario.HayNinios.Value)
                {
                    puntaje += 3;
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(formulario.EdadesNinios))
                        puntaje += 2;
                    else
                        puntaje += 1;
                }
            }
            else
            {
                puntaje += 2; // Si no especifica, valor neutral
            }

            return Math.Min(puntaje, PUNTAJE_MAX_DISPONIBILIDAD);
        }

        private int EvaluarMotivacionYReferencias(FormularioAdopcionDetalle formulario)
        {
            int puntaje = 0;

            // Motivo de adopción (6 puntos)
            if (!string.IsNullOrWhiteSpace(formulario.MotivoAdopcion))
            {
                int longitud = formulario.MotivoAdopcion.Length;

                if (longitud > 150)
                    puntaje += 6;
                else if (longitud > 100)
                    puntaje += 5;
                else if (longitud > 50)
                    puntaje += 4;
                else
                    puntaje += 2;

                string motivoLower = formulario.MotivoAdopcion.ToLower();
                var palabrasPositivas = new[] { "amor", "compañía", "familia", "rescate", "ayudar", "cuidar", "responsabilidad" };

                if (palabrasPositivas.Any(p => motivoLower.Contains(p)))
                    puntaje += 1;
            }
            else
            {
                puntaje += 1;
            }

            // Plan si cambia residencia (4 puntos)
            if (!string.IsNullOrWhiteSpace(formulario.QuePasaSiCambiaResidencia))
            {
                if (formulario.QuePasaSiCambiaResidencia.Length > 50)
                    puntaje += 4;
                else if (formulario.QuePasaSiCambiaResidencia.Length > 20)
                    puntaje += 3;
                else
                    puntaje += 2;
            }

            // Plan para problemas de comportamiento (4 puntos)
            if (!string.IsNullOrWhiteSpace(formulario.QuePasaSiProblemasComportamiento))
            {
                if (formulario.QuePasaSiProblemasComportamiento.Length > 50)
                    puntaje += 4;
                else if (formulario.QuePasaSiProblemasComportamiento.Length > 20)
                    puntaje += 3;
                else
                    puntaje += 2;
            }

            // Referencias (1 punto adicional)
            if (!string.IsNullOrWhiteSpace(formulario.ReferenciaPersonal1) ||
                !string.IsNullOrWhiteSpace(formulario.ReferenciaPersonal2))
            {
                puntaje += 1;
            }

            return Math.Min(puntaje, PUNTAJE_MAX_REFERENCIAS);
        }

        #endregion

        #region DETERMINACIÓN DE RESULTADO

        private string DeterminarResultado(ResultadoEvaluacionModel resultado)
        {
            if (resultado.PuntajeCompromiso < PUNTAJE_MINIMO_COMPROMISOS)
            {
                return "No apto";
            }

            if (resultado.PuntajeTotal >= UMBRAL_APTO)
            {
                return "Apto";
            }
            else if (resultado.PuntajeTotal >= UMBRAL_REVISION)
            {
                return "Revisión manual";
            }
            else
            {
                return "No apto";
            }
        }

        private string GenerarObservaciones(ResultadoEvaluacionModel resultado, FormularioAdopcionDetalle formulario)
        {
            var obs = new System.Text.StringBuilder();

            obs.AppendLine($"Puntaje Total: {resultado.PuntajeTotal}/100");
            obs.AppendLine($"Resultado: {resultado.Resultado}");
            obs.AppendLine();

            obs.AppendLine("=== DESGLOSE POR CATEGORÍAS ===");
            obs.AppendLine($"• Vivienda: {resultado.PuntajeVivienda}/{PUNTAJE_MAX_VIVIENDA}");
            obs.AppendLine($"• Experiencia: {resultado.PuntajeExperiencia}/{PUNTAJE_MAX_EXPERIENCIA}");
            obs.AppendLine($"• Compromisos LOBA: {resultado.PuntajeCompromiso}/{PUNTAJE_MAX_COMPROMISO}");
            obs.AppendLine($"• Disponibilidad: {resultado.PuntajeDisponibilidad}/{PUNTAJE_MAX_DISPONIBILIDAD}");
            obs.AppendLine($"• Motivación: {resultado.PuntajeReferencias}/{PUNTAJE_MAX_REFERENCIAS}");
            obs.AppendLine();

            obs.AppendLine("=== ANÁLISIS ===");

            var puntosFuertes = new System.Collections.Generic.List<string>();
            if (resultado.PuntajeVivienda >= 20) puntosFuertes.Add("Excelentes condiciones de vivienda");
            if (resultado.PuntajeExperiencia >= 16) puntosFuertes.Add("Amplia experiencia con mascotas");
            if (resultado.PuntajeCompromiso == PUNTAJE_MAX_COMPROMISO) puntosFuertes.Add("Acepta todos los compromisos LOBA");
            if (resultado.PuntajeDisponibilidad >= 12) puntosFuertes.Add("Buena disponibilidad de tiempo");
            if (resultado.PuntajeReferencias >= 12) puntosFuertes.Add("Motivación sólida y planes claros");

            if (puntosFuertes.Any())
            {
                obs.AppendLine("Puntos Fuertes:");
                foreach (var punto in puntosFuertes)
                {
                    obs.AppendLine($"  ✓ {punto}");
                }
            }

            var areasMejora = new System.Collections.Generic.List<string>();
            if (resultado.PuntajeVivienda < 15) areasMejora.Add("Mejorar condiciones de vivienda");
            if (resultado.PuntajeExperiencia < 12) areasMejora.Add("Adquirir más experiencia con mascotas");
            if (resultado.PuntajeCompromiso < PUNTAJE_MINIMO_COMPROMISOS) areasMejora.Add("CRÍTICO: Debe aceptar compromisos LOBA");
            if (resultado.PuntajeDisponibilidad < 10) areasMejora.Add("Aumentar disponibilidad de tiempo");
            if (resultado.PuntajeReferencias < 10) areasMejora.Add("Fortalecer motivación y planes a largo plazo");

            if (areasMejora.Any())
            {
                obs.AppendLine();
                obs.AppendLine("Áreas de Mejora:");
                foreach (var area in areasMejora)
                {
                    obs.AppendLine($"  ⚠ {area}");
                }
            }

            obs.AppendLine();
            obs.AppendLine("=== RECOMENDACIÓN ===");
            switch (resultado.Resultado)
            {
                case "Apto":
                    obs.AppendLine("✓ Candidato APTO para adopción.");
                    obs.AppendLine("  Cumple con todos los requisitos LOBA.");
                    break;

                case "Revisión manual":
                    obs.AppendLine("⚠ Requiere REVISIÓN MANUAL.");
                    obs.AppendLine("  Cumple parcialmente con los requisitos.");
                    break;

                case "No apto":
                    obs.AppendLine("✗ NO APTO para adopción.");
                    obs.AppendLine("  No cumple con los requisitos mínimos LOBA.");
                    break;
            }

            return obs.ToString();
        }

        #endregion

        #region DISPOSE

        public void Dispose()
        {
            db?.Dispose();
        }

        #endregion
    }
}
