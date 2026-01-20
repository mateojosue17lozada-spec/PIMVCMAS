using System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MVCMASCOTAS.Models.ViewModels
{
    public class MascotaDetalleViewModel
    {
        // Información básica
        public int MascotaId { get; set; }
        public string Nombre { get; set; }
        public string Especie { get; set; }
        public string Raza { get; set; }
        public string Sexo { get; set; }
        public string EdadAproximada { get; set; }
        public string Tamanio { get; set; }
        public string Color { get; set; }
        public string Categoria { get; set; }
        public string TipoEspecial { get; set; }
        public string Estado { get; set; }

        // Descripciones
        public string DescripcionGeneral { get; set; }
        public string CaracteristicasComportamiento { get; set; }
        public string HistoriaRescate { get; set; }

        // Imágenes
        public string ImagenPrincipalBase64 { get; set; }
        public List<string> ImagenesAdicionalesBase64 { get; set; }

        // Información médica
        public bool Esterilizado { get; set; }
        public string Microchip { get; set; }
        public DateTime? FechaIngreso { get; set; }
        public DateTime? FechaDisponible { get; set; }
        public DateTime? FechaAdopcion { get; set; }

        // Personal relacionado
        public string VeterinarioNombre { get; set; }
        public string RescatistaNombre { get; set; }

        // Historial médico
        public List<HistorialMedicoItem> HistorialMedico { get; set; }
        public List<VacunaItem> Vacunas { get; set; }
        public List<TratamientoItem> Tratamientos { get; set; }

        // Información de adopción
        public bool TieneSolicitudPendiente { get; set; }
        public int? SolicitudUsuarioId { get; set; }
        public bool PuedeSerAdoptado => Estado == "Disponible";

        // Apadrinamiento
        public bool TieneApadrinamiento { get; set; }
        public List<ApadrinamientoItem> Apadrinamientos { get; set; }

        // Estado visual
        public string EstadoColor { get; set; }
        public string EstadoIcono { get; set; }
        public int DiasEnRefugio { get; set; }

        public MascotaDetalleViewModel()
        {
            ImagenesAdicionalesBase64 = new List<string>();
            HistorialMedico = new List<HistorialMedicoItem>();
            Vacunas = new List<VacunaItem>();
            Tratamientos = new List<TratamientoItem>();
            Apadrinamientos = new List<ApadrinamientoItem>();
        }
    }

    public class HistorialMedicoItem
    {
        public DateTime Fecha { get; set; }
        public string TipoConsulta { get; set; }
        public string Diagnostico { get; set; }
        public string Tratamiento { get; set; }
        public string VeterinarioNombre { get; set; }
    }

    public class VacunaItem
    {
        public string NombreVacuna { get; set; }
        public DateTime FechaAplicacion { get; set; }
        public DateTime? FechaProximaDosis { get; set; }
        public string VeterinarioNombre { get; set; }
    }

    public class TratamientoItem
    {
        public string NombreTratamiento { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public string Estado { get; set; }
        public string Descripcion { get; set; }
    }

    public class ApadrinamientoItem
    {
        public string NombrePadrino { get; set; }
        public decimal MontoMensual { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public bool Activo { get; set; }
    }
}