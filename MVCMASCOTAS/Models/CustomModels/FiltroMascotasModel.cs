namespace MVCMASCOTAS.Models.CustomModels
{
    /// <summary>
    /// Modelo para filtros de búsqueda de mascotas
    /// </summary>
    public class FiltroMascotasModel
    {
        public string Estado { get; set; }
        public string Especie { get; set; }
        public string Tamanio { get; set; }
        public string Sexo { get; set; }
        public string EdadAproximada { get; set; }
        public string Categoria { get; set; }
        public bool SoloEsterilizados { get; set; }

        public FiltroMascotasModel()
        {
            Estado = "Todos";
            Especie = "Todos";
            Tamanio = "Todos";
            Sexo = "Todos";
            EdadAproximada = "Todos";
            Categoria = "Todos";
            SoloEsterilizados = false;
        }
    }
}
