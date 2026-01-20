using System.Collections.Generic;

namespace MVCMASCOTAS.Models.CustomModels
{
    /// <summary>
    /// Modelo para resultados de evaluación de adopción
    /// </summary>
    public class ResultadoEvaluacionModel
    {
        public int Puntaje { get; set; }
        public string Resultado { get; set; }
        public string Recomendacion { get; set; }
        public List<string> DetallesEvaluacion { get; set; }

        public ResultadoEvaluacionModel()
        {
            DetallesEvaluacion = new List<string>();
        }
    }
}
