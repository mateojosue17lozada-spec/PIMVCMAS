using System;
using System.ComponentModel.DataAnnotations;

namespace MVCMASCOTAS.Models
{
    public class Configuracion
    {
        [Key]
        public int ConfigId { get; set; }

        [Required(ErrorMessage = "El nombre del refugio es requerido")]
        [Display(Name = "Nombre del Refugio")]
        public string NombreRefugio { get; set; }

        [Required(ErrorMessage = "El email de contacto es requerido")]
        [EmailAddress(ErrorMessage = "Ingrese un email válido")]
        [Display(Name = "Email de Contacto")]
        public string EmailContacto { get; set; }

        [Required(ErrorMessage = "El teléfono es requerido")]
        [Phone(ErrorMessage = "Ingrese un número de teléfono válido")]
        [Display(Name = "Teléfono")]
        public string Telefono { get; set; }

        [Required(ErrorMessage = "La dirección es requerida")]
        [Display(Name = "Dirección")]
        public string Direccion { get; set; }

        [Display(Name = "Mascotas por página")]
        [Range(6, 50, ErrorMessage = "Debe estar entre 6 y 50")]
        public int MascotasPorPagina { get; set; } = 12;

        [Display(Name = "Monto mínimo de donación")]
        [Range(1, 1000, ErrorMessage = "Debe estar entre $1 y $1000")]
        public decimal MontoMinimoDonacion { get; set; } = 5;

        [Display(Name = "Días de validez de solicitud")]
        [Range(1, 365, ErrorMessage = "Debe estar entre 1 y 365 días")]
        public int DiasValidezSolicitud { get; set; } = 30;

        [Display(Name = "Notificar solicitudes")]
        public bool NotificacionesSolicitudes { get; set; } = true;

        [Display(Name = "Notificar donaciones")]
        public bool NotificacionesDonaciones { get; set; } = true;

        [Display(Name = "Notificar rescates")]
        public bool NotificacionesRescates { get; set; } = true;

        public DateTime FechaModificacion { get; set; } = DateTime.Now;
        public int? ModificadoPor { get; set; }
    }
}