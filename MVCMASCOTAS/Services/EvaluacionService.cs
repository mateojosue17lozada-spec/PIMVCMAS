using System;
using MVCMASCOTAS.Models;
using MVCMASCOTAS.Models.CustomModels;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MVCMASCOTAS.Services
{
    public class EvaluacionService
    {
        private readonly RefugioMascotasDBEntities db;

        public EvaluacionService()
        {
            db = new RefugioMascotasDBEntities();
        }

        // Evaluar solicitud de adopción (sistema de 100 puntos)
        public ResultadoEvaluacionModel EvaluarSolicitudAdopcion(FormularioAdopcionDetalle formulario)
        {
            var resultado = new ResultadoEvaluacionModel();

            // 1. Evaluación de Vivienda (25 puntos)
            resultado.Criterios.Add(EvaluarVivienda(formulario));

            // 2. Evaluación de Experiencia (20 puntos)
            resultado.Criterios.Add(EvaluarExperiencia(formulario));

            // 3. Evaluación de Disponibilidad (20 puntos)
            resultado.Criterios.Add(EvaluarDisponibilidad(formulario));

            // 4. Evaluación de Compromisos Legales (25 puntos)
            resultado.Criterios.Add(EvaluarCompromisosLegales(formulario));

            // 5. Evaluación de Motivación (10 puntos)
            resultado.Criterios.Add(EvaluarMotivacion(formulario));

            // Calcular puntaje total
            resultado.PuntajeTotal = resultado.Criterios.Sum(c => c.Puntaje);

            // Determinar resultado
            if (resultado.PuntajeTotal >= 85)
            {
                resultado.Resultado = "Excelente";
                resultado.Recomendacion = "Candidato ideal para adopción. Cumple con todos los requisitos.";
                resultado.NivelAdopcion = "Alto";
            }
            else if (resultado.PuntajeTotal >= 70)
            {
                resultado.Resultado = "Bueno";
                resultado.Recomendacion = "Candidato apto para adopción. Cumple con la mayoría de requisitos.";
                resultado.NivelAdopcion = "Medio";
            }
            else if (resultado.PuntajeTotal >= 60)
            {
                resultado.Resultado = "Regular";
                resultado.Recomendacion = "Requiere seguimiento adicional. Considerar entrevista personal.";
                resultado.NivelAdopcion = "Bajo";
            }
            else
            {
                resultado.Resultado = "Insuficiente";
                resultado.Recomendacion = "No cumple con los requisitos mínimos de adopción.";
                resultado.NivelAdopcion = "No Apto";
            }

            return resultado;
        }

        private CriterioEvaluacion EvaluarVivienda(FormularioAdopcionDetalle formulario)
        {
            int puntaje = 0;

            // Tipo de vivienda (5 puntos)
            if (formulario.TipoVivienda == "Casa")
                puntaje += 5;
            else if (formulario.TipoVivienda == "Departamento")
                puntaje += 3;
            else
                puntaje += 2;

            // Vivienda propia (5 puntos)
            if (formulario.ViviendaPropia == true)
                puntaje += 5;
            else if (formulario.PermisoMascotas == true)
                puntaje += 3;

            // Jardín (5 puntos)
            if (formulario.TieneJardin == true)
                puntaje += 5;
            else
                puntaje += 2;

            // Permiso para mascotas (10 puntos)
            if (formulario.PermisoMascotas == true)
                puntaje += 10;

            return new CriterioEvaluacion
            {
                Nombre = "Condiciones de Vivienda",
                Puntaje = Math.Min(puntaje, 25),
                PuntajeMaximo = 25,
                Categoria = "Vivienda",
                Descripcion = "Evalúa si la vivienda es adecuada para la mascota"
            };
        }

        private CriterioEvaluacion EvaluarExperiencia(FormularioAdopcionDetalle formulario)
        {
            int puntaje = 0;

            // Experiencia previa (10 puntos)
            if (formulario.ExperienciaPreviaConMascotas == true)
                puntaje += 10;
            else
                puntaje += 3;

            // Mascotas actuales (5 puntos)
            if (formulario.TieneMascotasActualmente == true)
            {
                puntaje += 3;
                if (formulario.MascotasEsterilizadas == true)
                    puntaje += 2;
            }

            // Detalle de experiencia (5 puntos)
            if (!string.IsNullOrEmpty(formulario.DetalleExperiencia) && formulario.DetalleExperiencia.Length > 50)
                puntaje += 5;
            else if (!string.IsNullOrEmpty(formulario.DetalleExperiencia))
                puntaje += 2;

            return new CriterioEvaluacion
            {
                Nombre = "Experiencia con Mascotas",
                Puntaje = Math.Min(puntaje, 20),
                PuntajeMaximo = 20,
                Categoria = "Experiencia",
                Descripcion = "Evalúa la experiencia previa con mascotas"
            };
        }

        private CriterioEvaluacion EvaluarDisponibilidad(FormularioAdopcionDetalle formulario)
        {
            int puntaje = 0;

            // Tiempo disponible (10 puntos)
            if (formulario.TiempoDisponibleDiario == "Más de 4 horas")
                puntaje += 10;
            else if (formulario.TiempoDisponibleDiario == "2-4 horas")
                puntaje += 7;
            else if (formulario.TiempoDisponibleDiario == "1-2 horas")
                puntaje += 4;
            else
                puntaje += 2;

            // Personas en casa (5 puntos)
            if (formulario.PersonasEnCasa >= 2)
                puntaje += 5;
            else if (formulario.PersonasEnCasa == 1)
                puntaje += 3;

            // Niños en casa (5 puntos)
            if (formulario.HayNinios == false)
                puntaje += 5;
            else if (!string.IsNullOrEmpty(formulario.EdadesNinios))
                puntaje += 3;

            return new CriterioEvaluacion
            {
                Nombre = "Disponibilidad y Tiempo",
                Puntaje = Math.Min(puntaje, 20),
                PuntajeMaximo = 20,
                Categoria = "Disponibilidad",
                Descripcion = "Evalúa el tiempo disponible para cuidar la mascota"
            };
        }

        private CriterioEvaluacion EvaluarCompromisosLegales(FormularioAdopcionDetalle formulario)
        {
            int puntaje = 0;

            // Todos los compromisos son obligatorios
            if (formulario.AceptaEsterilizacion == true)
                puntaje += 7;

            if (formulario.AceptaVisitasSeguimiento == true)
                puntaje += 6;

            if (formulario.AceptaCondicionesLOBA == true)
                puntaje += 6;

            if (formulario.AceptaDevolucionSiNoPuedeAtender == true)
                puntaje += 6;

            return new CriterioEvaluacion
            {
                Nombre = "Compromisos Legales",
                Puntaje = puntaje,
                PuntajeMaximo = 25,
                Categoria = "Legal",
                Descripcion = "Evalúa la aceptación de compromisos legales y éticos"
            };
        }

        private CriterioEvaluacion EvaluarMotivacion(FormularioAdopcionDetalle formulario)
        {
            int puntaje = 0;

            // Motivo de adopción (5 puntos)
            if (!string.IsNullOrEmpty(formulario.MotivoAdopcion))
            {
                if (formulario.MotivoAdopcion.Length > 100)
                    puntaje += 5;
                else if (formulario.MotivoAdopcion.Length > 50)
                    puntaje += 3;
                else
                    puntaje += 1;
            }

            // Planes futuros (3 puntos)
            if (!string.IsNullOrEmpty(formulario.QuePasaSiCambiaResidencia))
                puntaje += 3;

            // Manejo de problemas (2 puntos)
            if (!string.IsNullOrEmpty(formulario.QuePasaSiProblemasComportamiento))
                puntaje += 2;

            return new CriterioEvaluacion
            {
                Nombre = "Motivación y Compromiso",
                Puntaje = Math.Min(puntaje, 10),
                PuntajeMaximo = 10,
                Categoria = "Motivación",
                Descripcion = "Evalúa la motivación y compromiso a largo plazo"
            };
        }

        public void Dispose()
        {
            db?.Dispose();
        }
    }
}