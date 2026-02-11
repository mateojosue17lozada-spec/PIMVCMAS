using System;
using System.ComponentModel.DataAnnotations;

namespace MVCMASCOTAS.Models.ViewModels
{
    public class RegistroAuditoriaViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Usuario")]
        public string Usuario { get; set; }

        [Display(Name = "Acción")]
        public string Accion { get; set; }

        [Display(Name = "Tipo")]
        public string Tipo { get; set; } // Creación, Modificación, Eliminación, Acceso, etc.

        [Display(Name = "Módulo")]
        public string Modulo { get; set; } // Usuarios, Mascotas, Adopciones, etc.

        [Display(Name = "Detalles")]
        public string Detalles { get; set; }

        [Display(Name = "Dirección IP")]
        public string IpAddress { get; set; }

        [Display(Name = "Fecha")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}")]
        public DateTime Fecha { get; set; }

        // Propiedades adicionales para visualización
        [Display(Name = "Icono")]
        public string Icono
        {
            get
            {
                // Versión compatible con C# 7.3
                if (Tipo == "Creación")
                    return "fa-plus-circle text-success";
                else if (Tipo == "Modificación")
                    return "fa-edit text-warning";
                else if (Tipo == "Eliminación")
                    return "fa-trash-alt text-danger";
                else if (Tipo == "Acceso")
                    return "fa-sign-in-alt text-info";
                else if (Tipo == "Error")
                    return "fa-exclamation-circle text-danger";
                else
                    return "fa-info-circle text-secondary";
            }
        }

        [Display(Name = "Color de Fondo")]
        public string ColorFondo
        {
            get
            {
                // Versión compatible con C# 7.3
                if (Tipo == "Creación")
                    return "bg-success-light";
                else if (Tipo == "Modificación")
                    return "bg-warning-light";
                else if (Tipo == "Eliminación")
                    return "bg-danger-light";
                else if (Tipo == "Acceso")
                    return "bg-info-light";
                else
                    return "bg-secondary-light";
            }
        }

        [Display(Name = "Resumen")]
        public string Resumen
        {
            get
            {
                if (!string.IsNullOrEmpty(Detalles) && Detalles.Length > 50)
                    return Detalles.Substring(0, 50) + "...";
                return Detalles ?? string.Empty;
            }
        }
    }
}