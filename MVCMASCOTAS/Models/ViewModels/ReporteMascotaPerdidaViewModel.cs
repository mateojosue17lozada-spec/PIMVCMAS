using System;
using System.ComponentModel.DataAnnotations;
using System.Web;

namespace MVCMASCOTAS.Models.ViewModels
{
    public class ReporteMascotaPerdidaViewModel
    {
        // ======================
        // DATOS DE LA MASCOTA
        // ======================

        [Required(ErrorMessage = "El nombre de la mascota es requerido")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres")]
        [Display(Name = "Nombre de la Mascota")]
        public string NombreMascota { get; set; }

        [Required(ErrorMessage = "La especie es requerida")]
        [StringLength(50, ErrorMessage = "La especie no puede exceder 50 caracteres")]
        [Display(Name = "Especie")]
        public string Especie { get; set; }

        [StringLength(100, ErrorMessage = "La raza no puede exceder 100 caracteres")]
        [Display(Name = "Raza")]
        public string Raza { get; set; }

        // CAMBIO AQUÍ: Cambiar de char a string
        [Required(ErrorMessage = "El sexo es requerido")]
        [Display(Name = "Sexo")]
        public string Sexo { get; set; }

        [StringLength(50, ErrorMessage = "La edad no puede exceder 50 caracteres")]
        [Display(Name = "Edad")]
        public string Edad { get; set; }

        [StringLength(100, ErrorMessage = "El color no puede exceder 100 caracteres")]
        [Display(Name = "Color")]
        public string Color { get; set; }

        [Required(ErrorMessage = "Por favor describe características distintivas")]
        [StringLength(1000, ErrorMessage = "La descripción no puede exceder 1000 caracteres")]
        [Display(Name = "Características Distintivas")]
        public string CaracteristicasDistintivas { get; set; }

        // ======================
        // DATOS DE LA PÉRDIDA
        // ======================

        [Required(ErrorMessage = "La fecha de pérdida es requerida")]
        [Display(Name = "Fecha de Pérdida")]
        [DataType(DataType.DateTime)]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}", ApplyFormatInEditMode = true)]
        public DateTime FechaPerdida { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "La ubicación de pérdida es requerida")]
        [StringLength(255, ErrorMessage = "La ubicación no puede exceder 255 caracteres")]
        [Display(Name = "Ubicación de la Pérdida")]
        public string UbicacionPerdida { get; set; }

        [StringLength(255, ErrorMessage = "Las coordenadas no pueden exceder 100 caracteres")]
        [Display(Name = "Coordenadas GPS (opcional)")]
        public string CoordenadasGPS { get; set; }

        [StringLength(500, ErrorMessage = "Las observaciones no pueden exceder 500 caracteres")]
        [Display(Name = "Observaciones Adicionales")]
        public string Observaciones { get; set; }

        [Display(Name = "Ofrecer Recompensa")]
        public bool OfrecerRecompensa { get; set; }

        [Display(Name = "Monto de Recompensa (USD)")]
        [Range(0, 10000, ErrorMessage = "El monto debe estar entre 0 y 10,000")]
        [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
        public decimal? Recompensa { get; set; }

        // ======================
        // DATOS DEL PROPIETARIO/CONTACTO
        // ======================

        [Required(ErrorMessage = "El nombre de contacto es requerido")]
        [StringLength(150, ErrorMessage = "El nombre no puede exceder 150 caracteres")]
        [Display(Name = "Nombre de Contacto")]
        public string ContactoNombre { get; set; }

        [Required(ErrorMessage = "El teléfono de contacto es requerido")]
        [StringLength(20, ErrorMessage = "El teléfono no puede exceder 20 caracteres")]
        [Display(Name = "Teléfono de Contacto")]
        [Phone(ErrorMessage = "Formato de teléfono inválido")]
        public string ContactoTelefono { get; set; }

        [StringLength(150, ErrorMessage = "El email no puede exceder 150 caracteres")]
        [Display(Name = "Email de Contacto (opcional)")]
        [EmailAddress(ErrorMessage = "Formato de email inválido")]
        public string ContactoEmail { get; set; }

        // ======================
        // IMAGEN DE LA MASCOTA
        // ======================

        [Display(Name = "Foto de la Mascota")]
        [DataType(DataType.Upload)]
        public HttpPostedFileBase ImagenMascota { get; set; }

        // ======================
        // VALIDACIÓN
        // ======================

        [Required(ErrorMessage = "Debes aceptar los términos y condiciones")]
        [Display(Name = "Acepto los términos y condiciones")]
        public bool AceptaTerminos { get; set; }

        // ======================
        // MÉTODOS DE AYUDA
        // ======================

        public bool TieneRecompensa()
        {
            return OfrecerRecompensa && Recompensa.HasValue && Recompensa.Value > 0;
        }
    }
}