using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using English_language_center_management_system.Models;

namespace English_language_center_management_system.Controllers
{
    public class CourseMaterialsController : Controller
    {
        private EnglishCenterDbContext db = new EnglishCenterDbContext();

        // GET: CourseMaterials
        public ActionResult Index()
        {
            var courseMaterials = db.CourseMaterials.Include(c => c.Class);
            return View(courseMaterials.ToList());
        }

        // GET: CourseMaterials/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            CourseMaterial courseMaterial = db.CourseMaterials.Find(id);
            if (courseMaterial == null)
            {
                return HttpNotFound();
            }
            return View(courseMaterial);
        }

        // GET: CourseMaterials/Create
        public ActionResult Create()
        {
            ViewBag.ClassId = new SelectList(db.Classes, "Id", "ClassName");
            return View();
        }

        // POST: CourseMaterials/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,ClassId,Title,FileUrl,UploadedAt")] CourseMaterial courseMaterial)
        {
            if (ModelState.IsValid)
            {
                db.CourseMaterials.Add(courseMaterial);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.ClassId = new SelectList(db.Classes, "Id", "ClassName", courseMaterial.ClassId);
            return View(courseMaterial);
        }

        // GET: CourseMaterials/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            CourseMaterial courseMaterial = db.CourseMaterials.Find(id);
            if (courseMaterial == null)
            {
                return HttpNotFound();
            }
            ViewBag.ClassId = new SelectList(db.Classes, "Id", "ClassName", courseMaterial.ClassId);
            return View(courseMaterial);
        }

        // POST: CourseMaterials/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,ClassId,Title,FileUrl,UploadedAt")] CourseMaterial courseMaterial)
        {
            if (ModelState.IsValid)
            {
                db.Entry(courseMaterial).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.ClassId = new SelectList(db.Classes, "Id", "ClassName", courseMaterial.ClassId);
            return View(courseMaterial);
        }

        // GET: CourseMaterials/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            CourseMaterial courseMaterial = db.CourseMaterials.Find(id);
            if (courseMaterial == null)
            {
                return HttpNotFound();
            }
            return View(courseMaterial);
        }

        // POST: CourseMaterials/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            CourseMaterial courseMaterial = db.CourseMaterials.Find(id);
            db.CourseMaterials.Remove(courseMaterial);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
