// =====================================================================
// ARCHIVO NUEVO: Models/ViewModels/SeguimientoAdopcionViewModel.cs
// Crear este archivo en la carpeta Models/ViewModels del proyecto
// =====================================================================

using System;

namespace MVCMASCOTAS.Models.ViewModels
{
    public class SeguimientoAdopcionViewModel
    {
        public int MascotaId { get; set; }
        public string Nombre { get; set; }
        public string Especie { get; set; }
        public string Raza { get; set; }
        public string NumeroContrato { get; set; }
        public string AdoptanteNombre { get; set; }
        public int? ContratoId { get; set; }
        public DateTime? UltimoSeguimiento { get; set; }
        public DateTime? ProximoSeguimiento { get; set; }
        public bool RequiereIntervencion { get; set; }
        public string EstadoSeguimiento { get; set; }
        public bool TieneSeguimientos { get; set; }
        public bool TieneContrato { get; set; }
    }
}