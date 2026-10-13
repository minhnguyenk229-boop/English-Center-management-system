using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using English_language_center_management_system.Models;
using English_language_center_management_system.Models.ViewModels.Courses;

namespace English_language_center_management_system.Controllers
{
    public class CoursesController : Controller
    {
        private EnglishCenterDbContext db = new EnglishCenterDbContext();

        #region 1. PUBLIC ACTIONS (Dành cho Học viên / Khách xem Khóa học)

        // GET: Courses/Index (Trang Catalog danh sách khóa học Public)
        public ActionResult Index(string searchTerm, string level)
        {
            var coursesQuery = db.Courses.AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                coursesQuery = coursesQuery.Where(c => c.CourseName.Contains(searchTerm));
            }

            if (!string.IsNullOrEmpty(level))
            {
                coursesQuery = coursesQuery.Where(c => c.Level == level);
            }

            var viewModel = new CourseCatalogViewModel
            {
                Courses = coursesQuery.OrderBy(c => c.CourseName).ToList(),
                SearchTerm = searchTerm,
                SelectedLevel = level
            };

            return View(viewModel);
        }

        // GET: Courses/Details/5 (Trang chi tiết khóa học)
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Course course = db.Courses.Find(id);
            if (course == null)
            {
                return HttpNotFound();
            }

            var availableClasses = db.Classes
                .Where(c => c.CourseId == id && c.Status == "Open")
                .Include(c => c.Teacher)
                .OrderBy(c => c.ClassName)
                .ToList();

            var viewModel = new CourseDetailViewModel
            {
                Course = course,
                AvailableClasses = availableClasses
            };

            return View(viewModel);
        }

        #endregion

        #region 2. ADMIN ACTIONS (Dành cho Quản trị viên)

        // GET: Courses/AdminIndex (Trang quản lý khóa học cho Admin)
        public ActionResult AdminIndex(string searchTerm, string level)
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập vào trang Quản lý Khóa học!";
                return RedirectToAction("Login", "Auth");
            }

            var coursesQuery = db.Courses.AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                coursesQuery = coursesQuery.Where(c => c.CourseName.Contains(searchTerm) || c.Description.Contains(searchTerm));
            }

            if (!string.IsNullOrEmpty(level))
            {
                coursesQuery = coursesQuery.Where(c => c.Level == level);
            }

            var courseList = coursesQuery.OrderByDescending(c => c.Id).ToList();

            var adminItems = courseList.Select(c => new CourseAdminItemViewModel
            {
                Course = c,
                TotalClassesCount = db.Classes.Count(cls => cls.CourseId == c.Id),
                ActiveClassesCount = db.Classes.Count(cls => cls.CourseId == c.Id && (cls.Status == "Open" || cls.Status == "Studying")),
                TotalStudentsCount = db.Enrollments.Count(e => e.Class.CourseId == c.Id && (e.Status == "Active" || e.Status == "Approved"))
            }).ToList();

            var viewModel = new CourseAdminIndexViewModel
            {
                Courses = adminItems,
                SearchTerm = searchTerm,
                SelectedLevel = level
            };

            return View(viewModel);
        }

        // GET: Courses/Create
        public ActionResult Create()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            return View();
        }

        // POST: Courses/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(CourseCreateViewModel model)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            if (ModelState.IsValid)
            {
                // Kiểm tra tên khóa học trùng lặp
                bool isDuplicate = db.Courses.Any(c => c.CourseName.Equals(model.CourseName, StringComparison.OrdinalIgnoreCase));
                if (isDuplicate)
                {
                    ModelState.AddModelError("CourseName", "Tên khóa học này đã tồn tại trong hệ thống.");
                }

                if (ModelState.IsValid)
                {
                    var course = new Course
                    {
                        CourseName = model.CourseName,
                        Level = model.Level,
                        Description = model.Description,
                        Duration = model.Duration,
                        TuitionFee = model.TuitionFee,
                        Syllabus = model.Syllabus
                    };

                    db.Courses.Add(course);
                    db.SaveChanges();

                    TempData["SuccessMessage"] = "Thêm khóa học mới thành công!";
                    return RedirectToAction("AdminIndex");
                }
            }

            return View(model);
        }

        // GET: Courses/Edit/5
        public ActionResult Edit(int? id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Course course = db.Courses.Find(id);
            if (course == null)
            {
                return HttpNotFound();
            }

            var viewModel = new CourseEditViewModel
            {
                Id = course.Id,
                CourseName = course.CourseName,
                Level = course.Level,
                Description = course.Description,
                Duration = course.Duration,
                TuitionFee = course.TuitionFee,
                Syllabus = course.Syllabus
            };

            return View(viewModel);
        }

        // POST: Courses/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(CourseEditViewModel model)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            if (ModelState.IsValid)
            {
                Course course = db.Courses.Find(model.Id);
                if (course == null)
                {
                    return HttpNotFound();
                }

                // Kiểm tra trùng tên khóa học ngoại trừ khóa hiện tại
                bool isDuplicate = db.Courses.Any(c => c.Id != model.Id && c.CourseName.Equals(model.CourseName, StringComparison.OrdinalIgnoreCase));
                if (isDuplicate)
                {
                    ModelState.AddModelError("CourseName", "Tên khóa học này đã trùng với một khóa học khác.");
                }

                if (ModelState.IsValid)
                {
                    course.CourseName = model.CourseName;
                    course.Level = model.Level;
                    course.Description = model.Description;
                    course.Duration = model.Duration;
                    course.TuitionFee = model.TuitionFee;
                    course.Syllabus = model.Syllabus;

                    db.Entry(course).State = EntityState.Modified;
                    db.SaveChanges();

                    TempData["SuccessMessage"] = "Cập nhật thông tin khóa học thành công!";
                    return RedirectToAction("AdminIndex");
                }
            }

            return View(model);
        }

        // GET: Courses/Delete/5
        public ActionResult Delete(int? id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Course course = db.Courses.Find(id);
            if (course == null)
            {
                return HttpNotFound();
            }

            ViewBag.RelatedClassesCount = db.Classes.Count(c => c.CourseId == id);
            return View(course);
        }

        // POST: Courses/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            Course course = db.Courses.Find(id);
            if (course != null)
            {
                // Ràng buộc an toàn: Không cho xóa nếu đã có lớp học thuộc khóa này
                bool hasClasses = db.Classes.Any(c => c.CourseId == id);
                if (hasClasses)
                {
                    TempData["ErrorMessage"] = "Không thể xóa khóa học đã có danh sách lớp học liên kết!";
                    return RedirectToAction("AdminIndex");
                }

                db.Courses.Remove(course);
                db.SaveChanges();
                TempData["SuccessMessage"] = "Đã xóa khóa học thành công!";
            }

            return RedirectToAction("AdminIndex");
        }
        // GET: Courses/AdminDetails/5
        public ActionResult AdminDetails(int? id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Course course = db.Courses.Find(id);
            if (course == null)
            {
                return HttpNotFound();
            }

            // Lấy tất cả lớp học thuộc khóa học này (bao gồm giáo viên phụ trách)
            var classes = db.Classes
                .Include(c => c.Teacher)
                .Where(c => c.CourseId == id)
                .OrderByDescending(c => c.Id)
                .ToList();

            // Mapping sang danh sách lớp kèm sĩ số và doanh thu
            var classItems = classes.Select(c => new CourseAdminClassItemViewModel
            {
                Class = c,
                EnrolledCount = db.Enrollments.Count(e => e.ClassId == c.Id && (e.Status == "Active" || e.Status == "Approved")),
                ClassRevenue = db.Payments.Where(p => p.Enrollment.ClassId == c.Id && (p.PaymentStatus == "Paid" || p.PaymentStatus == "Completed")).Sum(p => (decimal?)p.AmountPaid) ?? 0
            }).ToList();

            var viewModel = new CourseAdminDetailViewModel
            {
                Course = course,
                TotalClassesCount = classes.Count,
                ActiveClassesCount = classes.Count(c => c.Status == "Open" || c.Status == "Studying"),
                TotalStudentsCount = db.Enrollments.Count(e => e.Class.CourseId == id && (e.Status == "Active" || e.Status == "Approved")),
                TotalRevenueGenerated = classItems.Sum(ci => ci.ClassRevenue),
                Classes = classItems
            };

            return View(viewModel);
        }

        #endregion

        #region 3. HELPER METHODS

        private bool IsAdmin()
        {
            if (Session["UserId"] == null) return false;

            string roleName = Session["RoleName"]?.ToString();
            if (string.IsNullOrEmpty(roleName)) return false;

            return roleName.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                   roleName.Equals("Administrator", StringComparison.OrdinalIgnoreCase) ||
                   roleName.Equals("Quản trị viên", StringComparison.OrdinalIgnoreCase);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        #endregion
    }
}