using System;
using System.ComponentModel.DataAnnotations;

namespace MVCMASCOTAS.Models.ViewModels
{
    public class ConfiguracionViewModel
    {
        [Required(ErrorMessage = "El nombre del refugio es requerido")]
        [StringLength(200, ErrorMessage = "El nombre no puede exceder 200 caracteres")]
        [Display(Name = "Nombre del Refugio")]
        public string NombreRefugio { get; set; }

        [Required(ErrorMessage = "El email de contacto es requerido")]
        [EmailAddress(ErrorMessage = "Ingrese un email válido")]
        [Display(Name = "Email de Contacto")]
        public string EmailContacto { get; set; }

        [Required(ErrorMessage = "El teléfono de contacto es requerido")]
        [StringLength(20, ErrorMessage = "El teléfono no puede exceder 20 caracteres")]
        [Display(Name = "Teléfono de Contacto")]
        public string TelefonoContacto { get; set; }

        [Required(ErrorMessage = "La dirección es requerida")]
        [StringLength(255, ErrorMessage = "La dirección no puede exceder 255 caracteres")]
        [Display(Name = "Dirección del Refugio")]
        public string DireccionRefugio { get; set; }

        [Required(ErrorMessage = "La capacidad máxima es requerida")]
        [Range(1, 1000, ErrorMessage = "La capacidad debe estar entre 1 y 1000")]
        [Display(Name = "Capacidad Máxima de Mascotas")]
        public int CapacidadMaximaMascotas { get; set; }

        [Required(ErrorMessage = "El monto mínimo es requerido")]
        [Range(1, 1000, ErrorMessage = "El monto debe estar entre 1 y 1000")]
        [Display(Name = "Monto Mínimo de Apadrinamiento")]
        public decimal MontoMinimoApadrinamiento { get; set; }

        [StringLength(1000, ErrorMessage = "El mensaje no puede exceder 1000 caracteres")]
        [Display(Name = "Mensaje de Bienvenida")]
        public string MensajeBienvenida { get; set; }

        [Display(Name = "Notificar Nuevas Solicitudes")]
        public bool NotificarSolicitudes { get; set; }

        [Display(Name = "Notificar Nuevas Donaciones")]
        public bool NotificarDonaciones { get; set; }

        [Display(Name = "Notificar Reportes de Rescate")]
        public bool NotificarRescates { get; set; }

        [Range(6, 50, ErrorMessage = "Debe estar entre 6 y 50")]
        [Display(Name = "Mascotas por Página")]
        public int MascotasPorPagina { get; set; }

        [Range(1, 1000, ErrorMessage = "El monto debe ser positivo")]
        [Display(Name = "Monto Mínimo de Donación")]
        public decimal MontoMinimoDonacion { get; set; }

        [Range(1, 365, ErrorMessage = "Debe estar entre 1 y 365 días")]
        [Display(Name = "Días de Validez de Solicitud")]
        public int DiasValidezSolicitud { get; set; }

        // Propiedades adicionales para mostrar en vista
        public DateTime? UltimaModificacion { get; set; }
        public string ModificadoPorUsuario { get; set; }
    }
}