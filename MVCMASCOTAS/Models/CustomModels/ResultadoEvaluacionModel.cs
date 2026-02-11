using MVCMASCOTAS.Controllers;
using MVCMASCOTAS.Models;
using MVCMASCOTAS.Models.CustomModels;
using MVCMASCOTAS.Services;
using System;
using System.Collections.Generic;

namespace MVCMASCOTAS.Models.CustomModels
{
    public class ResultadoEvaluacionModel
    {
        // Puntajes por categoría
        public int PuntajeVivienda { get; set; }
        public int PuntajeExperiencia { get; set; }
        public int PuntajeDisponibilidad { get; set; }
        public int PuntajeReferencias { get; set; }
        public int PuntajeCompromiso { get; set; }

        // Puntaje total
        public int PuntajeTotal { get; set; }

        // Resultado y recomendación
        public string Resultado { get; set; }
        public string Recomendacion { get; set; }
        public string NivelAdopcion { get; set; }

        // ⚡ NUEVAS PROPIEDADES REQUERIDAS
        public string Observaciones { get; set; }
        public DateTime FechaEvaluacion { get; set; }

        // Lista de criterios detallados
        public List<CriterioEvaluacion> Criterios { get; set; }

        // Propiedades calculadas
        public bool Aprobado => PuntajeTotal >= 70;

        public string ResultadoColorClass
        {
            get
            {
                switch (Resultado)
                {
                    case "Apto":
                    case "Excelente":
                    case "Bueno":
                        return "success";
                    case "Revisión manual":
                    case "Regular":
                        return "warning";
                    case "No apto":
                    case "Insuficiente":
                        return "danger";
                    default:
                        return "secondary";
                }
            }
        }

        public string ResultadoIcono
        {
            get
            {
                switch (Resultado)
                {
                    case "Apto":
                    case "Excelente":
                        return "fa-check-circle";
                    case "Bueno":
                        return "fa-thumbs-up";
                    case "Revisión manual":
                    case "Regular":
                        return "fa-exclamation-triangle";
                    case "No apto":
                    case "Insuficiente":
                        return "fa-times-circle";
                    default:
                        return "fa-question-circle";
                }
            }
        }

        // Constructor
        public ResultadoEvaluacionModel()
        {
            Criterios = new List<CriterioEvaluacion>();
            FechaEvaluacion = DateTime.Now;
            Observaciones = string.Empty;
            Resultado = "Pendiente";
        }
    }

    public class CriterioEvaluacion
    {
        public string Nombre { get; set; }
        public int Puntaje { get; set; }
        public int PuntajeMaximo { get; set; }
        public string Descripcion { get; set; }
        public string Categoria { get; set; }

        // Propiedad calculada para porcentaje
        public double Porcentaje => PuntajeMaximo > 0 ? (double)Puntaje / PuntajeMaximo * 100 : 0;

        public string PorcentajeFormateado => $"{Porcentaje:F1}%";

        public string ColorClass
        {
            get
            {
                if (Porcentaje >= 80) return "success";
                if (Porcentaje >= 60) return "warning";
                return "danger";
            }
        }
    }
}
