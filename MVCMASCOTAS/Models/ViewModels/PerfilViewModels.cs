using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MVCMASCOTAS.Models.ViewModels
{
    public class SolicitudPerfilViewModel
    {
        public int SolicitudId { get; set; }
        public string NombreMascota { get; set; }
        public string Especie { get; set; }
        public string FechaSolicitud { get; set; }
        public string Estado { get; set; }
    }

    public class DonacionPerfilViewModel
    {
        public string TipoDonacion { get; set; }
        public decimal? Monto { get; set; }
        public string FechaDonacion { get; set; }
        public string Estado { get; set; }
    }

    public class ApadrinamientoPerfilViewModel
    {
        public string NombreMascota { get; set; }
        public string Especie { get; set; }
        public decimal? MontoMensual { get; set; }
        public string FechaInicio { get; set; }
        public string Estado { get; set; }
    }

    public class ActividadPerfilViewModel
    {
        public string NombreActividad { get; set; }
        public string TipoActividad { get; set; }
        public string FechaActividad { get; set; }
        public string Estado { get; set; }
    }

    public class PedidoPerfilViewModel
    {
        public string NumeroPedido { get; set; }
        public string FechaPedido { get; set; }
        public decimal? Total { get; set; }
        public string Estado { get; set; }
    }

    public class ReportePerfilViewModel
    {
        public string TipoAnimal { get; set; }
        public string UbicacionReporte { get; set; }
        public string FechaReporte { get; set; }
        public string Estado { get; set; }
    }
}