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
    public class ClassSessionsController : Controller
    {
        private EnglishCenterDbContext db = new EnglishCenterDbContext();

        // GET: ClassSessions
        public ActionResult Index()
        {
            var classSessions = db.ClassSessions.Include(c => c.Class);
            return View(classSessions.ToList());
        }

        // GET: ClassSessions/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ClassSession classSession = db.ClassSessions.Find(id);
            if (classSession == null)
            {
                return HttpNotFound();
            }
            return View(classSession);
        }

        // GET: ClassSessions/Create
        public ActionResult Create()
        {
            ViewBag.ClassId = new SelectList(db.Classes, "Id", "ClassName");
            return View();
        }

        // POST: ClassSessions/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,ClassId,SessionDate,StartTime,EndTime,Room")] ClassSession classSession)
        {
            if (ModelState.IsValid)
            {
                db.ClassSessions.Add(classSession);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.ClassId = new SelectList(db.Classes, "Id", "ClassName", classSession.ClassId);
            return View(classSession);
        }

        // GET: ClassSessions/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ClassSession classSession = db.ClassSessions.Find(id);
            if (classSession == null)
            {
                return HttpNotFound();
            }
            ViewBag.ClassId = new SelectList(db.Classes, "Id", "ClassName", classSession.ClassId);
            return View(classSession);
        }

        // POST: ClassSessions/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,ClassId,SessionDate,StartTime,EndTime,Room")] ClassSession classSession)
        {
            if (ModelState.IsValid)
            {
                db.Entry(classSession).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.ClassId = new SelectList(db.Classes, "Id", "ClassName", classSession.ClassId);
            return View(classSession);
        }

        // GET: ClassSessions/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            ClassSession classSession = db.ClassSessions.Find(id);
            if (classSession == null)
            {
                return HttpNotFound();
            }
            return View(classSession);
        }

        // POST: ClassSessions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            ClassSession classSession = db.ClassSessions.Find(id);
            db.ClassSessions.Remove(classSession);
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
