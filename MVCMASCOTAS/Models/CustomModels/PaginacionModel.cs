using System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MVCMASCOTAS.Models.CustomModels
{
    
    public class PaginacionModel<T>
    {
        public List<T> Items { get; set; }
        public int PaginaActual { get; set; }
        public int ElementosPorPagina { get; set; }
        public int TotalElementos { get; set; }
        public int TotalPaginas { get; set; }

        public bool TienePaginaAnterior => PaginaActual > 1;
        public bool TienePaginaSiguiente => PaginaActual < TotalPaginas;

        public int PrimerElemento => (PaginaActual - 1) * ElementosPorPagina + 1;
        public int UltimoElemento => Math.Min(PaginaActual * ElementosPorPagina, TotalElementos);

        public PaginacionModel()
        {
            Items = new List<T>();
        }

        public PaginacionModel(List<T> items, int totalElementos, int paginaActual, int elementosPorPagina)
        {
            Items = items;
            TotalElementos = totalElementos;
            PaginaActual = paginaActual;
            ElementosPorPagina = elementosPorPagina;
            TotalPaginas = (int)Math.Ceiling(totalElementos / (double)elementosPorPagina);
        }

        public List<int> ObtenerPaginasVisibles(int maxPaginasVisibles = 5)
        {
            var paginas = new List<int>();
            int mitad = maxPaginasVisibles / 2;

            int inicio = Math.Max(1, PaginaActual - mitad);
            int fin = Math.Min(TotalPaginas, inicio + maxPaginasVisibles - 1);

            if (fin - inicio < maxPaginasVisibles - 1)
            {
                inicio = Math.Max(1, fin - maxPaginasVisibles + 1);
            }

            for (int i = inicio; i <= fin; i++)
            {
                paginas.Add(i);
            }

            return paginas;
        }
    }
}