using System;
using System.ComponentModel.DataAnnotations;

namespace MVCMASCOTAS.Models.ViewModels
{
    public class ConfiguracionResumenViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "La clave es requerida")]
        [StringLength(100, ErrorMessage = "La clave no puede exceder 100 caracteres")]
        [Display(Name = "Clave")]
        public string Clave { get; set; }

        [Required(ErrorMessage = "El valor es requerido")]
        [Display(Name = "Valor")]
        public string Valor { get; set; }

        [StringLength(500, ErrorMessage = "La descripción no puede exceder 500 caracteres")]
        [Display(Name = "Descripción")]
        public string Descripcion { get; set; }

        [Display(Name = "Categoría")]
        public string Categoria { get; set; } // General, Seguridad, Notificaciones, etc.

        [Display(Name = "Es Crítica")]
        public bool EsCritica { get; set; }

        [Display(Name = "Última Modificación")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}")]
        public DateTime UltimaModificacion { get; set; }

        [Display(Name = "Modificado Por")]
        public string ModificadoPor { get; set; }

        // Propiedades para UI
        [Display(Name = "Icono")]
        public string Icono
        {
            get
            {
                // Versión compatible con C# 7.3
                if (Categoria == "General")
                    return "fa-cog";
                else if (Categoria == "Seguridad")
                    return "fa-shield-alt";
                else if (Categoria == "Notificaciones")
                    return "fa-bell";
                else if (Categoria == "Email")
                    return "fa-envelope";
                else if (Categoria == "Base de Datos")
                    return "fa-database";
                else if (Categoria == "Sistema")
                    return "fa-server";
                else
                    return "fa-sliders-h";
            }
        }

        [Display(Name = "Color")]
        public string ColorClase
        {
            get
            {
                return EsCritica ? "text-danger" : "text-success";
            }
        }

        [Display(Name = "Valor Corto")]
        public string ValorCorto
        {
            get
            {
                if (!string.IsNullOrEmpty(Valor) && Valor.Length > 30)
                    return Valor.Substring(0, 30) + "...";
                return Valor ?? string.Empty;
            }
        }
    }
}