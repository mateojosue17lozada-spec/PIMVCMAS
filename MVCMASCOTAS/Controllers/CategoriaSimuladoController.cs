using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace MVCMASCOTAS.Controllers
{
    public class CategoriaSimuladoController : Controller
    {
        // Clase modelo temporal (puedes definirla aquí mismo o en un namespace aparte)
        public class CategoriaItem
        {
            public int Id { get; set; }
            public string Nombre { get; set; }
            public string Descripcion { get; set; }
            public bool Activo { get; set; }
        }

        // Lista estática para simular datos
        private static List<CategoriaItem> _categorias = new List<CategoriaItem>
        {
            new CategoriaItem { Id = 1, Nombre = "Perros", Descripcion = "Mascotas caninas", Activo = true },
            new CategoriaItem { Id = 2, Nombre = "Gatos", Descripcion = "Mascotas felinas", Activo = true },
            new CategoriaItem { Id = 3, Nombre = "Aves", Descripcion = "Pájaros y aves pequeñas", Activo = true },
            new CategoriaItem { Id = 4, Nombre = "Roedores", Descripcion = "Hamsters, conejos, etc.", Activo = false }
        };

        // GET: CategoriaSimulado
        public ActionResult Index()
        {
            return View(_categorias.Where(c => c.Activo).ToList());
        }

        // GET: CategoriaSimulado/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: CategoriaSimulado/Create
        [HttpPost]
        public ActionResult Create(CategoriaItem model)
        {
            if (ModelState.IsValid)
            {
                model.Id = _categorias.Max(c => c.Id) + 1;
                model.Activo = true;
                _categorias.Add(model);
                return RedirectToAction("Index");
            }
            return View(model);
        }

        // GET: CategoriaSimulado/Edit/5
        public ActionResult Edit(int id)
        {
            var cat = _categorias.FirstOrDefault(c => c.Id == id);
            if (cat == null) return HttpNotFound();
            return View(cat);
        }

        // POST: CategoriaSimulado/Edit/5
        [HttpPost]
        public ActionResult Edit(CategoriaItem model)
        {
            var cat = _categorias.FirstOrDefault(c => c.Id == model.Id);
            if (cat != null)
            {
                cat.Nombre = model.Nombre;
                cat.Descripcion = model.Descripcion;
                // No cambiamos activo por simplicidad
            }
            return RedirectToAction("Index");
        }

        // GET: CategoriaSimulado/Delete/5
        public ActionResult Delete(int id)
        {
            var cat = _categorias.FirstOrDefault(c => c.Id == id);
            if (cat == null) return HttpNotFound();
            return View(cat);
        }

        // POST: CategoriaSimulado/Delete/5
        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            var cat = _categorias.FirstOrDefault(c => c.Id == id);
            if (cat != null)
            {
                _categorias.Remove(cat);
            }
            return RedirectToAction("Index");
        }
    }
}