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
    public class TeacherFeedbacksController : Controller
    {
        private EnglishCenterDbContext db = new EnglishCenterDbContext();

        // GET: TeacherFeedbacks
        public ActionResult Index()
        {
            var teacherFeedbacks = db.TeacherFeedbacks.Include(t => t.Enrollment).Include(t => t.Teacher);
            return View(teacherFeedbacks.ToList());
        }

        // GET: TeacherFeedbacks/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            TeacherFeedback teacherFeedback = db.TeacherFeedbacks.Find(id);
            if (teacherFeedback == null)
            {
                return HttpNotFound();
            }
            return View(teacherFeedback);
        }

        // GET: TeacherFeedbacks/Create
        public ActionResult Create()
        {
            ViewBag.EnrollmentId = new SelectList(db.Enrollments, "Id", "Status");
            ViewBag.TeacherId = new SelectList(db.Users, "Id", "Username");
            return View();
        }

        // POST: TeacherFeedbacks/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,EnrollmentId,TeacherId,Comments,CreatedAt")] TeacherFeedback teacherFeedback)
        {
            if (ModelState.IsValid)
            {
                db.TeacherFeedbacks.Add(teacherFeedback);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.EnrollmentId = new SelectList(db.Enrollments, "Id", "Status", teacherFeedback.EnrollmentId);
            ViewBag.TeacherId = new SelectList(db.Users, "Id", "Username", teacherFeedback.TeacherId);
            return View(teacherFeedback);
        }

        // GET: TeacherFeedbacks/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            TeacherFeedback teacherFeedback = db.TeacherFeedbacks.Find(id);
            if (teacherFeedback == null)
            {
                return HttpNotFound();
            }
            ViewBag.EnrollmentId = new SelectList(db.Enrollments, "Id", "Status", teacherFeedback.EnrollmentId);
            ViewBag.TeacherId = new SelectList(db.Users, "Id", "Username", teacherFeedback.TeacherId);
            return View(teacherFeedback);
        }

        // POST: TeacherFeedbacks/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,EnrollmentId,TeacherId,Comments,CreatedAt")] TeacherFeedback teacherFeedback)
        {
            if (ModelState.IsValid)
            {
                db.Entry(teacherFeedback).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.EnrollmentId = new SelectList(db.Enrollments, "Id", "Status", teacherFeedback.EnrollmentId);
            ViewBag.TeacherId = new SelectList(db.Users, "Id", "Username", teacherFeedback.TeacherId);
            return View(teacherFeedback);
        }

        // GET: TeacherFeedbacks/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            TeacherFeedback teacherFeedback = db.TeacherFeedbacks.Find(id);
            if (teacherFeedback == null)
            {
                return HttpNotFound();
            }
            return View(teacherFeedback);
        }

        // POST: TeacherFeedbacks/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            TeacherFeedback teacherFeedback = db.TeacherFeedbacks.Find(id);
            db.TeacherFeedbacks.Remove(teacherFeedback);
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
