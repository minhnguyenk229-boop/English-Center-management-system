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
    public class TeacherProfilesController : Controller
    {
        private EnglishCenterDbContext db = new EnglishCenterDbContext();

        // GET: TeacherProfiles
        public ActionResult Index()
        {
            var teacherProfiles = db.TeacherProfiles.Include(t => t.User);
            return View(teacherProfiles.ToList());
        }

        // GET: TeacherProfiles/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            TeacherProfile teacherProfile = db.TeacherProfiles.Find(id);
            if (teacherProfile == null)
            {
                return HttpNotFound();
            }
            return View(teacherProfile);
        }

        // GET: TeacherProfiles/Create
        public ActionResult Create()
        {
            ViewBag.UserId = new SelectList(db.Users, "Id", "Username");
            return View();
        }

        // POST: TeacherProfiles/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "UserId,Specialization,Bio")] TeacherProfile teacherProfile)
        {
            if (ModelState.IsValid)
            {
                db.TeacherProfiles.Add(teacherProfile);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.UserId = new SelectList(db.Users, "Id", "Username", teacherProfile.UserId);
            return View(teacherProfile);
        }

        // GET: TeacherProfiles/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            TeacherProfile teacherProfile = db.TeacherProfiles.Find(id);
            if (teacherProfile == null)
            {
                return HttpNotFound();
            }
            ViewBag.UserId = new SelectList(db.Users, "Id", "Username", teacherProfile.UserId);
            return View(teacherProfile);
        }

        // POST: TeacherProfiles/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "UserId,Specialization,Bio")] TeacherProfile teacherProfile)
        {
            if (ModelState.IsValid)
            {
                db.Entry(teacherProfile).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.UserId = new SelectList(db.Users, "Id", "Username", teacherProfile.UserId);
            return View(teacherProfile);
        }

        // GET: TeacherProfiles/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            TeacherProfile teacherProfile = db.TeacherProfiles.Find(id);
            if (teacherProfile == null)
            {
                return HttpNotFound();
            }
            return View(teacherProfile);
        }

        // POST: TeacherProfiles/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            TeacherProfile teacherProfile = db.TeacherProfiles.Find(id);
            db.TeacherProfiles.Remove(teacherProfile);
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
