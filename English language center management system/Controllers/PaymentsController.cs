using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using English_language_center_management_system.Models;
using English_language_center_management_system.Models.ViewModels.Payments;

namespace English_language_center_management_system.Controllers
{
    public class PaymentsController : Controller
    {
        private EnglishCenterDbContext db = new EnglishCenterDbContext();

        #region 1. DANH SÁCH GIAO DỊCH / THU HỌC PHÍ (INDEX)

        // GET: Payments
        public ActionResult Index(string searchTerm, string status, string method)
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập vào quản lý học phí và tài chính!";
                return RedirectToAction("Login", "Auth");
            }

            var paymentsQuery = db.Payments
                .Include(p => p.Enrollment)
                .Include(p => p.Enrollment.Student)
                .Include(p => p.Enrollment.Class)
                .Include(p => p.Enrollment.Class.Course)
                .AsQueryable();

            // Tìm kiếm theo Mã biên lai, Tên học viên, Username hoặc Tên lớp
            if (!string.IsNullOrEmpty(searchTerm))
            {
                paymentsQuery = paymentsQuery.Where(p =>
                    p.ReceiptNumber.Contains(searchTerm) ||
                    p.Enrollment.Student.FullName.Contains(searchTerm) ||
                    p.Enrollment.Student.Username.Contains(searchTerm) ||
                    p.Enrollment.Class.ClassName.Contains(searchTerm));
            }

            // Lọc theo Trạng thái thanh toán (Paid, Partial, Pending)
            if (!string.IsNullOrEmpty(status))
            {
                paymentsQuery = paymentsQuery.Where(p => p.PaymentStatus == status);
            }

            // Lọc theo Phương thức thanh toán (Cash, Bank Transfer, Credit Card)
            if (!string.IsNullOrEmpty(method))
            {
                paymentsQuery = paymentsQuery.Where(p => p.PaymentMethod == method);
            }

            var paymentList = paymentsQuery.OrderByDescending(p => p.PaymentDate).ToList();

            // Ánh xạ sang PaymentIndexItemViewModel và tính dư nợ
            var paymentItems = paymentList.Select(p =>
            {
                decimal tuitionFee = p.Enrollment?.Class?.Course?.TuitionFee ?? 0;
                decimal remaining = tuitionFee - p.AmountPaid;
                if (remaining < 0) remaining = 0;

                return new PaymentIndexItemViewModel
                {
                    Payment = p,
                    StudentName = p.Enrollment?.Student?.FullName ?? "N/A",
                    ClassName = p.Enrollment?.Class?.ClassName ?? "N/A",
                    CourseName = p.Enrollment?.Class?.Course?.CourseName ?? "N/A",
                    TotalTuitionFee = tuitionFee,
                    RemainingBalance = remaining
                };
            }).ToList();

            var viewModel = new PaymentIndexViewModel
            {
                Payments = paymentItems,
                SearchTerm = searchTerm,
                SelectedStatus = status,
                SelectedMethod = method,
                TotalFilteredRevenue = paymentList.Sum(p => p.AmountPaid)
            };

            return View(viewModel);
        }

        #endregion

        #region 2. XEM CHI TIẾT GIAO DỊCH (DETAILS)

        // GET: Payments/Details/5
        public ActionResult Details(int? id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Payment payment = db.Payments
                .Include(p => p.Enrollment)
                .Include(p => p.Enrollment.Student)
                .Include(p => p.Enrollment.Class)
                .Include(p => p.Enrollment.Class.Course)
                .FirstOrDefault(p => p.Id == id);

            if (payment == null)
            {
                return HttpNotFound();
            }

            return View(payment);
        }

        #endregion

        #region 3. THÊM MỚI PHIẾU THU HỌC PHÍ (CREATE)

        // GET: Payments/Create
        public ActionResult Create(int? enrollmentId)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            PopulateDropdowns(enrollmentId);

            var model = new PaymentCreateViewModel
            {
                PaymentDate = DateTime.Now,
                ReceiptNumber = GenerateReceiptNumber(),
                PaymentMethod = "Cash",
                PaymentStatus = "Paid"
            };

            if (enrollmentId.HasValue)
            {
                model.EnrollmentId = enrollmentId.Value;
                var enrollment = db.Enrollments
                    .Include(e => e.Class)
                    .Include(e => e.Class.Course)
                    .FirstOrDefault(e => e.Id == enrollmentId.Value);

                if (enrollment != null && enrollment.Class != null && enrollment.Class.Course != null)
                {
                    model.AmountPaid = enrollment.Class.Course.TuitionFee;
                }
            }

            return View(model);
        }

        // POST: Payments/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(PaymentCreateViewModel model)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            if (ModelState.IsValid)
            {
                // Kiểm tra xem đơn ghi danh đã có hóa đơn chưa
                bool existingPayment = db.Payments.Any(p => p.EnrollmentId == model.EnrollmentId);
                if (existingPayment)
                {
                    ModelState.AddModelError("EnrollmentId", "Đơn ghi danh này đã phát sinh giao dịch thanh toán trước đó.");
                }

                if (ModelState.IsValid)
                {
                    var payment = new Payment
                    {
                        EnrollmentId = model.EnrollmentId,
                        AmountPaid = model.AmountPaid,
                        PaymentDate = model.PaymentDate,
                        ReceiptNumber = string.IsNullOrEmpty(model.ReceiptNumber) ? GenerateReceiptNumber() : model.ReceiptNumber,
                        PaymentMethod = model.PaymentMethod,
                        PaymentStatus = model.PaymentStatus
                    };

                    db.Payments.Add(payment);

                    // Cập nhật trạng thái của đơn Ghi danh thành Active nếu đã đóng tiền đầy đủ
                    var enrollment = db.Enrollments.Find(model.EnrollmentId);
                    if (enrollment != null && (model.PaymentStatus == "Paid" || model.PaymentStatus == "Completed"))
                    {
                        enrollment.Status = "Active";
                    }

                    db.SaveChanges();

                    TempData["SuccessMessage"] = "Ghi nhận thanh toán học phí thành công!";
                    return RedirectToAction("Index");
                }
            }

            PopulateDropdowns(model.EnrollmentId);
            return View(model);
        }

        #endregion

        #region 4. CẬP NHẬT GIAO DỊCH (EDIT)

        // GET: Payments/Edit/5
        public ActionResult Edit(int? id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Payment payment = db.Payments.Find(id);
            if (payment == null)
            {
                return HttpNotFound();
            }

            var viewModel = new PaymentEditViewModel
            {
                Id = payment.Id,
                EnrollmentId = payment.EnrollmentId,
                AmountPaid = payment.AmountPaid,
                PaymentDate = payment.PaymentDate,
                ReceiptNumber = payment.ReceiptNumber,
                PaymentMethod = payment.PaymentMethod,
                PaymentStatus = payment.PaymentStatus
            };

            PopulateDropdowns(payment.EnrollmentId);
            return View(viewModel);
        }

        // POST: Payments/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(PaymentEditViewModel model)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            if (ModelState.IsValid)
            {
                Payment payment = db.Payments.Find(model.Id);
                if (payment == null)
                {
                    return HttpNotFound();
                }

                payment.EnrollmentId = model.EnrollmentId;
                payment.AmountPaid = model.AmountPaid;
                payment.PaymentDate = model.PaymentDate;
                payment.ReceiptNumber = model.ReceiptNumber;
                payment.PaymentMethod = model.PaymentMethod;
                payment.PaymentStatus = model.PaymentStatus;

                db.Entry(payment).State = EntityState.Modified;
                db.SaveChanges();

                TempData["SuccessMessage"] = "Cập nhật thông tin phiếu thu thành công!";
                return RedirectToAction("Index");
            }

            PopulateDropdowns(model.EnrollmentId);
            return View(model);
        }

        #endregion

        #region 5. IN BIÊN LAI THU TIỀN (RECEIPT)

        // GET: Payments/Receipt/5
        public ActionResult Receipt(int? id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Payment payment = db.Payments
                .Include(p => p.Enrollment)
                .Include(p => p.Enrollment.Student)
                .Include(p => p.Enrollment.Class)
                .Include(p => p.Enrollment.Class.Course)
                .FirstOrDefault(p => p.Id == id);

            if (payment == null)
            {
                return HttpNotFound();
            }

            return View(payment);
        }

        #endregion

        #region 6. XÓA GIAO DỊCH (DELETE)

        // GET: Payments/Delete/5
        public ActionResult Delete(int? id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Payment payment = db.Payments
                .Include(p => p.Enrollment)
                .Include(p => p.Enrollment.Student)
                .Include(p => p.Enrollment.Class)
                .FirstOrDefault(p => p.Id == id);

            if (payment == null)
            {
                return HttpNotFound();
            }

            return View(payment);
        }

        // POST: Payments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            Payment payment = db.Payments.Find(id);
            if (payment != null)
            {
                db.Payments.Remove(payment);
                db.SaveChanges();
                TempData["SuccessMessage"] = "Đã xóa bản ghi giao dịch!";
            }

            return RedirectToAction("Index");
        }

        #endregion

        #region 7. HELPER METHODS


        private void PopulateDropdowns(int? selectedEnrollmentId = null)
        {
            var enrollments = db.Enrollments
                .Include(e => e.Student)
                .Include(e => e.Class)
                .Include(e => e.Class.Course)
                .Where(e =>
                    (e.Status != "Cancelled" && e.Status != "Rejected") ||
                    (selectedEnrollmentId.HasValue &&
                     e.Id == selectedEnrollmentId.Value))
                .ToList()
                .Select(e => new
                {
                    Id = e.Id,
                    DisplayText =
                        (e.Student != null
                            ? e.Student.FullName + " (" + e.Student.Username + ")"
                            : "Học viên chưa xác định")
                        + " - Lớp: "
                        + (e.Class != null
                            ? e.Class.ClassName
                            : "Lớp chưa xác định")
                        + " [Học phí: "
                        + (e.Class != null && e.Class.Course != null
                            ? e.Class.Course.TuitionFee.ToString("N0")
                            : "0")
                        + " VNĐ]"
                })
                .OrderBy(e => e.DisplayText)
                .ToList();

            ViewBag.EnrollmentId = new SelectList(
                enrollments,
                "Id",
                "DisplayText",
                selectedEnrollmentId
            );
        }


        private string GenerateReceiptNumber()
        {
            string datePart = DateTime.Now.ToString("yyyyMMdd");
            int todayCount = db.Payments.Count(p => DbFunctions.TruncateTime(p.PaymentDate) == DateTime.Today) + 1;
            return $"REC-{datePart}-{todayCount:D4}";
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