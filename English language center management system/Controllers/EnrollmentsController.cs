using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using English_language_center_management_system.Models;
using English_language_center_management_system.Models.ViewModels.Enrollments;

namespace English_language_center_management_system.Controllers
{
    public class EnrollmentsController : Controller
    {
        private EnglishCenterDbContext db = new EnglishCenterDbContext();

        #region 1. DANH SÁCH GHI DANH (INDEX)

        // GET: Enrollments
        public ActionResult Index(string searchTerm, int? classId, string status)
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập vào quản lý ghi danh!";
                return RedirectToAction("Login", "Auth");
            }

            var enrollmentsQuery = db.Enrollments
                .Include(e => e.Student)
                .Include(e => e.Class)
                .Include(e => e.Class.Course)
                .AsQueryable();

            // Tìm kiếm theo Tên học viên, Username, Email hoặc Tên lớp
            if (!string.IsNullOrEmpty(searchTerm))
            {
                enrollmentsQuery = enrollmentsQuery.Where(e =>
                    e.Student.FullName.Contains(searchTerm) ||
                    e.Student.Username.Contains(searchTerm) ||
                    e.Student.Email.Contains(searchTerm) ||
                    e.Class.ClassName.Contains(searchTerm));
            }

            // Lọc theo Lớp học
            if (classId.HasValue && classId.Value > 0)
            {
                enrollmentsQuery = enrollmentsQuery.Where(e => e.ClassId == classId.Value);
            }

            // Lọc theo Trạng thái (Pending, Approved, Active, Rejected, Cancelled)
            if (!string.IsNullOrEmpty(status))
            {
                enrollmentsQuery = enrollmentsQuery.Where(e => e.Status == status);
            }

            var enrollmentList = enrollmentsQuery.OrderByDescending(e => e.EnrollmentDate).ToList();

            // Lấy thông tin thanh toán cho từng lượt ghi danh
            var enrollmentItems = enrollmentList.Select(e =>
            {
                var payment = db.Payments.FirstOrDefault(p => p.EnrollmentId == e.Id);
                return new EnrollmentIndexItemViewModel
                {
                    Enrollment = e,
                    AmountPaid = payment != null ? payment.AmountPaid : 0,
                    PaymentStatus = payment != null ? payment.PaymentStatus : "Unpaid"
                };
            }).ToList();

            var viewModel = new EnrollmentIndexViewModel
            {
                Enrollments = enrollmentItems,
                SearchTerm = searchTerm,
                SelectedClassId = classId,
                SelectedStatus = status,
                Classes = db.Classes.OrderBy(c => c.ClassName).ToList()
            };

            return View(viewModel);
        }

        #endregion

        #region 2. XEM CHI TIẾT GHI DANH (DETAILS)

        // GET: Enrollments/Details/5
        public ActionResult Details(int? id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Enrollment enrollment = db.Enrollments
                .Include(e => e.Student)
                .Include(e => e.Class)
                .Include(e => e.Class.Course)
                .Include(e => e.Class.Teacher)
                .FirstOrDefault(e => e.Id == id);

            if (enrollment == null)
            {
                return HttpNotFound();
            }

            ViewBag.Payments = db.Payments.Where(p => p.EnrollmentId == id).ToList();

            return View(enrollment);
        }

        #endregion

        #region 3. THÊM GHI DANH MỚI (CREATE)

        // GET: Enrollments/Create
        public ActionResult Create()
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Auth");

            PopulateDropdowns();

            return View(new EnrollmentCreateViewModel());
        }

        // POST: Enrollments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(EnrollmentCreateViewModel model)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            if (ModelState.IsValid)
            {
                // Kiểm tra học viên đã đăng ký lớp này chưa
                bool alreadyEnrolled = db.Enrollments.Any(e =>
                    e.StudentId == model.StudentId &&
                    e.ClassId == model.ClassId &&
                    e.Status != "Cancelled" &&
                    e.Status != "Rejected");

                if (alreadyEnrolled)
                {
                    ModelState.AddModelError("", "Học viên này đã ghi danh vào lớp học này rồi!");
                }

                // Kiểm tra sĩ số lớp học
                var selectedClass = db.Classes.Find(model.ClassId);
                if (selectedClass != null)
                {
                    int currentEnrolled = db.Enrollments.Count(e => e.ClassId == model.ClassId && (e.Status == "Active" || e.Status == "Approved"));
                    if (currentEnrolled >= selectedClass.MaxCapacity)
                    {
                        ModelState.AddModelError("ClassId", $"Lớp học này đã đủ sĩ số tối đa ({selectedClass.MaxCapacity}/{selectedClass.MaxCapacity})!");
                    }
                }

                if (ModelState.IsValid)
                {
                    var enrollment = new Enrollment
                    {
                        StudentId = model.StudentId,
                        ClassId = model.ClassId,
                        Status = model.Status,
                        EnrollmentDate = model.EnrollmentDate
                    };

                    db.Enrollments.Add(enrollment);
                    db.SaveChanges();

                    TempData["SuccessMessage"] = "Ghi danh học viên vào lớp thành công!";
                    return RedirectToAction("Index");
                }
            }

            PopulateDropdowns(model.StudentId, model.ClassId);
            return View(model);
        }

        #endregion

        #region 4. CẬP NHẬT GHI DANH (EDIT)

        // GET: Enrollments/Edit/5
        public ActionResult Edit(int? id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Enrollment enrollment = db.Enrollments.Find(id);
            if (enrollment == null)
            {
                return HttpNotFound();
            }

            var viewModel = new EnrollmentEditViewModel
            {
                Id = enrollment.Id,
                StudentId = enrollment.StudentId,
                ClassId = enrollment.ClassId,
                Status = enrollment.Status,
                EnrollmentDate = enrollment.EnrollmentDate
            };

            PopulateDropdowns(enrollment.StudentId, enrollment.ClassId);
            return View(viewModel);
        }

        // POST: Enrollments/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(EnrollmentEditViewModel model)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            if (ModelState.IsValid)
            {
                Enrollment enrollment = db.Enrollments.Find(model.Id);
                if (enrollment == null)
                {
                    return HttpNotFound();
                }

                // Nếu đổi sang trạng thái Approved/Active, kiểm tra xem lớp có bị vượt quá sĩ số không
                if ((model.Status == "Approved" || model.Status == "Active") &&
                    (enrollment.Status != "Approved" && enrollment.Status != "Active"))
                {
                    var selectedClass = db.Classes.Find(model.ClassId);
                    if (selectedClass != null)
                    {
                        int currentEnrolled = db.Enrollments.Count(e => e.ClassId == model.ClassId && (e.Status == "Active" || e.Status == "Approved"));
                        if (currentEnrolled >= selectedClass.MaxCapacity)
                        {
                            ModelState.AddModelError("Status", $"Không thể duyệt! Lớp học đã đạt sĩ số tối đa ({selectedClass.MaxCapacity}).");
                        }
                    }
                }

                if (ModelState.IsValid)
                {
                    enrollment.StudentId = model.StudentId;
                    enrollment.ClassId = model.ClassId;
                    enrollment.Status = model.Status;
                    enrollment.EnrollmentDate = model.EnrollmentDate;

                    db.Entry(enrollment).State = EntityState.Modified;
                    db.SaveChanges();

                    TempData["SuccessMessage"] = "Cập nhật thông tin ghi danh thành công!";
                    return RedirectToAction("Index");
                }
            }

            PopulateDropdowns(model.StudentId, model.ClassId);
            return View(model);
        }

        #endregion

        #region 5. QUICK ACTIONS: DUYỆT / TỪ CHỐI ĐƠN

        // POST: Enrollments/Approve/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Approve(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            Enrollment enrollment = db.Enrollments.Find(id);
            if (enrollment != null)
            {
                var selectedClass = db.Classes.Find(enrollment.ClassId);
                int currentEnrolled = db.Enrollments.Count(e => e.ClassId == enrollment.ClassId && (e.Status == "Active" || e.Status == "Approved"));

                if (selectedClass != null && currentEnrolled >= selectedClass.MaxCapacity)
                {
                    TempData["ErrorMessage"] = "Không thể duyệt đơn! Lớp học đã đầy sĩ số.";
                }
                else
                {
                    enrollment.Status = "Approved";
                    db.SaveChanges();
                    TempData["SuccessMessage"] = "Đã phê duyệt đơn ghi danh thành công!";
                }
            }

            return RedirectToAction("Index");
        }

        // POST: Enrollments/Reject/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Reject(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            Enrollment enrollment = db.Enrollments.Find(id);
            if (enrollment != null)
            {
                enrollment.Status = "Rejected";
                db.SaveChanges();
                TempData["SuccessMessage"] = "Đã từ chối đơn ghi danh!";
            }

            return RedirectToAction("Index");
        }

        #endregion

        #region 6. XÓA GHI DANH (DELETE)

        // GET: Enrollments/Delete/5
        public ActionResult Delete(int? id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Enrollment enrollment = db.Enrollments
                .Include(e => e.Student)
                .Include(e => e.Class)
                .FirstOrDefault(e => e.Id == id);

            if (enrollment == null)
            {
                return HttpNotFound();
            }

            return View(enrollment);
        }

        // POST: Enrollments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            Enrollment enrollment = db.Enrollments.Find(id);
            if (enrollment != null)
            {
                // Kiểm tra xem đã phát sinh giao dịch thanh toán chưa
                bool hasPayments = db.Payments.Any(p => p.EnrollmentId == id);
                if (hasPayments)
                {
                    TempData["ErrorMessage"] = "Không thể xóa đơn ghi danh đã phát sinh lịch sử thanh toán học phí!";
                    return RedirectToAction("Index");
                }

                db.Enrollments.Remove(enrollment);
                db.SaveChanges();
                TempData["SuccessMessage"] = "Đã xóa bản ghi ghi danh!";
            }

            return RedirectToAction("Index");
        }

        #endregion

        #region 7. HELPER METHODS

        private void PopulateDropdowns(
    int? selectedStudentId = null,
    int? selectedClassId = null)
        {
            // Lấy danh sách học viên, luôn giữ lại học viên
            // hiện tại nếu đang sửa một bản ghi ghi danh.
            var students = db.Users
                .Where(u =>
                    (u.Role != null &&
                     u.Role.Name.ToLower().Contains("student")) ||
                    db.StudentProfiles.Any(
                        sp => sp.UserId == u.Id) ||
                    (selectedStudentId.HasValue &&
                     u.Id == selectedStudentId.Value))
                .Select(u => new
                {
                    Id = u.Id,
                    FullNameWithUsername =
                        u.FullName + " (" + u.Username + ")"
                })
                .OrderBy(u => u.FullNameWithUsername)
                .ToList();

            ViewBag.StudentId = new SelectList(
                students,
                "Id",
                "FullNameWithUsername",
                selectedStudentId
            );

            // Lấy danh sách lớp học và tên khóa học.
            var classes = db.Classes
                .Include(c => c.Course)
                .Select(c => new
                {
                    Id = c.Id,
                    ClassNameWithCourse =
                        c.ClassName + " - " +
                        (c.Course != null
                            ? c.Course.CourseName
                            : "") +
                        " [" + c.Status + "]"
                })
                .OrderBy(c => c.ClassNameWithCourse)
                .ToList();

            ViewBag.ClassId = new SelectList(
                classes,
                "Id",
                "ClassNameWithCourse",
                selectedClassId
            );
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