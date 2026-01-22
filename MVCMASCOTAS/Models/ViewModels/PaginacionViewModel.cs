using System.Collections.Generic;

namespace MVCMASCOTAS.Models.ViewModels
{
    public class PaginacionViewModel
    {
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalItems { get; set; }
        public int PageSize { get; set; }
        public string BaseUrl { get; set; }
        public string QueryString { get; set; }

        // Constructor
        public PaginacionViewModel()
        {
            PageSize = 10; // Valor por defecto
        }

        // Constructor completo
        public PaginacionViewModel(int currentPage, int totalItems, int pageSize, string baseUrl, string queryString = "")
        {
            CurrentPage = currentPage;
            TotalItems = totalItems;
            PageSize = pageSize;
            BaseUrl = baseUrl;
            QueryString = queryString;
            TotalPages = (int)System.Math.Ceiling((decimal)totalItems / pageSize);
        }
    }
}