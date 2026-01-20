using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

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