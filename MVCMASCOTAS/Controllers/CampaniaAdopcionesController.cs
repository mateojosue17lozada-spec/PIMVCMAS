using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using MVCMASCOTAS.Helpers;
using MVCMASCOTAS.Models;

namespace MVCMASCOTAS.Controllers
{
    [AuthorizeRoles("Administrador")]
    public class CampaniaAdopcionesController : Controller
    {
        private RefugioMascotasDBEntities db = new RefugioMascotasDBEntities();

        // GET: CampaniaAdopciones
        [AllowAnonymous]
        public ActionResult Index()
        {
            try
            {
                var campanias = db.CampaniaAdopcion
                    .OrderByDescending(c => c.FechaCreacion)
                    .ToList();

                ViewBag.TotalCampanias = db.CampaniaAdopcion.Count();
                ViewBag.CampaniasActivas = db.CampaniaAdopcion.Count(c => c.Estado == true);

                return View(campanias);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en Index: {ex.Message}");
                TempData["ErrorMessage"] = "Error al cargar las campañas";
                return View(new System.Collections.Generic.List<CampaniaAdopcion>());
            }
        }

        // GET: CampaniaAdopciones/Details/5
        [AllowAnonymous]
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                TempData["ErrorMessage"] = "Debe seleccionar una campaña para ver los detalles";
                return RedirectToAction("Index");
            }

            try
            {
                CampaniaAdopcion campaniaAdopcion = db.CampaniaAdopcion.Find(id);

                if (campaniaAdopcion == null)
                {
                    TempData["ErrorMessage"] = "Campaña no encontrada";
                    return RedirectToAction("Index");
                }

                // Convertir imagen si existe
                if (campaniaAdopcion.Foto != null && campaniaAdopcion.Foto.Length > 0)
                {
                    ViewBag.ImagenBase64 = Convert.ToBase64String(campaniaAdopcion.Foto);
                }

                return View(campaniaAdopcion);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en Details: {ex.Message}");
                TempData["ErrorMessage"] = "Error al cargar los detalles de la campaña";
                return RedirectToAction("Index");
            }
        }

        // GET: CampaniaAdopciones/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: CampaniaAdopciones/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "IdCampania,NombreCampania,Lugar,Fecha,Hora,Actividades,Foto,Estado,FechaCreacion")] CampaniaAdopcion campaniaAdopcion)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Procesar imagen si se proporciona
                    if (Request.Files.Count > 0)
                    {
                        var file = Request.Files[0];
                        if (file != null && file.ContentLength > 0)
                        {
                            if (ImageHelper.IsValidImage(file))
                            {
                                byte[] imagenBytes = ImageHelper.ConvertImageToByteArray(file);
                                campaniaAdopcion.Foto = ImageHelper.ResizeImage(imagenBytes, 800, 600);
                            }
                        }
                    }

                    campaniaAdopcion.FechaCreacion = DateTime.Now;
                    // ✅ CORREGIDO: Estado es bool (no nullable), por defecto true
                    // Si no se especifica en el formulario, será false por defecto del tipo bool
                    // No necesitamos validar HasValue porque bool siempre tiene un valor

                    db.CampaniaAdopcion.Add(campaniaAdopcion);
                    db.SaveChanges();

                    AuditoriaHelper.RegistrarCreacion("CampaniaAdopcion", campaniaAdopcion.IdCampania,
                        $"Campaña: {campaniaAdopcion.NombreCampania}",
                        UserHelper.GetCurrentUserId() ?? 0);

                    TempData["SuccessMessage"] = "Campaña creada exitosamente";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error en Create: {ex.Message}");
                    ModelState.AddModelError("", "Error al crear la campaña");
                }
            }

            return View(campaniaAdopcion);
        }

        // GET: CampaniaAdopciones/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                TempData["ErrorMessage"] = "Debe seleccionar una campaña para editar";
                return RedirectToAction("Index");
            }

            try
            {
                CampaniaAdopcion campaniaAdopcion = db.CampaniaAdopcion.Find(id);

                if (campaniaAdopcion == null)
                {
                    TempData["ErrorMessage"] = "Campaña no encontrada";
                    return RedirectToAction("Index");
                }

                // Convertir imagen si existe
                if (campaniaAdopcion.Foto != null && campaniaAdopcion.Foto.Length > 0)
                {
                    ViewBag.ImagenBase64 = Convert.ToBase64String(campaniaAdopcion.Foto);
                }

                return View(campaniaAdopcion);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en Edit GET: {ex.Message}");
                TempData["ErrorMessage"] = "Error al cargar la campaña";
                return RedirectToAction("Index");
            }
        }

        // POST: CampaniaAdopciones/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "IdCampania,NombreCampania,Lugar,Fecha,Hora,Actividades,Foto,Estado,FechaCreacion")] CampaniaAdopcion campaniaAdopcion)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var campaniaDb = db.CampaniaAdopcion.Find(campaniaAdopcion.IdCampania);

                    if (campaniaDb != null)
                    {
                        // Actualizar campos
                        campaniaDb.NombreCampania = campaniaAdopcion.NombreCampania;
                        campaniaDb.Lugar = campaniaAdopcion.Lugar;
                        campaniaDb.Fecha = campaniaAdopcion.Fecha;
                        campaniaDb.Hora = campaniaAdopcion.Hora;
                        campaniaDb.Actividades = campaniaAdopcion.Actividades;
                        campaniaDb.Estado = campaniaAdopcion.Estado;

                        // Procesar nueva imagen si se proporciona
                        if (Request.Files.Count > 0)
                        {
                            var file = Request.Files[0];
                            if (file != null && file.ContentLength > 0)
                            {
                                if (ImageHelper.IsValidImage(file))
                                {
                                    byte[] imagenBytes = ImageHelper.ConvertImageToByteArray(file);
                                    campaniaDb.Foto = ImageHelper.ResizeImage(imagenBytes, 800, 600);
                                }
                            }
                        }

                        db.Entry(campaniaDb).State = EntityState.Modified;
                        db.SaveChanges();

                        AuditoriaHelper.RegistrarCambioDatos("CampaniaAdopcion", campaniaAdopcion.IdCampania,
                            $"Campaña actualizada: {campaniaAdopcion.NombreCampania}",
                            UserHelper.GetCurrentUserId() ?? 0);

                        TempData["SuccessMessage"] = "Campaña actualizada exitosamente";
                    }

                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error en Edit POST: {ex.Message}");
                    ModelState.AddModelError("", "Error al actualizar la campaña");
                }
            }

            // Mantener imagen en caso de error
            var campaniaActual = db.CampaniaAdopcion.Find(campaniaAdopcion.IdCampania);
            if (campaniaActual?.Foto != null)
            {
                ViewBag.ImagenBase64 = Convert.ToBase64String(campaniaActual.Foto);
            }

            return View(campaniaAdopcion);
        }

        // GET: CampaniaAdopciones/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                TempData["ErrorMessage"] = "Debe seleccionar una campaña para eliminar";
                return RedirectToAction("Index");
            }

            try
            {
                CampaniaAdopcion campaniaAdopcion = db.CampaniaAdopcion.Find(id);

                if (campaniaAdopcion == null)
                {
                    TempData["ErrorMessage"] = "Campaña no encontrada";
                    return RedirectToAction("Index");
                }

                // Convertir imagen si existe
                if (campaniaAdopcion.Foto != null && campaniaAdopcion.Foto.Length > 0)
                {
                    ViewBag.ImagenBase64 = Convert.ToBase64String(campaniaAdopcion.Foto);
                }

                return View(campaniaAdopcion);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en Delete GET: {ex.Message}");
                TempData["ErrorMessage"] = "Error al cargar la campaña";
                return RedirectToAction("Index");
            }
        }

        // POST: CampaniaAdopciones/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                CampaniaAdopcion campaniaAdopcion = db.CampaniaAdopcion.Find(id);

                if (campaniaAdopcion != null)
                {
                    string nombreCampania = campaniaAdopcion.NombreCampania;

                    db.CampaniaAdopcion.Remove(campaniaAdopcion);
                    db.SaveChanges();

                    AuditoriaHelper.RegistrarEliminacion("CampaniaAdopcion", id,
                        $"Campaña eliminada: {nombreCampania}",
                        UserHelper.GetCurrentUserId() ?? 0);

                    TempData["SuccessMessage"] = $"Campaña '{nombreCampania}' eliminada exitosamente";
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en DeleteConfirmed: {ex.Message}");
                TempData["ErrorMessage"] = "Error al eliminar la campaña. Puede tener datos relacionados.";
                return RedirectToAction("Index");
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}