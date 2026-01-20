using System;
using System.Collections.Generic;
using System.Linq;
using System;
using System.ComponentModel.DataAnnotations;
using System.Web;

namespace MVCMASCOTAS.Models.ViewModels
{
    public class ProductoViewModel
    {
        public int ProductoId { get; set; }

        [Required(ErrorMessage = "El nombre del producto es requerido")]
        [StringLength(200)]
        [Display(Name = "Nombre del Producto")]
        public string NombreProducto { get; set; }

        [Required(ErrorMessage = "La descripción es requerida")]
        [DataType(DataType.MultilineText)]
        [Display(Name = "Descripción")]
        public string Descripcion { get; set; }

        [Required(ErrorMessage = "El precio es requerido")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor a 0")]
        [DataType(DataType.Currency)]
        [Display(Name = "Precio")]
        public decimal Precio { get; set; }

        [Required(ErrorMessage = "La categoría es requerida")]
        [Display(Name = "Categoría")]
        public int CategoriaId { get; set; }

        [Display(Name = "Stock")]
        [Range(0, int.MaxValue, ErrorMessage = "El stock no puede ser negativo")]
        public int Stock { get; set; }

        [Display(Name = "Stock Mínimo")]
        [Range(0, int.MaxValue, ErrorMessage = "El stock mínimo no puede ser negativo")]
        public int StockMinimo { get; set; }

        [StringLength(50)]
        [Display(Name = "SKU")]
        public string SKU { get; set; }

        [Display(Name = "Imagen Principal")]
        public HttpPostedFileBase ImagenPrincipalFile { get; set; }

        public byte[] ImagenPrincipal { get; set; }

        [Display(Name = "Activo")]
        public bool Activo { get; set; }

        [Display(Name = "Destacado")]
        public bool Destacado { get; set; }

        public DateTime FechaCreacion { get; set; }

        // Para mostrar en vistas
        public string ImagenBase64 { get; set; }
        public string NombreCategoria { get; set; }
        public bool StockBajo { get; set; }

        // Para cálculos
        public int CantidadVendida { get; set; }
        public decimal TotalVentas { get; set; }
    }
}