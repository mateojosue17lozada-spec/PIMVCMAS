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

        // Para mostrar en vistas
        public string NombreDonante { get; set; }
        public string EmailDonante { get; set; }
        public string ComprobanteElectronico { get; set; }

        // Datos del apadrinamiento (si aplica)
        public int? MascotaId { get; set; }
        public string NombreMascota { get; set; }
        public string ImagenMascotaBase64 { get; set; }

        [Display(Name = "Duración del Apadrinamiento (meses)")]
        public int? DuracionMeses { get; set; }

        // Descripción para donaciones en especie
        [Display(Name = "Descripción del Artículo")]
        [DataType(DataType.MultilineText)]
        [StringLength(500)]
        public string DescripcionArticulo { get; set; }

        [Display(Name = "Cantidad")]
        public int? Cantidad { get; set; }
    }
}