using MVCMASCOTAS.Models;
using MVCMASCOTAS.Models.CustomModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MVCMASCOTAS.Services
{
    public class MascotaService
    {
        private readonly RefugioMascotasEntities db;

        public MascotaService()
        {
            db = new RefugioMascotasEntities();
        }

        // Obtener mascotas disponibles para adopción
        public List<Mascotas> ObtenerMascotasDisponibles()
        {
            return db.Mascotas
                .Where(m => m.Estado == "Disponible" && m.Activo == true)
                .OrderByDescending(m => m.FechaIngreso)
                .ToList();
        }

        // Obtener mascotas con filtros
        public PaginacionModel<Mascotas> ObtenerMascotasFiltradas(FiltroMascotasModel filtro)
        {
            var query = db.Mascotas.Where(m => m.Activo == true);

            // Aplicar filtros
            if (!string.IsNullOrEmpty(filtro.Busqueda))
            {
                query = query.Where(m => m.Nombre.Contains(filtro.Busqueda) ||
                                        m.Raza.Contains(filtro.Busqueda) ||
                                        m.DescripcionGeneral.Contains(filtro.Busqueda));
            }

            if (!string.IsNullOrEmpty(filtro.Especie))
                query = query.Where(m => m.Especie == filtro.Especie);

            if (!string.IsNullOrEmpty(filtro.Sexo))
                query = query.Where(m => m.Sexo == filtro.Sexo);

            if (!string.IsNullOrEmpty(filtro.Tamanio))
                query = query.Where(m => m.Tamanio == filtro.Tamanio);

            if (!string.IsNullOrEmpty(filtro.Categoria))
                query = query.Where(m => m.Categoria == filtro.Categoria);

            if (!string.IsNullOrEmpty(filtro.Estado))
                query = query.Where(m => m.Estado == filtro.Estado);

            if (!string.IsNullOrEmpty(filtro.EdadAproximada))
                query = query.Where(m => m.EdadAproximada == filtro.EdadAproximada);

            if (filtro.Esterilizado.HasValue)
                query = query.Where(m => m.Esterilizado == filtro.Esterilizado.Value);

            if (!string.IsNullOrEmpty(filtro.TipoEspecial))
                query = query.Where(m => m.TipoEspecial == filtro.TipoEspecial);

            // Ordenar
            switch (filtro.OrdenarPor)
            {
                case "Nombre":
                    query = query.OrderBy(m => m.Nombre);
                    break;
                case "FechaIngreso":
                    query = query.OrderByDescending(m => m.FechaIngreso);
                    break;
                default:
                    query = query.OrderByDescending(m => m.FechaIngreso);
                    break;
            }

            // Paginación
            var totalElementos = query.Count();
            var mascotas = query
                .Skip((filtro.Pagina - 1) * filtro.ElementosPorPagina)
                .Take(filtro.ElementosPorPagina)
                .ToList();

            return new PaginacionModel<Mascotas>(mascotas, totalElementos, filtro.Pagina, filtro.ElementosPorPagina);
        }

        // Obtener mascota por ID
        public Mascotas ObtenerMascotaPorId(int mascotaId)
        {
            return db.Mascotas.Find(mascotaId);
        }

        // Crear mascota
        public Mascotas CrearMascota(Mascotas mascota)
        {
            mascota.FechaIngreso = DateTime.Now;
            mascota.Activo = true;
            mascota.Estado = "En Evaluación";

            db.Mascotas.Add(mascota);
            db.SaveChanges();

            return mascota;
        }

        // Actualizar mascota
        public void ActualizarMascota(Mascotas mascota)
        {
            var mascotaExistente = db.Mascotas.Find(mascota.MascotaId);
            if (mascotaExistente != null)
            {
                mascotaExistente.Nombre = mascota.Nombre;
                mascotaExistente.Especie = mascota.Especie;
                mascotaExistente.Raza = mascota.Raza;
                mascotaExistente.Sexo = mascota.Sexo;
                mascotaExistente.EdadAproximada = mascota.EdadAproximada;
                mascotaExistente.Tamanio = mascota.Tamanio;
                mascotaExistente.Color = mascota.Color;
                mascotaExistente.Categoria = mascota.Categoria;
                mascotaExistente.TipoEspecial = mascota.TipoEspecial;
                mascotaExistente.Estado = mascota.Estado;
                mascotaExistente.DescripcionGeneral = mascota.DescripcionGeneral;
                mascotaExistente.CaracteristicasComportamiento = mascota.CaracteristicasComportamiento;
                mascotaExistente.HistoriaRescate = mascota.HistoriaRescate;
                mascotaExistente.Esterilizado = mascota.Esterilizado;
                mascotaExistente.Microchip = mascota.Microchip;
                mascotaExistente.VeterinarioAsignado = mascota.VeterinarioAsignado;

                if (mascota.ImagenPrincipal != null)
                {
                    mascotaExistente.ImagenPrincipal = mascota.ImagenPrincipal;
                }

                db.SaveChanges();
            }
        }

        // Eliminar mascota (soft delete)
        public void EliminarMascota(int mascotaId)
        {
            var mascota = db.Mascotas.Find(mascotaId);
            if (mascota != null)
            {
                mascota.Activo = false;
                db.SaveChanges();
            }
        }

        // Cambiar estado de mascota
        public void CambiarEstado(int mascotaId, string nuevoEstado)
        {
            var mascota = db.Mascotas.Find(mascotaId);
            if (mascota != null)
            {
                mascota.Estado = nuevoEstado;

                if (nuevoEstado == "Disponible")
                {
                    mascota.FechaDisponible = DateTime.Now;
                }
                else if (nuevoEstado == "Adoptado")
                {
                    mascota.FechaAdopcion = DateTime.Now;
                }

                db.SaveChanges();
            }
        }

        // Obtener mascotas por estado
        public List<Mascotas> ObtenerMascotasPorEstado(string estado)
        {
            return db.Mascotas
                .Where(m => m.Estado == estado && m.Activo == true)
                .OrderBy(m => m.Nombre)
                .ToList();
        }

        // Obtener mascotas recientes
        public List<Mascotas> ObtenerMascotasRecientes(int cantidad = 6)
        {
            return db.Mascotas
                .Where(m => m.Estado == "Disponible" && m.Activo == true)
                .OrderByDescending(m => m.FechaIngreso)
                .Take(cantidad)
                .ToList();
        }

        // Buscar mascotas
        public List<Mascotas> BuscarMascotas(string termino)
        {
            return db.Mascotas
                .Where(m => m.Activo == true &&
                           (m.Nombre.Contains(termino) ||
                            m.Especie.Contains(termino) ||
                            m.Raza.Contains(termino) ||
                            m.DescripcionGeneral.Contains(termino)))
                .OrderBy(m => m.Nombre)
                .ToList();
        }

        // Obtener estadísticas
        public int ObtenerTotalMascotas()
        {
            return db.Mascotas.Count(m => m.Activo == true);
        }

        public int ObtenerMascotasPorEstadoCount(string estado)
        {
            return db.Mascotas.Count(m => m.Estado == estado && m.Activo == true);
        }

        public int ObtenerMascotasDisponiblesCount()
        {
            return db.Mascotas.Count(m => m.Estado == "Disponible" && m.Activo == true);
        }

        // Obtener todas las especies únicas
        public List<string> ObtenerEspecies()
        {
            return db.Mascotas
                .Where(m => m.Activo == true && !string.IsNullOrEmpty(m.Especie))
                .Select(m => m.Especie)
                .Distinct()
                .OrderBy(e => e)
                .ToList();
        }

        // Obtener todas las razas de una especie
        public List<string> ObtenerRazasPorEspecie(string especie)
        {
            return db.Mascotas
                .Where(m => m.Activo == true && m.Especie == especie && !string.IsNullOrEmpty(m.Raza))
                .Select(m => m.Raza)
                .Distinct()
                .OrderBy(r => r)
                .ToList();
        }

        public void Dispose()
        {
            db?.Dispose();
        }
    }
}