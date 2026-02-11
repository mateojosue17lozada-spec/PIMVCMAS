using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MVCMASCOTAS.Models.ViewModels
{
    public class DetalleMascotaPerdidaViewModel
    {
        public int MascotaPerdidaId { get; set; }

        // ======================
        // DATOS DE LA MASCOTA
        // ======================
        [Display(Name = "Nombre")]
        public string NombreMascota { get; set; }

        [Display(Name = "Especie")]
        public string Especie { get; set; }

        [Display(Name = "Raza")]
        public string Raza { get; set; }

        [Display(Name = "Sexo")]
        public string Sexo { get; set; }

        [Display(Name = "Edad")]
        public string Edad { get; set; }

        [Display(Name = "Color")]
        public string Color { get; set; }

        [Display(Name = "Características Distintivas")]
        public string CaracteristicasDistintivas { get; set; }

        // ======================
        // DATOS DE LA PÉRDIDA
        // ======================
        [Display(Name = "Fecha de Pérdida")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}")]
        public DateTime FechaPerdida { get; set; }

        [Display(Name = "Ubicación")]
        public string UbicacionPerdida { get; set; }

        [Display(Name = "Coordenadas GPS")]
        public string CoordenadasGPS { get; set; }

        [Display(Name = "Observaciones")]
        public string Observaciones { get; set; }

        // ======================
        // DATOS DEL PROPIETARIO
        // ======================
        public int? UsuarioPropietarioId { get; set; }

        [Display(Name = "Dueño")]
        public string PropietarioNombre { get; set; }

        [Display(Name = "Teléfono")]
        public string ContactoTelefono { get; set; }

        [Display(Name = "Email")]
        public string ContactoEmail { get; set; }

        [Display(Name = "Reportado por")]
        public string ContactoNombre { get; set; }

        // ======================
        // RECOMPENSA
        // ======================
        [Display(Name = "Recompensa")]
        [DisplayFormat(DataFormatString = "${0:N2}")]
        public decimal? Recompensa { get; set; }

        [Display(Name = "Ofrece Recompensa")]
        public bool TieneRecompensa { get; set; }

        // ======================
        // ESTADO Y FECHAS
        // ======================
        [Display(Name = "Estado")]
        public string Estado { get; set; }

        [Display(Name = "Fecha de Reporte")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}")]
        public DateTime FechaPublicacion { get; set; }

        [Display(Name = "Fecha de Encuentro")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}")]
        public DateTime? FechaEncontrada { get; set; }

        // ======================
        // IMAGEN
        // ======================
        public byte[] ImagenMascota { get; set; }
        public string ImagenBase64 { get; set; }

        // ======================
        // AVISTAMIENTOS
        // ======================
        public List<AvistamientoViewModel> Avistamientos { get; set; } = new List<AvistamientoViewModel>();

        // ======================
        // PERMISOS
        // ======================
        public bool EsPropietario { get; set; }
        public bool EsAdmin { get; set; }
        public bool EsRescatista { get; set; }
        public bool PuedeEditar { get; set; }
        public bool PuedeMarcarEncontrada { get; set; }
        public bool PuedeCerrar { get; set; }

        // ======================
        // CONSTRUCTOR
        // ======================
        public DetalleMascotaPerdidaViewModel()
        {
            Avistamientos = new List<AvistamientoViewModel>();
        }
    }

    public class AvistamientoViewModel
    {
        [Display(Name = "Fecha")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}")]
        public DateTime FechaAvistamiento { get; set; }

        [Display(Name = "Ubicación")]
        public string Ubicacion { get; set; }

        [Display(Name = "Reportante")]
        public string ReportanteNombre { get; set; }

        [Display(Name = "Teléfono")]
        public string ReportanteTelefono { get; set; }

        [Display(Name = "Descripción")]
        public string Descripcion { get; set; }
    }
}