using System;
using System.ComponentModel.DataAnnotations;

namespace MVCMASCOTAS.Models.ViewModels
{
    public class DonacionViewModel
    {
        public int DonacionId { get; set; }

        [Required(ErrorMessage = "El tipo de donación es requerido")]
        [Display(Name = "Tipo de Donación")]
        public string TipoDonacion { get; set; }

        [Display(Name = "Monto")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El monto debe ser mayor a 0")]
        [DataType(DataType.Currency)]
        public decimal Monto { get; set; }

        [Display(Name = "Frecuencia")]
        public string Frecuencia { get; set; }

        [Display(Name = "Método de Pago")]
        public string MetodoPago { get; set; }

        [Display(Name = "Número de Transacción")]
        [StringLength(100)]
        public string NumeroTransaccion { get; set; }

        [Display(Name = "Estado")]
        public string Estado { get; set; }

        [Display(Name = "Donación Anónima")]
        public bool Anonima { get; set; }

        [Display(Name = "Mensaje (Opcional)")]
        [DataType(DataType.MultilineText)]
        [StringLength(500)]
        public string Mensaje { get; set; }

        public DateTime FechaDonacion { get; set; }

        // Datos del donante (REQUERIDOS)
        [Required(ErrorMessage = "El nombre del donante es requerido")]
        [Display(Name = "Nombre Completo")]
        [StringLength(150, ErrorMessage = "El nombre no puede exceder 150 caracteres")]
        public string NombreDonante { get; set; }

        [Required(ErrorMessage = "El email es requerido")]
        [EmailAddress(ErrorMessage = "Email inválido")]
        [Display(Name = "Email")]
        [StringLength(150, ErrorMessage = "El email no puede exceder 150 caracteres")]
        public string EmailDonante { get; set; }

        [Required(ErrorMessage = "El teléfono es requerido")]
        [Phone(ErrorMessage = "Teléfono inválido")]
        [Display(Name = "Teléfono")]
        [StringLength(20, ErrorMessage = "El teléfono no puede exceder 20 caracteres")]
        public string TelefonoDonante { get; set; }

        [Display(Name = "Comprobante Electrónico")]
        public string ComprobanteElectronico { get; set; }

        // Datos del apadrinamiento (si aplica)
        public int? MascotaId { get; set; }
        public string NombreMascota { get; set; }
        public string ImagenMascotaBase64 { get; set; }

        [Display(Name = "Duración (meses)")]
        [Range(1, 36, ErrorMessage = "La duración debe ser entre 1 y 36 meses")]
        public int? DuracionMeses { get; set; }

        // Datos para donaciones en especie
        [Display(Name = "Descripción del Artículo")]
        [DataType(DataType.MultilineText)]
        [StringLength(500)]
        public string DescripcionArticulo { get; set; }

        [Display(Name = "Cantidad")]
        [Range(1, 1000, ErrorMessage = "La cantidad debe ser entre 1 y 1000")]
        public int? Cantidad { get; set; }

        // Propiedades auxiliares para validación condicional
        public bool EsDonacionMonetaria => TipoDonacion == "Monetaria";
        public bool EsDonacionEspecie => TipoDonacion == "Especie";
        public bool EsApadrinamiento => TipoDonacion == "Apadrinamiento";
    }
}