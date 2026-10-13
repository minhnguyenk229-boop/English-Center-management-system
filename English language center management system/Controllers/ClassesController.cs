using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using English_language_center_management_system.Models;
using English_language_center_management_system.Models.ViewModels.Classes;

namespace English_language_center_management_system.Controllers
{
    public class ClassesController : Controller
    {
        private EnglishCenterDbContext db = new EnglishCenterDbContext();

        #region 1. DANH SÁCH LỚP HỌC (INDEX)

        // GET: Classes
        public ActionResult Index(string searchTerm, int? courseId, string status)
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập vào quản lý lớp học!";
                return RedirectToAction("Login", "Auth");
            }

            var classesQuery = db.Classes
                .Include(c => c.Course)
                .Include(c => c.Teacher)
                .AsQueryable();

            // Tìm kiếm theo Tên lớp hoặc Phòng học
            if (!string.IsNullOrEmpty(searchTerm))
            {
                classesQuery = classesQuery.Where(c =>
                    c.ClassName.Contains(searchTerm) ||
                    c.Room.Contains(searchTerm));
            }

            // Lọc theo Khóa học
            if (courseId.HasValue && courseId.Value > 0)
            {
                classesQuery = classesQuery.Where(c => c.CourseId == courseId.Value);
            }

            // Lọc theo Trạng thái (Open, Studying, Completed, Closed)
            if (!string.IsNullOrEmpty(status))
            {
                classesQuery = classesQuery.Where(c => c.Status == status);
            }

            var classList = classesQuery.OrderByDescending(c => c.Id).ToList();

            // Tính sĩ số học viên đã ghi danh thành công cho từng lớp
            var classIndexItems = classList.Select(c => new ClassIndexItemViewModel
            {
                Class = c,
                EnrolledCount = db.Enrollments.Count(e => e.ClassId == c.Id && (e.Status == "Active" || e.Status == "Approved" || e.Status == "Studying"))
            }).ToList();

            var viewModel = new ClassIndexViewModel
            {
                Classes = classIndexItems,
                SearchTerm = searchTerm,
                SelectedCourseId = courseId,
                SelectedStatus = status,
                Courses = db.Courses.OrderBy(co => co.CourseName).ToList()
            };

            return View(viewModel);
        }

        #endregion

        #region 2. XEM CHI TIẾT LỚP HỌC (DETAILS)

        // GET: Classes/Details/5
        public ActionResult Details(int? id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Class @class = db.Classes
                .Include(c => c.Course)
                .Include(c => c.Teacher)
                .FirstOrDefault(c => c.Id == id);

            if (@class == null)
            {
                return HttpNotFound();
            }

            // Lấy danh sách học viên đang ghi danh lớp này
            ViewBag.Enrollments = db.Enrollments
                .Include(e => e.Student)
                .Where(e => e.ClassId == id)
                .ToList();

            return View(@class);
        }

        #endregion

        #region 3. MỞ LỚP HỌC MỚI (CREATE)

        // GET: Classes/Create
        public ActionResult Create()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            PopulateDropdowns();
            return View();
        }

        // POST: Classes/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(ClassCreateViewModel model)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            if (ModelState.IsValid)
            {
                var @class = new Class
                {
                    CourseId = model.CourseId,
                    TeacherId = model.TeacherId,
                    ClassName = model.ClassName,
                    Room = model.Room,
                    MaxCapacity = model.MaxCapacity,
                    ScheduleDescription = model.ScheduleDescription,
                    Status = model.Status
                };

                db.Classes.Add(@class);
                db.SaveChanges();

                TempData["SuccessMessage"] = "Mở lớp học mới thành công!";
                return RedirectToAction("Index");
            }

            PopulateDropdowns(model.CourseId, model.TeacherId);
            return View(model);
        }

        #endregion

        #region 4. CẬP NHẬT LỚP HỌC (EDIT)

        // GET: Classes/Edit/5
        public ActionResult Edit(int? id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Class @class = db.Classes.Find(id);
            if (@class == null)
            {
                return HttpNotFound();
            }

            var viewModel = new ClassEditViewModel
            {
                Id = @class.Id,
                CourseId = @class.CourseId,
                TeacherId = @class.TeacherId,
                ClassName = @class.ClassName,
                Room = @class.Room,
                MaxCapacity = @class.MaxCapacity,
                ScheduleDescription = @class.ScheduleDescription,
                Status = @class.Status
            };

            PopulateDropdowns(@class.CourseId, @class.TeacherId);
            return View(viewModel);
        }

        // POST: Classes/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(ClassEditViewModel model)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            if (ModelState.IsValid)
            {
                Class @class = db.Classes.Find(model.Id);
                if (@class == null)
                {
                    return HttpNotFound();
                }

                @class.CourseId = model.CourseId;
                @class.TeacherId = model.TeacherId;
                @class.ClassName = model.ClassName;
                @class.Room = model.Room;
                @class.MaxCapacity = model.MaxCapacity;
                @class.ScheduleDescription = model.ScheduleDescription;
                @class.Status = model.Status;

                db.Entry(@class).State = EntityState.Modified;
                db.SaveChanges();

                TempData["SuccessMessage"] = "Cập nhật thông tin lớp học thành công!";
                return RedirectToAction("Index");
            }

            PopulateDropdowns(model.CourseId, model.TeacherId);
            return View(model);
        }

        #endregion

        #region 5. XÓA LỚP HỌC (DELETE)

        // GET: Classes/Delete/5
        public ActionResult Delete(int? id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Class @class = db.Classes
                .Include(c => c.Course)
                .Include(c => c.Teacher)
                .FirstOrDefault(c => c.Id == id);

            if (@class == null)
            {
                return HttpNotFound();
            }

            return View(@class);
        }

        // POST: Classes/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            Class @class = db.Classes.Find(id);
            if (@class != null)
            {
                // Kiểm tra xem đã có học viên đăng ký lớp này chưa
                bool hasEnrollments = db.Enrollments.Any(e => e.ClassId == id);
                if (hasEnrollments)
                {
                    TempData["ErrorMessage"] = "Không thể xóa lớp học đã có học viên ghi danh!";
                    return RedirectToAction("Index");
                }

                db.Classes.Remove(@class);
                db.SaveChanges();
                TempData["SuccessMessage"] = "Xóa lớp học thành công!";
            }

            return RedirectToAction("Index");
        }

        #endregion

        #region 6. HELPER METHODS

        private void PopulateDropdowns(int? selectedCourseId = null, int? selectedTeacherId = null)
        {
            // Danh sách Khóa học
            ViewBag.CourseId = new SelectList(db.Courses.OrderBy(c => c.CourseName), "Id", "CourseName", selectedCourseId);

            // Lọc danh sách chỉ lấy người dùng là Giáo viên
            var teachers = db.Users
                .Where(u => u.Role.Name.ToLower().Contains("teacher") || db.TeacherProfiles.Any(tp => tp.UserId == u.Id))
                .Select(u => new
                {
                    Id = u.Id,
                    FullNameWithUsername = u.FullName + " (" + u.Username + ")"
                })
                .OrderBy(t => t.FullNameWithUsername)
                .ToList();

            ViewBag.TeacherId = new SelectList(teachers, "Id", "FullNameWithUsername", selectedTeacherId);
        }

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