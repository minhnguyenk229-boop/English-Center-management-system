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
    public class AssessmentScoresController : Controller
    {
        private EnglishCenterDbContext db = new EnglishCenterDbContext();

        // GET: AssessmentScores
        public ActionResult Index()
        {
            var assessmentScores = db.AssessmentScores.Include(a => a.Enrollment);
            return View(assessmentScores.ToList());
        }

        // GET: AssessmentScores/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            AssessmentScore assessmentScore = db.AssessmentScores.Find(id);
            if (assessmentScore == null)
            {
                return HttpNotFound();
            }
            return View(assessmentScore);
        }

        // GET: AssessmentScores/Create
        public ActionResult Create()
        {
            ViewBag.EnrollmentId = new SelectList(db.Enrollments, "Id", "Status");
            return View();
        }

        // POST: AssessmentScores/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "EnrollmentId,MidtermScore,FinalScore,TotalScore,LearningOutcome")] AssessmentScore assessmentScore)
        {
            if (ModelState.IsValid)
            {
                db.AssessmentScores.Add(assessmentScore);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.EnrollmentId = new SelectList(db.Enrollments, "Id", "Status", assessmentScore.EnrollmentId);
            return View(assessmentScore);
        }

        // GET: AssessmentScores/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            AssessmentScore assessmentScore = db.AssessmentScores.Find(id);
            if (assessmentScore == null)
            {
                return HttpNotFound();
            }
            ViewBag.EnrollmentId = new SelectList(db.Enrollments, "Id", "Status", assessmentScore.EnrollmentId);
            return View(assessmentScore);
        }

        // POST: AssessmentScores/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "EnrollmentId,MidtermScore,FinalScore,TotalScore,LearningOutcome")] AssessmentScore assessmentScore)
        {
            if (ModelState.IsValid)
            {
                db.Entry(assessmentScore).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.EnrollmentId = new SelectList(db.Enrollments, "Id", "Status", assessmentScore.EnrollmentId);
            return View(assessmentScore);
        }

        // GET: AssessmentScores/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            AssessmentScore assessmentScore = db.AssessmentScores.Find(id);
            if (assessmentScore == null)
            {
                return HttpNotFound();
            }
            return View(assessmentScore);
        }

        // POST: AssessmentScores/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            AssessmentScore assessmentScore = db.AssessmentScores.Find(id);
            db.AssessmentScores.Remove(assessmentScore);
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
