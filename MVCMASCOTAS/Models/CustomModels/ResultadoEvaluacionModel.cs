using System;
using System.Collections.Generic;

namespace MVCMASCOTAS.Models.CustomModels
{
    public class ResultadoEvaluacionModel
    {
        public int PuntajeTotal { get; set; }
        public string Resultado { get; set; }
        public string Recomendacion { get; set; }
        public List<CriterioEvaluacion> Criterios { get; set; }
        public bool Aprobado => PuntajeTotal >= 70;
        public string NivelAdopcion { get; set; }

        // ✅ AÑADE ESTAS PROPIEDADES FALTANTES:
        public int PuntajeVivienda { get; set; }
        public int PuntajeExperiencia { get; set; }
        public int PuntajeDisponibilidad { get; set; }
        public int PuntajeReferencias { get; set; }
        public int PuntajeCompromiso { get; set; }

        public ResultadoEvaluacionModel()
        {
            Criterios = new List<CriterioEvaluacion>();
        }
    }

    public class CriterioEvaluacion
    {
        public string Nombre { get; set; }
        public int Puntaje { get; set; }
        public int PuntajeMaximo { get; set; }
        public string Descripcion { get; set; }
        public string Categoria { get; set; }
    }
}