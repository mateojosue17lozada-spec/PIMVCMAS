using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MVCMASCOTAS.Models.ViewModels
{
    public class CarritoViewModel
    {
        public List<CarritoItemViewModel> Items { get; set; }
        public decimal Subtotal { get; set; }
        public decimal IVA { get; set; }
        public decimal CostoEnvio { get; set; }
        public decimal Total { get; set; }
        public int CantidadItems { get; set; }

        // Información de envío
        public string DireccionEnvio { get; set; }
        public string Ciudad { get; set; }
        public string Provincia { get; set; }
        public string CodigoPostal { get; set; }
        public string Telefono { get; set; }
        public string NotasAdicionales { get; set; }

        // Método de pago
        public string MetodoPago { get; set; }

        public CarritoViewModel()
        {
            Items = new List<CarritoItemViewModel>();
        }

        public void CalcularTotales()
        {
            Subtotal = Items.Sum(i => i.Subtotal);
            IVA = Subtotal * 0.15m; // 15% IVA Ecuador
            Total = Subtotal + IVA + CostoEnvio;
            CantidadItems = Items.Sum(i => i.Cantidad);
        }
    }

    public class CarritoItemViewModel
    {
        public int ProductoId { get; set; }
        public string NombreProducto { get; set; }
        public string Descripcion { get; set; }
        public decimal PrecioUnitario { get; set; }
        public int Cantidad { get; set; }
        public decimal Subtotal { get; set; }
        public string ImagenBase64 { get; set; }
        public int StockDisponible { get; set; }

        public void CalcularSubtotal()
        {
            Subtotal = PrecioUnitario * Cantidad;
        }
    }
}