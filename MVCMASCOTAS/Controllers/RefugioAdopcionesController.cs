using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using MVCMASCOTAS.Helpers;
using MVCMASCOTAS.Models;

namespace MVCMASCOTAS.Controllers
{
    [AuthorizeRoles("Administrador")]
    public class RefugioAdopcionesController : Controller
    {
        private RefugioMascotasDBEntities db = new RefugioMascotasDBEntities();

        // GET: RefugioAdopciones
        [AllowAnonymous]
        public ActionResult Index()
        {
            try
            {
                var refugios = db.RefugioAdopcion
                    .OrderBy(r => r.NombreRefugio)
                    .ToList();

                ViewBag.TotalRefugios = db.RefugioAdopcion.Count();
                ViewBag.RefugiosActivos = db.RefugioAdopcion.Count(r => r.Estado == true);

                return View(refugios);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en Index: {ex.Message}");
                TempData["ErrorMessage"] = "Error al cargar los refugios";
                return View(new System.Collections.Generic.List<RefugioAdopcion>());
            }
        }

        // GET: RefugioAdopciones/Details/5
        [AllowAnonymous]
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                TempData["ErrorMessage"] = "Debe seleccionar un refugio para ver los detalles";
                return RedirectToAction("Index");
            }

            try
            {
                RefugioAdopcion refugioAdopcion = db.RefugioAdopcion.Find(id);

                if (refugioAdopcion == null)
                {
                    TempData["ErrorMessage"] = "Refugio no encontrado";
                    return RedirectToAction("Index");
                }

                return View(refugioAdopcion);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en Details: {ex.Message}");
                TempData["ErrorMessage"] = "Error al cargar los detalles del refugio";
                return RedirectToAction("Index");
            }
        }

        // GET: RefugioAdopciones/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: RefugioAdopciones/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "IdRefugio,NombreRefugio,Contacto,Direccion,PersonaEncargada,Estado,FechaRegistro")] RefugioAdopcion refugioAdopcion)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    refugioAdopcion.FechaRegistro = DateTime.Now;
                    // ✅ Estado es bool (no nullable), por defecto será false si no se marca el checkbox
                    // No necesitamos validar HasValue

                    db.RefugioAdopcion.Add(refugioAdopcion);
                    db.SaveChanges();

                    AuditoriaHelper.RegistrarCreacion("RefugioAdopcion", refugioAdopcion.IdRefugio,
                        $"Refugio: {refugioAdopcion.NombreRefugio}",
                        UserHelper.GetCurrentUserId() ?? 0);

                    TempData["SuccessMessage"] = "Refugio creado exitosamente";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error en Create: {ex.Message}");
                    ModelState.AddModelError("", "Error al crear el refugio");
                }
            }

            return View(refugioAdopcion);
        }

        // GET: RefugioAdopciones/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                TempData["ErrorMessage"] = "Debe seleccionar un refugio para editar";
                return RedirectToAction("Index");
            }

            try
            {
                RefugioAdopcion refugioAdopcion = db.RefugioAdopcion.Find(id);

                if (refugioAdopcion == null)
                {
                    TempData["ErrorMessage"] = "Refugio no encontrado";
                    return RedirectToAction("Index");
                }

                return View(refugioAdopcion);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en Edit GET: {ex.Message}");
                TempData["ErrorMessage"] = "Error al cargar el refugio";
                return RedirectToAction("Index");
            }
        }

        // POST: RefugioAdopciones/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "IdRefugio,NombreRefugio,Contacto,Direccion,PersonaEncargada,Estado,FechaRegistro")] RefugioAdopcion refugioAdopcion)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    db.Entry(refugioAdopcion).State = EntityState.Modified;
                    db.SaveChanges();

                    AuditoriaHelper.RegistrarCambioDatos("RefugioAdopcion", refugioAdopcion.IdRefugio,
                        $"Refugio actualizado: {refugioAdopcion.NombreRefugio}",
                        UserHelper.GetCurrentUserId() ?? 0);

                    TempData["SuccessMessage"] = "Refugio actualizado exitosamente";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error en Edit POST: {ex.Message}");
                    ModelState.AddModelError("", "Error al actualizar el refugio");
                }
            }

            return View(refugioAdopcion);
        }

        // GET: RefugioAdopciones/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                TempData["ErrorMessage"] = "Debe seleccionar un refugio para eliminar";
                return RedirectToAction("Index");
            }

            try
            {
                RefugioAdopcion refugioAdopcion = db.RefugioAdopcion.Find(id);

                if (refugioAdopcion == null)
                {
                    TempData["ErrorMessage"] = "Refugio no encontrado";
                    return RedirectToAction("Index");
                }

                return View(refugioAdopcion);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en Delete GET: {ex.Message}");
                TempData["ErrorMessage"] = "Error al cargar el refugio";
                return RedirectToAction("Index");
            }
        }

        // POST: RefugioAdopciones/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                RefugioAdopcion refugioAdopcion = db.RefugioAdopcion.Find(id);

                if (refugioAdopcion != null)
                {
                    string nombreRefugio = refugioAdopcion.NombreRefugio;

                    db.RefugioAdopcion.Remove(refugioAdopcion);
                    db.SaveChanges();

                    AuditoriaHelper.RegistrarEliminacion("RefugioAdopcion", id,
                        $"Refugio eliminado: {nombreRefugio}",
                        UserHelper.GetCurrentUserId() ?? 0);

                    TempData["SuccessMessage"] = $"Refugio '{nombreRefugio}' eliminado exitosamente";
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en DeleteConfirmed: {ex.Message}");
                TempData["ErrorMessage"] = "Error al eliminar el refugio. Puede tener datos relacionados.";
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