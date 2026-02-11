// Models/ViewModels/ApadrinarViewModels.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace MVCMASCOTAS.Models.ViewModels
{
    // Para la vista Index - Lista de mascotas
    public class MascotaResumenViewModel
    {
        public int MascotaId { get; set; }
        public string Nombre { get; set; }
        public string Especie { get; set; }
        public string Sexo { get; set; }
        public string EdadAproximada { get; set; }
        public string Estado { get; set; }
        public string DescripcionGeneral { get; set; }
        public byte[] ImagenPrincipal { get; set; }
        public string Raza { get; set; }
        public string Color { get; set; }
        public string Tamanio { get; set; }
        public bool Esterilizado { get; set; }
    }

    // Para la vista Confirmar - Formulario de confirmación
    public class ConfirmarApadrinamientoViewModel
    {
        [Required(ErrorMessage = "El ID de la mascota es requerido")]
        public int MascotaId { get; set; }

        [Display(Name = "Nombre de la Mascota")]
        public string MascotaNombre { get; set; }

        [Display(Name = "Especie")]
        public string MascotaEspecie { get; set; }

        // Propiedad temporal como string para capturar la entrada del usuario
        [Required(ErrorMessage = "El monto mensual es requerido")]
        [RegularExpression(@"^\d+([\.,]\d{1,2})?$", ErrorMessage = "Formato inválido. Use 10.00 o 10,00")]
        [Display(Name = "Monto Mensual (USD)")]
        public string MontoMensualTexto { get; set; } = "10.00";

        // Propiedad real que se usará después de parsear
        [Range(10.00, 10000.00, ErrorMessage = "El monto debe ser entre $10.00 y $10,000.00")]
        public decimal MontoMensual { get; set; } = 10.00m;

        // Método para parsear el texto a decimal
        public void ParsearMonto()
        {
            if (!string.IsNullOrEmpty(MontoMensualTexto))
            {
                string montoLimpio = MontoMensualTexto
                    .Replace(',', '.')
                    .Replace(" ", "")
                    .Trim();

                if (decimal.TryParse(montoLimpio, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal resultado))
                {
                    MontoMensual = Math.Round(resultado, 2);
                }
                else
                {
                    // Si no se puede parsear, usar el valor por defecto
                    MontoMensual = 10.00m;
                }
            }
        }

        [Required(ErrorMessage = "El método de pago es requerido")]
        [Display(Name = "Método de Pago")]
        public string MetodoPagoSeleccionado { get; set; }

        [Display(Name = "Métodos de Pago Disponibles")]
        public List<MetodoPagoViewModel> MetodosPago { get; set; }

        [Required(ErrorMessage = "Debes aceptar los términos y condiciones")]
        [Display(Name = "Acepto los términos y condiciones")]
        public bool AceptaTerminos { get; set; }

        // Información del usuario
        public string UsuarioNombre { get; set; }
        public string UsuarioEmail { get; set; }
        public string UsuarioTelefono { get; set; }
    }

    public class MetodoPagoViewModel
    {
        public string Id { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public bool Disponible { get; set; } = true;
    }

    // Para MisApadrinamientos
    public class MisApadrinamientosViewModel
    {
        public List<ApadrinamientoResumenViewModel> ApadrinamientosActivos { get; set; }
        public List<ApadrinamientoResumenViewModel> ApadrinamientosHistorial { get; set; }
        public ResumenFinancieroViewModel Resumen { get; set; }
    }

    public class ApadrinamientoResumenViewModel
    {
        public int ApadrinamientoId { get; set; }
        public int MascotaId { get; set; }
        public string MascotaNombre { get; set; }
        public string MascotaEspecie { get; set; }
        public byte[] MascotaImagen { get; set; }
        public decimal MontoMensual { get; set; }
        public DateTime FechaInicio { get; set; }
        public string Estado { get; set; }
        public decimal TotalContribuido { get; set; }
        public int MesesActivo { get; set; }
        public DateTime? ProximoPago { get; set; }
    }

    public class ResumenFinancieroViewModel
    {
        public decimal TotalMensual { get; set; }
        public decimal TotalAnual { get; set; }
        public decimal TotalHistorico { get; set; }
        public int MascotasApadrinadas { get; set; }
        public int PagosRealizados { get; set; }
        public DateTime? ProximaFechaPago { get; set; }
    }

    // Para Detalle de Apadrinamiento
    public class ApadrinamientoDetalleViewModel
    {
        public int ApadrinamientoId { get; set; }
        public int MascotaId { get; set; }
        public string MascotaNombre { get; set; }
        public string MascotaEspecie { get; set; }
        public byte[] MascotaImagen { get; set; }
        public decimal MontoMensual { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public string Estado { get; set; }
        public string MetodoPagoPreferido { get; set; }
        public int DiaCobroMensual { get; set; }
        public string Observaciones { get; set; }
        public decimal TotalContribuido { get; set; }
        public List<PagoViewModel> Pagos { get; set; }
    }

    public class PagoViewModel
    {
        public int PagoId { get; set; }
        public DateTime FechaPago { get; set; }
        public decimal Monto { get; set; }
        public string MesPagado { get; set; }
        public string MetodoPago { get; set; }
        public string NumeroTransaccion { get; set; }
        public string Estado { get; set; }
    }
}