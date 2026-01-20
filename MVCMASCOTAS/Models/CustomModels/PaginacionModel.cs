using System.Collections.Generic;

namespace MVCMASCOTAS.Models.CustomModels
{
    /// <summary>
    /// Modelo genérico para paginación
    /// </summary>
    public class PaginacionModel<T>
    {
        public List<T> Items { get; set; }
        public int PaginaActual { get; set; }
        public int TotalPaginas { get; set; }
        public int TotalItems { get; set; }
        public int TamanioPagina { get; set; }

        public bool TienePaginaAnterior => PaginaActual > 1;
        public bool TienePaginaSiguiente => PaginaActual < TotalPaginas;

        public PaginacionModel()
        {
            Items = new List<T>();
        }
    }
}
