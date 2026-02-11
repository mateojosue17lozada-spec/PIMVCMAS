using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MVCMASCOTAS.Models.ViewModels
{
    /// <summary>
    /// Resultado de validación de imágenes subidas por usuarios
    /// </summary>
    public class ResultadoValidacionImagen
    {
        public bool EsValida { get; set; }
        public string MensajeError { get; set; }
    }
}