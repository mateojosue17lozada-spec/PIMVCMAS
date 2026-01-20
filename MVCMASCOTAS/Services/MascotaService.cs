using System;
using System.Collections.Generic;
using System.Linq;
using MVCMASCOTAS.Models;
using MVCMASCOTAS.Models.CustomModels;

namespace MVCMASCOTAS.Services
{
    /// <summary>
    /// Servicio para lógica de negocio de mascotas
    /// </summary>
    public class MascotaService
    {
        private RefugioMascotasEntities db;

        public MascotaService()
        {
            db = new RefugioMascotasEntities();
        }

        public MascotaService(RefugioMascotasEntities context)
        {
            db = context;
        }

        /// <summary>
        /// Obtiene mascotas disponibles con filtros
        /// </summary>
        public List<Mascotas> ObtenerMascotasDisponibles(FiltroMascotasModel filtro)
        {
            var query = db.Mascotas.Where(m => m.Estado == "Disponible para adopción" && m.Activo);

            if (!string.IsNullOrEmpty(filtro.Especie) && filtro.Especie != "Todos")
            {
                query = query.Where(m => m.Especie == filtro.Especie);
            }

            if (!string.IsNullOrEmpty(filtro.Tamanio) && filtro.Tamanio != "Todos")
            {
                query = query.Where(m => m.Tamanio == filtro.Tamanio);
            }

            if (!string.IsNullOrEmpty(filtro.Sexo) && filtro.Sexo != "Todos")
            {
                query = query.Where(m => m.Sexo == filtro.Sexo);
            }

            if (!string.IsNullOrEmpty(filtro.EdadAproximada) && filtro.EdadAproximada != "Todos")
            {
                query = query.Where(m => m.EdadAproximada == filtro.EdadAproximada);
            }

            if (!string.IsNullOrEmpty(filtro.Categoria) && filtro.Categoria != "Todos")
            {
                query = query.Where(m => m.Categoria == filtro.Categoria);
            }

            if (filtro.SoloEsterilizados)
            {
                query = query.Where(m => m.Esterilizado == true);
            }

            return query.OrderByDescending(m => m.FechaIngreso).ToList();
        }

        /// <summary>
        /// Obtiene mascotas con paginación
        /// </summary>
        public PaginacionModel<Mascotas> ObtenerMascotasPaginadas(FiltroMascotasModel filtro, int pagina, int tamanioPagina)
        {
            var query = db.Mascotas.Where(m => m.Activo);

            // Aplicar filtros
            if (!string.IsNullOrEmpty(filtro.Estado) && filtro.Estado != "Todos")
            {
                query = query.Where(m => m.Estado == filtro.Estado);
            }

            if (!string.IsNullOrEmpty(filtro.Especie) && filtro.Especie != "Todos")
            {
                query = query.Where(m => m.Especie == filtro.Especie);
            }

            if (!string.IsNullOrEmpty(filtro.Tamanio) && filtro.Tamanio != "Todos")
            {
                query = query.Where(m => m.Tamanio == filtro.Tamanio);
            }

            if (!string.IsNullOrEmpty(filtro.Sexo) && filtro.Sexo != "Todos")
            {
                query = query.Where(m => m.Sexo == filtro.Sexo);
            }

            int totalItems = query.Count();
            int totalPaginas = (int)Math.Ceiling(totalItems / (double)tamanioPagina);

            var items = query
                .OrderByDescending(m => m.FechaIngreso)
                .Skip((pagina - 1) * tamanioPagina)
                .Take(tamanioPagina)
                .ToList();

            return new PaginacionModel<Mascotas>
            {
                Items = items,
                PaginaActual = pagina,
                TotalPaginas = totalPaginas,
                TotalItems = totalItems,
                TamanioPagina = tamanioPagina
            };
        }

        /// <summary>
        /// Crea una nueva mascota
        /// </summary>
        public Mascotas CrearMascota(Mascotas mascota, int? rescatistaId)
        {
            mascota.FechaIngreso = DateTime.Now;
            mascota.Activo = true;
            mascota.RescatistaId = rescatistaId;

            if (string.IsNullOrEmpty(mascota.Estado))
            {
                mascota.Estado = "Rescatada";
            }

            db.Mascotas.Add(mascota);
            db.SaveChanges();

            return mascota;
        }

        /// <summary>
        /// Actualiza la información de una mascota
        /// </summary>
        public bool ActualizarMascota(Mascotas mascota)
        {
            var mascotaExistente = db.Mascotas.Find(mascota.MascotaId);

            if (mascotaExistente == null)
            {
                return false;
            }

            // Actualizar campos
            mascotaExistente.Nombre = mascota.Nombre;
            mascotaExistente.Especie = mascota.Especie;
            mascotaExistente.Raza = mascota.Raza;
            mascotaExistente.Sexo = mascota.Sexo;
            mascotaExistente.EdadAproximada = mascota.EdadAproximada;
            mascotaExistente.Tamanio = mascota.Tamanio;
            mascotaExistente.Color = mascota.Color;
            mascotaExistente.Categoria = mascota.Categoria;
            mascotaExistente.TipoEspecial = mascota.TipoEspecial;
            mascotaExistente.DescripcionGeneral = mascota.DescripcionGeneral;
            mascotaExistente.CaracteristicasComportamiento = mascota.CaracteristicasComportamiento;
            mascotaExistente.HistoriaRescate = mascota.HistoriaRescate;
            mascotaExistente.Esterilizado = mascota.Esterilizado;
            mascotaExistente.Microchip = mascota.Microchip;

            if (mascota.ImagenPrincipal != null)
            {
                mascotaExistente.ImagenPrincipal = mascota.ImagenPrincipal;
            }

            db.SaveChanges();

            return true;
        }

        /// <summary>
        /// Cambia el estado de una mascota
        /// </summary>
        public bool CambiarEstado(int mascotaId, string nuevoEstado)
        {
            var mascota = db.Mascotas.Find(mascotaId);

            if (mascota == null)
            {
                return false;
            }

            mascota.Estado = nuevoEstado;

            if (nuevoEstado == "Adoptada")
            {
                mascota.FechaAdopcion = DateTime.Now;
            }

            db.SaveChanges();

            return true;
        }

        /// <summary>
        /// Desactiva una mascota (borrado lógico)
        /// </summary>
        public bool DesactivarMascota(int mascotaId)
        {
            var mascota = db.Mascotas.Find(mascotaId);

            if (mascota == null)
            {
                return false;
            }

            mascota.Activo = false;
            db.SaveChanges();

            return true;
        }

        /// <summary>
        /// Obtiene mascotas por estado
        /// </summary>
        public List<Mascotas> ObtenerMascotasPorEstado(string estado)
        {
            return db.Mascotas
                .Where(m => m.Estado == estado && m.Activo)
                .OrderByDescending(m => m.FechaIngreso)
                .ToList();
        }

        /// <summary>
        /// Obtiene mascotas rescatadas por un usuario
        /// </summary>
        public List<Mascotas> ObtenerMascotasRescatadasPor(int rescatistaId)
        {
            return db.Mascotas
                .Where(m => m.RescatistaId == rescatistaId && m.Activo)
                .OrderByDescending(m => m.FechaIngreso)
                .ToList();
        }

        /// <summary>
        /// Busca mascotas por nombre o características
        /// </summary>
        public List<Mascotas> BuscarMascotas(string termino)
        {
            if (string.IsNullOrEmpty(termino))
            {
                return new List<Mascotas>();
            }

            return db.Mascotas
                .Where(m => m.Activo &&
                       (m.Nombre.Contains(termino) ||
                        m.Raza.Contains(termino) ||
                        m.Color.Contains(termino) ||
                        m.DescripcionGeneral.Contains(termino)))
                .OrderByDescending(m => m.FechaIngreso)
                .ToList();
        }

        /// <summary>
        /// Obtiene estadísticas de mascotas
        /// </summary>
        public Dictionary<string, object> ObtenerEstadisticas()
        {
            var stats = new Dictionary<string, object>
            {
                ["TotalMascotas"] = db.Mascotas.Count(m => m.Activo),
                ["Disponibles"] = db.Mascotas.Count(m => m.Estado == "Disponible para adopción" && m.Activo),
                ["EnTratamiento"] = db.Mascotas.Count(m => m.Estado == "En tratamiento" && m.Activo),
                ["Adoptadas"] = db.Mascotas.Count(m => m.Estado == "Adoptada"),
                ["Rescatadas"] = db.Mascotas.Count(m => m.Estado == "Rescatada" && m.Activo),
                ["TotalPerros"] = db.Mascotas.Count(m => m.Especie == "Perro" && m.Activo),
                ["TotalGatos"] = db.Mascotas.Count(m => m.Especie == "Gato" && m.Activo),
                ["Esterilizadas"] = db.Mascotas.Count(m => m.Esterilizado == true && m.Activo),
                ["ConMicrochip"] = db.Mascotas.Count(m => !string.IsNullOrEmpty(m.Microchip) && m.Activo)
            };

            return stats;
        }

        /// <summary>
        /// Obtiene mascotas destacadas (últimas rescatadas o más antiguas sin adoptar)
        /// </summary>
        public List<Mascotas> ObtenerMascotasDestacadas(int cantidad = 6)
        {
            return db.Mascotas
                .Where(m => m.Estado == "Disponible para adopción" && m.Activo)
                .OrderByDescending(m => m.FechaIngreso)
                .Take(cantidad)
                .ToList();
        }

        /// <summary>
        /// Verifica si una mascota está disponible para adopción
        /// </summary>
        public bool EstaDisponibleParaAdopcion(int mascotaId)
        {
            var mascota = db.Mascotas.Find(mascotaId);
            return mascota != null && mascota.Estado == "Disponible para adopción" && mascota.Activo;
        }

        /// <summary>
        /// Obtiene imágenes adicionales de una mascota
        /// </summary>
        public List<ImagenesAdicionales> ObtenerImagenesAdicionales(int mascotaId)
        {
            return db.ImagenesAdicionales
                .Where(i => i.EntidadTipo == "Mascota" && i.EntidadId == mascotaId)
                .OrderBy(i => i.Orden)
                .ToList();
        }

        /// <summary>
        /// Agrega una imagen adicional a una mascota
        /// </summary>
        public ImagenesAdicionales AgregarImagenAdicional(int mascotaId, byte[] imagen, string descripcion)
        {
            // Obtener el siguiente orden
            int siguienteOrden = db.ImagenesAdicionales
                .Where(i => i.EntidadTipo == "Mascota" && i.EntidadId == mascotaId)
                .Max(i => (int?)i.Orden) ?? 0;

            var imagenAdicional = new ImagenesAdicionales
            {
                EntidadTipo = "Mascota",
                EntidadId = mascotaId,
                Imagen = imagen,
                Descripcion = descripcion,
                Orden = siguienteOrden + 1,
                FechaSubida = DateTime.Now
            };

            db.ImagenesAdicionales.Add(imagenAdicional);
            db.SaveChanges();

            return imagenAdicional;
        }

        /// <summary>
        /// Obtiene mascotas que necesitan atención urgente
        /// </summary>
        public List<Mascotas> ObtenerMascotasConAtencionUrgente()
        {
            // Mascotas en tratamiento o recién rescatadas
            return db.Mascotas
                .Where(m => (m.Estado == "En tratamiento" || m.Estado == "Rescatada") && m.Activo)
                .OrderBy(m => m.FechaIngreso)
                .ToList();
        }

        /// <summary>
        /// Calcula la edad aproximada en meses basándose en la descripción
        /// </summary>
        public int? CalcularEdadEnMeses(string edadAproximada)
        {
            if (string.IsNullOrEmpty(edadAproximada))
                return null;

            if (edadAproximada.Contains("Cachorro") || edadAproximada.Contains("0-1 año"))
                return 6;
            else if (edadAproximada.Contains("1-3 años") || edadAproximada.Contains("Joven"))
                return 24;
            else if (edadAproximada.Contains("3-7 años") || edadAproximada.Contains("Adulto"))
                return 60;
            else if (edadAproximada.Contains("7+ años") || edadAproximada.Contains("Senior"))
                return 96;

            return null;
        }

        public void Dispose()
        {
            db?.Dispose();
        }
    }
}
