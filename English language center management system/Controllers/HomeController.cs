using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using English_language_center_management_system.Models;
using English_language_center_management_system.Models.ViewModels.Home;

namespace English_language_center_management_system.Controllers
{
    public class HomeController : Controller
    {
        private EnglishCenterDbContext db = new EnglishCenterDbContext();

        #region 1. TRANG CHỦ PUBLIC & ĐIỀU HƯỚNG CHUNG

        // GET: /Home/Index
        [HttpGet]
        public ActionResult Index()
        {
            var viewModel = new HomeViewModel
            {
                FeaturedCourses = db.Courses.Take(6).ToList(),
                TotalCoursesCount = db.Courses.Count(),
                TotalStudentsCount = db.StudentProfiles.Count(),
                TotalTeachersCount = db.TeacherProfiles.Count(),
                TotalActiveClassesCount = db.Classes.Count(c => c.Status == "Open" || c.Status == "Studying")
            };

            return View(viewModel);
        }

        #endregion

        #region 2. ADMIN EXECUTIVE DASHBOARD (PHẦN D)

        // GET: /Home/AdminDashboard
        [HttpGet]
        public ActionResult AdminDashboard()
        {
            if (!IsAuthorizedRole(
                "Admin",
                "Administrator",
                "Quản trị viên"))
            {
                TempData["ErrorMessage"] =
                    "Bạn không có quyền truy cập trang quản trị!";

                return RedirectToAction("Login", "Auth");
            }

            // 1. Tổng doanh thu các khoản đã thanh toán.
            decimal totalRevenue = db.Payments
                .Where(p =>
                    p.PaymentStatus == "Paid" ||
                    p.PaymentStatus == "Completed")
                .Select(p => (decimal?)p.AmountPaid)
                .Sum() ?? 0m;

            // 2. Các chỉ số tổng quan.
            int totalStudents = db.StudentProfiles.Count();

            int totalTeachers = db.TeacherProfiles.Count();

            int totalUsers = db.Users.Count();

            int totalCourses = db.Courses.Count();

            int activeClasses = db.Classes.Count(c =>
                c.Status == "Open" ||
                c.Status == "Studying");

            int pendingEnrollments = db.Enrollments.Count(e =>
                e.Status == "Pending");

            string[] activeEnrollmentStatuses =
            {
        "Active",
        "Approved",
        "Confirmed",
        "Studying"
    };

            int activeStudents = db.Enrollments
                .Where(e => activeEnrollmentStatuses.Contains(e.Status))
                .Select(e => e.StudentId)
                .Distinct()
                .Count();

            // 3. Dữ liệu doanh thu 6 tháng gần nhất.
            DateTime currentMonth = new DateTime(
                DateTime.Today.Year,
                DateTime.Today.Month,
                1);

            DateTime chartStart = currentMonth.AddMonths(-5);

            var paymentRows = db.Payments
                .Where(p =>
                    (p.PaymentStatus == "Paid" ||
                     p.PaymentStatus == "Completed") &&
                    p.PaymentDate >= chartStart)
                .Select(p => new
                {
                    p.PaymentDate,
                    p.AmountPaid
                })
                .ToList();

            var monthlyRevenue = new List<MonthlyRevenuePointViewModel>();

            for (int i = 0; i < 6; i++)
            {
                DateTime monthStart = chartStart.AddMonths(i);
                DateTime monthEnd = monthStart.AddMonths(1);

                decimal revenue = paymentRows
                    .Where(p =>
                        p.PaymentDate >= monthStart &&
                        p.PaymentDate < monthEnd)
                    .Sum(p => p.AmountPaid);

                monthlyRevenue.Add(new MonthlyRevenuePointViewModel
                {
                    MonthLabel = "T" + monthStart.ToString("MM"),
                    Revenue = revenue
                });
            }

            // 4. Tìm các lớp đã sử dụng ít nhất 80% sức chứa.
            var classesToCheck = db.Classes
                .Include(c => c.Course)
                .Include(c => c.Enrollments)
                .Where(c =>
                    c.Status == "Open" ||
                    c.Status == "Studying" ||
                    c.Status == "Full")
                .ToList();

            var classesNearCapacity = classesToCheck
                .Select(c =>
                {
                    int enrolledCount = c.Enrollments == null
                        ? 0
                        : c.Enrollments.Count(e =>
                            activeEnrollmentStatuses.Contains(e.Status));

                    decimal occupancyPercent = c.MaxCapacity > 0
                        ? Math.Round(
                            enrolledCount * 100m / c.MaxCapacity,
                            1)
                        : 0m;

                    return new AdminClassCapacityViewModel
                    {
                        ClassId = c.Id,
                        ClassName = c.ClassName,

                        CourseName = c.Course != null
                            ? c.Course.CourseName
                            : "Chưa cập nhật",

                        Room = c.Room,
                        EnrolledCount = enrolledCount,
                        MaxCapacity = c.MaxCapacity,
                        OccupancyPercent = occupancyPercent,
                        Status = c.Status
                    };
                })
                .Where(c =>
                    c.MaxCapacity > 0 &&
                    c.OccupancyPercent >= 80)
                .OrderByDescending(c => c.OccupancyPercent)
                .Take(6)
                .ToList();

            // 5. Năm đơn ghi danh mới nhất.
            var recentEnrollments = db.Enrollments
                .Include(e => e.Student)
                .Include(e => e.Class)
                .Include(e => e.Class.Course)
                .OrderByDescending(e => e.EnrollmentDate)
                .Take(5)
                .ToList();

            // 6. Năm giao dịch gần nhất.
            var recentPayments = db.Payments
                .Include(p => p.Enrollment)
                .Include(p => p.Enrollment.Student)
                .Include(p => p.Enrollment.Class)
                .OrderByDescending(p => p.PaymentDate)
                .Take(5)
                .ToList();

            var model = new AdminDashboardViewModel
            {
                TotalRevenue = totalRevenue,
                TotalStudents = totalStudents,
                ActiveStudents = activeStudents,
                TotalTeachers = totalTeachers,
                TotalUsers = totalUsers,
                TotalCourses = totalCourses,
                ActiveClasses = activeClasses,
                PendingEnrollments = pendingEnrollments,
                MonthlyRevenue = monthlyRevenue,
                ClassesNearCapacity = classesNearCapacity,
                RecentEnrollments = recentEnrollments,
                RecentPayments = recentPayments
            };

            return View(model);
        }


        #endregion

        #region 3. TEACHER DASHBOARD (PHẦN B)

        // GET: /Home/TeacherDashboard

        [HttpGet]
        public ActionResult TeacherDashboard()
        {
            if (!IsAuthorizedRole("Teacher", "Giáo viên"))
            {
                TempData["ErrorMessage"] =
                    "Vui lòng đăng nhập bằng tài khoản Giáo viên!";

                return RedirectToAction("Login", "Auth");
            }

            int userId = Convert.ToInt32(Session["UserId"]);

            var user = db.Users.Find(userId);

            if (user == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var teacherProfile = db.TeacherProfiles
                .FirstOrDefault(tp => tp.UserId == userId);

            // 1. Các lớp được phân công cho giáo viên đang đăng nhập.
            var assignedClasses = db.Classes
                .Include(c => c.Course)
                .Where(c => c.TeacherId == userId)
                .OrderBy(c => c.ClassName)
                .ToList();

            var assignedClassIds = assignedClasses
                .Select(c => c.Id)
                .ToList();

            // Các trạng thái ghi danh có thể xuất hiện trong dự án.
            // Không tính học viên đã rút khỏi lớp hoặc đã hoàn thành.
            int totalStudents = db.Enrollments
                .Where(e =>
                    assignedClassIds.Contains(e.ClassId) &&
                    (
                        e.Status == "Confirmed" ||
                        e.Status == "Studying" ||
                        e.Status == "Active" ||
                        e.Status == "Approved"
                    ))
                .Select(e => e.StudentId)
                .Distinct()
                .Count();

            // 2. Các buổi dạy sắp diễn ra.
            DateTime today = DateTime.Today;

            var upcomingSessions = db.ClassSessions
                .Include(cs => cs.Class)
                .Include(cs => cs.Class.Course)
                .Where(cs =>
                    assignedClassIds.Contains(cs.ClassId) &&
                    cs.SessionDate >= today)
                .OrderBy(cs => cs.SessionDate)
                .ThenBy(cs => cs.StartTime)
                .Take(5)
                .ToList();

            // 3. Tổng số bài tập thuộc các lớp của giáo viên.
            int totalAssignments = db.Assignments
                .Count(a => assignedClassIds.Contains(a.ClassId));

            // 4. Số bài nộp chưa được chấm điểm.
            int pendingSubmissionsCount = db.Submissions
                .Count(s =>
                    assignedClassIds.Contains(s.Assignment.ClassId) &&
                    !s.Grade.HasValue);

            // 5. Danh sách tối đa 5 bài nộp chưa chấm gần nhất.
            var pendingSubmissions = db.Submissions
                .Include(s => s.Assignment)
                .Include(s => s.Assignment.Class)
                .Include(s => s.Student)
                .Where(s =>
                    assignedClassIds.Contains(s.Assignment.ClassId) &&
                    !s.Grade.HasValue)
                .OrderBy(s => s.SubmittedAt)
                .Take(5)
                .ToList();

            // 6. Các bài tập sắp đến hạn thuộc lớp của giáo viên.
            var upcomingAssignments = db.Assignments
                .Include(a => a.Class)
                .Include(a => a.Class.Course)
                .Where(a =>
                    assignedClassIds.Contains(a.ClassId) &&
                    a.DueDate >= today)
                .OrderBy(a => a.DueDate)
                .Take(5)
                .ToList();

            var dashboardData = new TeacherDashboardViewModel
            {
                TeacherInfo = user,
                Profile = teacherProfile,

                TotalAssignedClasses = assignedClasses.Count,
                TotalStudentsTaught = totalStudents,

                AssignedClasses = assignedClasses,
                UpcomingTeachingSessions = upcomingSessions,

                TotalAssignments = totalAssignments,
                PendingSubmissionsCount = pendingSubmissionsCount,
                PendingSubmissions = pendingSubmissions,
                UpcomingAssignments = upcomingAssignments
            };

            return View(dashboardData);
        }

        [HttpGet]
        public ActionResult TeacherTeachingSchedule(int weekOffset = 0)
        {
            if (!IsAuthorizedRole("Teacher", "Giáo viên"))
            {
                TempData["ErrorMessage"] =
                    "Vui lòng đăng nhập bằng tài khoản Giáo viên!";

                return RedirectToAction("Login", "Auth");
            }

            int userId = Convert.ToInt32(Session["UserId"]);

            var teacher = db.Users.Find(userId);

            if (teacher == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            // Giới hạn khoảng tuần có thể điều hướng.
            weekOffset = Math.Max(-52, Math.Min(52, weekOffset));

            // 1. Lấy những lớp được phân công cho chính giáo viên này.
            var assignedClasses = db.Classes
                .Include(c => c.Course)
                .Where(c => c.TeacherId == userId)
                .OrderBy(c => c.ClassName)
                .ToList();

            var classIds = assignedClasses
                .Select(c => c.Id)
                .ToList();

            // 2. Đếm học viên thuộc mỗi lớp.
            var enrollmentRows = db.Enrollments
                .Where(e =>
                    classIds.Contains(e.ClassId) &&
                    (
                        e.Status == "Active" ||
                        e.Status == "Approved" ||
                        e.Status == "Studying" ||
                        e.Status == "Confirmed"
                    ))
                .Select(e => new
                {
                    e.ClassId,
                    e.StudentId
                })
                .ToList();

            var enrolledCounts = enrollmentRows
                .GroupBy(e => e.ClassId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.StudentId).Distinct().Count()
                );

            // 3. Tính ngày thứ Hai của tuần hiện tại.
            DateTime today = DateTime.Today;

            int daysSinceMonday =
                ((int)today.DayOfWeek + 6) % 7;

            DateTime currentMonday =
                today.AddDays(-daysSinceMonday);

            DateTime weekStart =
                currentMonday.AddDays(7 * weekOffset);

            DateTime weekEndExclusive =
                weekStart.AddDays(7);

            // 4. Chỉ lấy buổi dạy trong các lớp giáo viên phụ trách.
            var sessions = db.ClassSessions
                .Include(s => s.Class)
                .Include(s => s.Class.Course)
                .Where(s =>
                    classIds.Contains(s.ClassId) &&
                    s.SessionDate >= weekStart &&
                    s.SessionDate < weekEndExclusive)
                .OrderBy(s => s.SessionDate)
                .ThenBy(s => s.StartTime)
                .ToList();

            var viewModel = new TeacherTeachingScheduleViewModel
            {
                TeacherInfo = teacher,
                AssignedClasses = assignedClasses,
                Sessions = sessions,
                EnrolledCounts = enrolledCounts,
                WeekStart = weekStart,
                WeekEnd = weekEndExclusive.AddDays(-1),
                WeekOffset = weekOffset
            };

            return View(viewModel);
        }

        [HttpGet]
        public ActionResult TeacherAttendance(int? sessionId)
        {
            if (!IsAuthorizedRole("Teacher", "Giáo viên"))
            {
                TempData["ErrorMessage"] =
                    "Vui lòng đăng nhập bằng tài khoản Giáo viên!";

                return RedirectToAction("Login", "Auth");
            }

            int userId = Convert.ToInt32(Session["UserId"]);
            var teacher = db.Users.Find(userId);

            if (teacher == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            // Chỉ lấy các buổi học thuộc lớp của giáo viên hiện tại.
            var availableSessions = db.ClassSessions
                .Include(s => s.Class)
                .Include(s => s.Class.Course)
                .Where(s => s.Class.TeacherId == userId)
                .OrderBy(s => s.SessionDate)
                .ThenBy(s => s.StartTime)
                .ToList();

            ClassSession selectedSession;

            if (sessionId.HasValue)
            {
                selectedSession = availableSessions
                    .FirstOrDefault(s => s.Id == sessionId.Value);

                // Buổi học không thuộc giáo viên này thì không cho truy cập.
                if (selectedSession == null)
                {
                    return HttpNotFound();
                }
            }
            else
            {
                // Ưu tiên buổi học sắp tới; nếu không có thì lấy buổi gần nhất.
                selectedSession = availableSessions
                    .FirstOrDefault(s => s.SessionDate.Date >= DateTime.Today)
                    ?? availableSessions.LastOrDefault();
            }

            var viewModel = new TeacherAttendanceViewModel
            {
                TeacherInfo = teacher,
                AvailableSessions = availableSessions,
                SelectedSessionId = selectedSession != null
                    ? (int?)selectedSession.Id
                    : null,
                SelectedSession = selectedSession,
                Students = new List<TeacherAttendanceRowViewModel>()
            };

            if (selectedSession != null)
            {
                // Những trạng thái ghi danh được phép xuất hiện trong danh sách.
                var eligibleStatuses = new[]
                {
            "Confirmed",
            "Studying",
            "Completed",
            "Active",
            "Approved"
        };

                var enrollments = db.Enrollments
                    .Include(e => e.Student)
                    .Where(e =>
                        e.ClassId == selectedSession.ClassId &&
                        eligibleStatuses.Contains(e.Status))
                    .OrderBy(e => e.Student.FullName)
                    .ToList();

                var savedAttendance = db.Attendances
                    .Where(a => a.SessionId == selectedSession.Id)
                    .ToList();

                viewModel.Students = enrollments.Select(e =>
                {
                    var attendance = savedAttendance.FirstOrDefault(
                        a => a.StudentId == e.StudentId);

                    return new TeacherAttendanceRowViewModel
                    {
                        StudentId = e.StudentId,

                        FullName = e.Student != null
                            ? e.Student.FullName
                            : "Học viên",

                        Username = e.Student != null
                            ? e.Student.Username
                            : "",

                        Status = attendance != null
                            ? attendance.Status
                            : "",

                        Notes = attendance != null
                            ? attendance.Notes
                            : ""
                    };
                }).ToList();
            }

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SaveTeacherAttendance(TeacherAttendanceViewModel model)
        {
            if (!IsAuthorizedRole("Teacher", "Giáo viên"))
            {
                TempData["ErrorMessage"] =
                    "Vui lòng đăng nhập bằng tài khoản Giáo viên!";

                return RedirectToAction("Login", "Auth");
            }

            int userId = Convert.ToInt32(Session["UserId"]);

            if (!model.SelectedSessionId.HasValue)
            {
                return new HttpStatusCodeResult(
                    System.Net.HttpStatusCode.BadRequest);
            }

            int selectedSessionId = model.SelectedSessionId.Value;

            // Xác minh buổi học thuộc lớp được giao cho giáo viên hiện tại.
            var session = db.ClassSessions
                .Include(s => s.Class)
                .FirstOrDefault(s =>
                    s.Id == selectedSessionId &&
                    s.Class.TeacherId == userId);

            if (session == null)
            {
                return HttpNotFound();
            }

            var eligibleStatuses = new[]
            {
        "Confirmed",
        "Studying",
        "Completed",
        "Active",
        "Approved"
    };

            var eligibleStudentIds = db.Enrollments
                .Where(e =>
                    e.ClassId == session.ClassId &&
                    eligibleStatuses.Contains(e.Status))
                .Select(e => e.StudentId)
                .Distinct()
                .ToList();

            var rows = model.Students
                ?? new List<TeacherAttendanceRowViewModel>();

            // Không cho bỏ học viên, gửi trùng ID hoặc thêm người ngoài lớp.
            bool invalidRoster =
                rows.Count != eligibleStudentIds.Count ||
                rows.Select(r => r.StudentId).Distinct().Count() != rows.Count ||
                rows.Any(r => !eligibleStudentIds.Contains(r.StudentId)) ||
                eligibleStudentIds.Any(
                    id => !rows.Any(r => r.StudentId == id));

            if (invalidRoster)
            {
                TempData["ErrorMessage"] =
                    "Danh sách học viên đã thay đổi. Vui lòng tải lại trang.";

                return RedirectToAction(
                    "TeacherAttendance",
                    new { sessionId = selectedSessionId });
            }

            var allowedAttendanceStatuses = new[]
            {
        "Present",
        "Absent",
        "Late",
        "Excused"
    };

            // Kiểm tra tất cả trạng thái và ghi chú trước khi ghi dữ liệu.
            bool invalidInput = rows.Any(r =>
                string.IsNullOrWhiteSpace(r.Status) ||
                !allowedAttendanceStatuses.Contains(r.Status) ||
                (r.Notes != null && r.Notes.Length > 255));

            if (invalidInput)
            {
                TempData["ErrorMessage"] =
                    "Vui lòng chọn trạng thái hợp lệ cho từng học viên. " +
                    "Ghi chú không được vượt quá 255 ký tự.";

                return RedirectToAction(
                    "TeacherAttendance",
                    new { sessionId = selectedSessionId });
            }

            foreach (var row in rows)
            {
                var attendance = db.Attendances.FirstOrDefault(a =>
                    a.SessionId == selectedSessionId &&
                    a.StudentId == row.StudentId);

                if (attendance == null)
                {
                    attendance = new Attendance
                    {
                        SessionId = selectedSessionId,
                        StudentId = row.StudentId
                    };

                    db.Attendances.Add(attendance);
                }

                attendance.Status = row.Status;

                attendance.Notes = string.IsNullOrWhiteSpace(row.Notes)
                    ? null
                    : row.Notes.Trim();
            }

            db.SaveChanges();

            TempData["SuccessMessage"] =
                "Đã lưu điểm danh cho " + rows.Count +
                " học viên thành công.";

            return RedirectToAction(
                "TeacherAttendance",
                new { sessionId = selectedSessionId });
        }
        [HttpGet]
        public ActionResult TeacherGrades(int? classId)
        {
            if (!IsAuthorizedRole("Teacher", "Giáo viên"))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập bằng tài khoản Giáo viên!";
                return RedirectToAction("Login", "Auth");
            }

            int userId = Convert.ToInt32(Session["UserId"]);

            // Chỉ lấy những lớp do giáo viên hiện tại phụ trách.
            var availableClasses = db.Classes
                .Include(c => c.Course)
                .Where(c => c.TeacherId == userId)
                .OrderBy(c => c.ClassName)
                .ToList();

            // Không cho phép mở lớp của giáo viên khác.
            if (classId.HasValue &&
                !availableClasses.Any(c => c.Id == classId.Value))
            {
                return HttpNotFound();
            }

            int? selectedClassId = classId;

            // Nếu chưa chọn lớp, mặc định chọn lớp đầu tiên.
            if (!selectedClassId.HasValue && availableClasses.Any())
            {
                selectedClassId = availableClasses[0].Id;
            }

            var selectedClass = selectedClassId.HasValue
                ? availableClasses.FirstOrDefault(c => c.Id == selectedClassId.Value)
                : null;

            var studentRows = new List<TeacherGradeRowViewModel>();

            if (selectedClass != null)
            {
                // Hỗ trợ các trạng thái đăng ký đang được sử dụng
                // trong các phần khác nhau của dự án.
                var enrollments = db.Enrollments
                    .Include(e => e.Student)
                    .Include(e => e.AssessmentScore)
                    .Where(e =>
                        e.ClassId == selectedClass.Id &&
                        (
                            e.Status == "Confirmed" ||
                            e.Status == "Studying" ||
                            e.Status == "Completed" ||
                            e.Status == "Active" ||
                            e.Status == "Approved"
                        ))
                    .OrderBy(e => e.Student.FullName)
                    .ToList();

                var enrollmentIds = enrollments
                    .Select(e => e.Id)
                    .ToList();

                // Lấy nhận xét gần nhất của giáo viên hiện tại
                // cho từng lượt đăng ký lớp học.
                var feedbackList = db.TeacherFeedbacks
                    .Where(f =>
                        f.TeacherId == userId &&
                        enrollmentIds.Contains(f.EnrollmentId))
                    .OrderByDescending(f => f.CreatedAt)
                    .ToList();

                var feedbackByEnrollment = feedbackList
                    .GroupBy(f => f.EnrollmentId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.First().Comments
                    );

                foreach (var enrollment in enrollments)
                {
                    var score = enrollment.AssessmentScore;

                    string feedback = "";

                    if (feedbackByEnrollment.ContainsKey(enrollment.Id))
                    {
                        feedback = feedbackByEnrollment[enrollment.Id];
                    }

                    studentRows.Add(new TeacherGradeRowViewModel
                    {
                        EnrollmentId = enrollment.Id,
                        StudentId = enrollment.StudentId,

                        FullName = enrollment.Student != null
                            ? enrollment.Student.FullName
                            : "Chưa có tên",

                        Username = enrollment.Student != null
                            ? enrollment.Student.Username
                            : "",

                        MidtermScore = score != null
                            ? score.MidtermScore
                            : null,

                        FinalScore = score != null
                            ? score.FinalScore
                            : null,

                        TotalScore = score != null
                            ? score.TotalScore
                            : null,

                        LearningOutcome = score != null
                            ? score.LearningOutcome
                            : "",

                        FeedbackComments = feedback
                    });
                }
            }

            var model = new TeacherGradesViewModel
            {
                AvailableClasses = availableClasses,
                SelectedClassId = selectedClassId,
                SelectedClass = selectedClass,
                Students = studentRows
            };

            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SaveTeacherGrades(TeacherGradesViewModel model)
        {
            if (!IsAuthorizedRole("Teacher", "Giáo viên"))
            {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập bằng tài khoản Giáo viên!";
                return RedirectToAction("Login", "Auth");
            }

            if (model == null || !model.SelectedClassId.HasValue)
            {
                return new HttpStatusCodeResult(400);
            }

            int userId = Convert.ToInt32(Session["UserId"]);

            // Xác nhận lớp thuộc giáo viên đang đăng nhập.
            var selectedClass = db.Classes
                .Include(c => c.Course)
                .FirstOrDefault(c =>
                    c.Id == model.SelectedClassId.Value &&
                    c.TeacherId == userId);

            if (selectedClass == null)
            {
                return HttpNotFound();
            }

            var availableClasses = db.Classes
                .Include(c => c.Course)
                .Where(c => c.TeacherId == userId)
                .OrderBy(c => c.ClassName)
                .ToList();

            var rows = model.Students
                ?? new List<TeacherGradeRowViewModel>();

            // Danh sách lượt đăng ký thực sự thuộc lớp đã chọn.
            var validEnrollmentIds = db.Enrollments
                .Where(e =>
                    e.ClassId == selectedClass.Id &&
                    (
                        e.Status == "Confirmed" ||
                        e.Status == "Studying" ||
                        e.Status == "Completed" ||
                        e.Status == "Active" ||
                        e.Status == "Approved"
                    ))
                .Select(e => e.Id)
                .ToList();

            // Kiểm tra dữ liệu gửi lên để ngăn sửa EnrollmentId
            // hoặc gửi thêm học viên không thuộc lớp.
            if (rows.Any(r => r == null))
            {
                return new HttpStatusCodeResult(400);
            }

            var postedEnrollmentIds = rows
                .Select(r => r.EnrollmentId)
                .ToList();

            if (postedEnrollmentIds.Count != validEnrollmentIds.Count ||
                postedEnrollmentIds.Distinct().Count() != postedEnrollmentIds.Count ||
                !postedEnrollmentIds.OrderBy(id => id)
                    .SequenceEqual(validEnrollmentIds.OrderBy(id => id)))
            {
                return new HttpStatusCodeResult(400);
            }

            // Kiểm tra điểm, kết quả học tập và độ dài nhận xét.
            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];

                if (row.MidtermScore.HasValue &&
                    (row.MidtermScore.Value < 0 || row.MidtermScore.Value > 10))
                {
                    ModelState.AddModelError(
                        "Students[" + i + "].MidtermScore",
                        "Điểm giữa kỳ phải từ 0 đến 10.");
                }

                if (row.FinalScore.HasValue &&
                    (row.FinalScore.Value < 0 || row.FinalScore.Value > 10))
                {
                    ModelState.AddModelError(
                        "Students[" + i + "].FinalScore",
                        "Điểm cuối kỳ phải từ 0 đến 10.");
                }

                if (row.TotalScore.HasValue &&
                    (row.TotalScore.Value < 0 || row.TotalScore.Value > 10))
                {
                    ModelState.AddModelError(
                        "Students[" + i + "].TotalScore",
                        "Tổng điểm phải từ 0 đến 10.");
                }

                if (!string.IsNullOrEmpty(row.LearningOutcome) &&
                    row.LearningOutcome.Length > 50)
                {
                    ModelState.AddModelError(
                        "Students[" + i + "].LearningOutcome",
                        "Kết quả học tập không được vượt quá 50 ký tự.");
                }

                if (!string.IsNullOrEmpty(row.FeedbackComments) &&
                    row.FeedbackComments.Length > 4000)
                {
                    ModelState.AddModelError(
                        "Students[" + i + "].FeedbackComments",
                        "Nhận xét không được vượt quá 4000 ký tự.");
                }
            }

            if (!ModelState.IsValid)
            {
                // Giữ lại dữ liệu người dùng vừa nhập để sửa lỗi.
                model.AvailableClasses = availableClasses;
                model.SelectedClass = selectedClass;

                return View("TeacherGrades", model);
            }

            foreach (var row in rows)
            {
                // 1. Lưu điểm của học viên.
                var score = db.AssessmentScores.Find(row.EnrollmentId);

                bool hasAnyScore =
                    row.MidtermScore.HasValue ||
                    row.FinalScore.HasValue ||
                    row.TotalScore.HasValue ||
                    !string.IsNullOrWhiteSpace(row.LearningOutcome);

                if (hasAnyScore)
                {
                    if (score == null)
                    {
                        score = new AssessmentScore
                        {
                            EnrollmentId = row.EnrollmentId
                        };

                        db.AssessmentScores.Add(score);
                    }

                    score.MidtermScore = row.MidtermScore;
                    score.FinalScore = row.FinalScore;
                    score.TotalScore = row.TotalScore;

                    score.LearningOutcome =
                        string.IsNullOrWhiteSpace(row.LearningOutcome)
                            ? null
                            : row.LearningOutcome.Trim();
                }
                else if (score != null)
                {
                    // Nếu toàn bộ điểm và kết quả học tập bị xóa,
                    // xóa bản ghi điểm của lượt đăng ký này.
                    db.AssessmentScores.Remove(score);
                }

                // 2. Lưu nhận xét của giáo viên hiện tại.
                var comment = (row.FeedbackComments ?? "").Trim();

                if (!string.IsNullOrWhiteSpace(comment))
                {
                    var feedback = db.TeacherFeedbacks
                        .Where(f =>
                            f.EnrollmentId == row.EnrollmentId &&
                            f.TeacherId == userId)
                        .OrderByDescending(f => f.CreatedAt)
                        .FirstOrDefault();

                    if (feedback == null)
                    {
                        db.TeacherFeedbacks.Add(new TeacherFeedback
                        {
                            EnrollmentId = row.EnrollmentId,
                            TeacherId = userId,
                            Comments = comment,
                            CreatedAt = DateTime.Now
                        });
                    }
                    else
                    {
                        // Sửa nhận xét gần nhất thay vì tạo bản ghi mới.
                        feedback.Comments = comment;
                        feedback.CreatedAt = DateTime.Now;
                    }
                }
            }

            db.SaveChanges();

            TempData["SuccessMessage"] = "Đã lưu điểm và nhận xét học viên thành công!";

            return RedirectToAction(
                "TeacherGrades",
                new { classId = model.SelectedClassId });
        }
        [HttpGet]
        public ActionResult TeacherSubmissions(int? classId, int? assignmentId)
        {
            if (!IsAuthorizedRole("Teacher", "Giáo viên"))
            {
                TempData["ErrorMessage"] =
                    "Vui lòng đăng nhập bằng tài khoản Giáo viên!";

                return RedirectToAction("Login", "Auth");
            }

            int userId = Convert.ToInt32(Session["UserId"]);

            // Chỉ lấy các lớp do giáo viên hiện tại phụ trách.
            var availableClasses = db.Classes
                .Include(c => c.Course)
                .Where(c => c.TeacherId == userId)
                .OrderBy(c => c.ClassName)
                .ToList();

            if (classId.HasValue &&
                !availableClasses.Any(c => c.Id == classId.Value))
            {
                return HttpNotFound();
            }

            int? selectedClassId = classId;

            if (!selectedClassId.HasValue && availableClasses.Any())
            {
                selectedClassId = availableClasses[0].Id;
            }

            var selectedClass = selectedClassId.HasValue
                ? availableClasses.FirstOrDefault(
                    c => c.Id == selectedClassId.Value)
                : null;

            var availableAssignments = new List<Assignment>();
            var submissionRows = new List<TeacherSubmissionRowViewModel>();

            if (selectedClass != null)
            {
                availableAssignments = db.Assignments
                    .Where(a => a.ClassId == selectedClass.Id)
                    .OrderByDescending(a => a.DueDate)
                    .ToList();

                // Không cho xem bài tập thuộc lớp của giáo viên khác.
                if (assignmentId.HasValue &&
                    !availableAssignments.Any(
                        a => a.Id == assignmentId.Value))
                {
                    return HttpNotFound();
                }

                var assignmentIds = availableAssignments
                    .Select(a => a.Id)
                    .ToList();

                var submissionQuery = db.Submissions
                    .Include(s => s.Assignment)
                    .Include(s => s.Student)
                    .Where(s => assignmentIds.Contains(s.AssignmentId));

                if (assignmentId.HasValue)
                {
                    submissionQuery = submissionQuery
                        .Where(s => s.AssignmentId == assignmentId.Value);
                }

                var submissions = submissionQuery
                    .OrderByDescending(s => s.SubmittedAt)
                    .ToList();

                foreach (var submission in submissions)
                {
                    // Chỉ cho phép mở liên kết HTTP hoặc HTTPS.
                    string safeFileUrl = null;
                    Uri parsedUri;

                    if (!string.IsNullOrWhiteSpace(submission.FileUrl) &&
                        Uri.TryCreate(
                            submission.FileUrl,
                            UriKind.Absolute,
                            out parsedUri) &&
                        (parsedUri.Scheme == Uri.UriSchemeHttp ||
                         parsedUri.Scheme == Uri.UriSchemeHttps))
                    {
                        safeFileUrl = submission.FileUrl;
                    }

                    submissionRows.Add(new TeacherSubmissionRowViewModel
                    {
                        SubmissionId = submission.Id,
                        AssignmentId = submission.AssignmentId,

                        AssignmentTitle = submission.Assignment != null
                            ? submission.Assignment.Title
                            : "Bài tập",

                        StudentName = submission.Student != null
                            ? submission.Student.FullName
                            : "Học viên",

                        StudentUsername = submission.Student != null
                            ? submission.Student.Username
                            : "",

                        FileUrl = safeFileUrl,
                        SubmittedAt = submission.SubmittedAt,

                        DueDate = submission.Assignment != null
                            ? submission.Assignment.DueDate
                            : DateTime.MinValue,

                        Grade = submission.Grade
                    });
                }
            }

            var model = new TeacherSubmissionsViewModel
            {
                AvailableClasses = availableClasses,
                AvailableAssignments = availableAssignments,
                SelectedClassId = selectedClassId,
                SelectedAssignmentId = assignmentId,
                SelectedClass = selectedClass,
                Submissions = submissionRows
            };

            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SaveSubmissionGrades(
    TeacherSubmissionsViewModel model)
        {
            if (!IsAuthorizedRole("Teacher", "Giáo viên"))
            {
                TempData["ErrorMessage"] =
                    "Vui lòng đăng nhập bằng tài khoản Giáo viên!";

                return RedirectToAction("Login", "Auth");
            }

            if (model == null || !model.SelectedClassId.HasValue)
            {
                return new HttpStatusCodeResult(400);
            }

            int userId = Convert.ToInt32(Session["UserId"]);
            int selectedClassId = model.SelectedClassId.Value;

            // Kiểm tra lớp có thuộc giáo viên hiện tại không.
            var selectedClass = db.Classes.FirstOrDefault(c =>
                c.Id == selectedClassId &&
                c.TeacherId == userId);

            if (selectedClass == null)
            {
                return HttpNotFound();
            }

            // Chỉ cho phép chấm bài tập thuộc lớp đang chọn.
            var assignmentIds = db.Assignments
                .Where(a => a.ClassId == selectedClassId)
                .Select(a => a.Id)
                .ToList();

            if (model.SelectedAssignmentId.HasValue &&
                !assignmentIds.Contains(model.SelectedAssignmentId.Value))
            {
                return HttpNotFound();
            }

            var validSubmissionQuery = db.Submissions
                .Where(s => assignmentIds.Contains(s.AssignmentId));

            if (model.SelectedAssignmentId.HasValue)
            {
                validSubmissionQuery = validSubmissionQuery
                    .Where(s => s.AssignmentId ==
                        model.SelectedAssignmentId.Value);
            }

            var validSubmissionIds = validSubmissionQuery
                .Select(s => s.Id)
                .ToList();

            var rows = model.Submissions
                ?? new List<TeacherSubmissionRowViewModel>();

            if (rows.Any(r => r == null))
            {
                return new HttpStatusCodeResult(400);
            }

            var postedIds = rows
                .Select(r => r.SubmissionId)
                .ToList();

            // Ngăn gửi ID bài nộp ngoài lớp hoặc gửi trùng ID.
            if (postedIds.Distinct().Count() != postedIds.Count ||
                postedIds.Any(id => !validSubmissionIds.Contains(id)))
            {
                return new HttpStatusCodeResult(400);
            }

            // Nếu dữ liệu gửi lên không hợp lệ thì không lưu.
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] =
                    "Dữ liệu điểm không hợp lệ. Vui lòng kiểm tra lại.";

                return RedirectToAction(
                    "TeacherSubmissions",
                    new
                    {
                        classId = selectedClassId,
                        assignmentId = model.SelectedAssignmentId
                    });
            }

            foreach (var row in rows)
            {
                if (row.Grade.HasValue &&
                    (row.Grade.Value < 0 || row.Grade.Value > 10))
                {
                    TempData["ErrorMessage"] =
                        "Điểm bài tập phải nằm trong khoảng từ 0 đến 10.";

                    return RedirectToAction(
                        "TeacherSubmissions",
                        new
                        {
                            classId = selectedClassId,
                            assignmentId = model.SelectedAssignmentId
                        });
                }
            }

            foreach (var row in rows)
            {
                var submission = db.Submissions.FirstOrDefault(s =>
                    s.Id == row.SubmissionId &&
                    validSubmissionIds.Contains(s.Id));

                if (submission == null)
                {
                    return HttpNotFound();
                }

                // Cho phép nhập điểm hoặc xóa điểm đã nhập.
                submission.Grade = row.Grade;
            }

            db.SaveChanges();

            TempData["SuccessMessage"] =
                "Đã lưu điểm bài tập của học viên thành công!";

            return RedirectToAction(
                "TeacherSubmissions",
                new
                {
                    classId = selectedClassId,
                    assignmentId = model.SelectedAssignmentId
                });
        }
        [HttpGet]
        public ActionResult TeacherClassResources(int? classId)
        {
            if (!IsAuthorizedRole("Teacher", "Giáo viên"))
            {
                TempData["ErrorMessage"] =
                    "Vui lòng đăng nhập bằng tài khoản Giáo viên!";

                return RedirectToAction("Login", "Auth");
            }

            int userId = Convert.ToInt32(Session["UserId"]);

            // Chỉ lấy lớp do giáo viên hiện tại phụ trách.
            var availableClasses = db.Classes
                .Include(c => c.Course)
                .Where(c => c.TeacherId == userId)
                .OrderBy(c => c.ClassName)
                .ToList();

            if (classId.HasValue &&
                !availableClasses.Any(c => c.Id == classId.Value))
            {
                return HttpNotFound();
            }

            int? selectedClassId = classId;

            if (!selectedClassId.HasValue && availableClasses.Any())
            {
                selectedClassId = availableClasses[0].Id;
            }

            var selectedClass = selectedClassId.HasValue
                ? availableClasses.FirstOrDefault(
                    c => c.Id == selectedClassId.Value)
                : null;

            var assignments = new List<Assignment>();
            var materials = new List<CourseMaterial>();

            if (selectedClass != null)
            {
                assignments = db.Assignments
                    .Where(a => a.ClassId == selectedClass.Id)
                    .OrderByDescending(a => a.DueDate)
                    .ToList();

                materials = db.CourseMaterials
                    .Where(m => m.ClassId == selectedClass.Id)
                    .OrderByDescending(m => m.UploadedAt)
                    .ToList();
            }

            var model = new TeacherClassResourcesViewModel
            {
                AvailableClasses = availableClasses,
                SelectedClassId = selectedClassId,
                SelectedClass = selectedClass,
                Assignments = assignments,
                Materials = materials
            };

            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateTeacherAssignment(
    TeacherClassResourcesViewModel model)
        {
            if (!IsAuthorizedRole("Teacher", "Giáo viên"))
            {
                TempData["ErrorMessage"] =
                    "Vui lòng đăng nhập bằng tài khoản Giáo viên!";

                return RedirectToAction("Login", "Auth");
            }

            if (model == null || !model.SelectedClassId.HasValue)
            {
                return new HttpStatusCodeResult(400);
            }

            int userId = Convert.ToInt32(Session["UserId"]);
            int classId = model.SelectedClassId.Value;

            // Xác minh lớp thuộc giáo viên đang đăng nhập.
            var selectedClass = db.Classes.FirstOrDefault(c =>
                c.Id == classId && c.TeacherId == userId);

            if (selectedClass == null)
            {
                return HttpNotFound();
            }

            string title = (model.NewAssignmentTitle ?? "").Trim();

            if (string.IsNullOrWhiteSpace(title))
            {
                TempData["ErrorMessage"] =
                    "Vui lòng nhập tiêu đề bài tập.";
            }
            else if (title.Length > 255)
            {
                TempData["ErrorMessage"] =
                    "Tiêu đề bài tập không được vượt quá 255 ký tự.";
            }
            else if (!model.NewAssignmentDueDate.HasValue)
            {
                TempData["ErrorMessage"] =
                    "Vui lòng chọn hạn nộp bài tập.";
            }
            else
            {
                var assignment = new Assignment
                {
                    ClassId = classId,
                    Title = title,
                    DueDate = model.NewAssignmentDueDate.Value
                };

                db.Assignments.Add(assignment);
                db.SaveChanges();

                TempData["SuccessMessage"] =
                    "Đã tạo bài tập thành công!";

                return RedirectToAction(
                    "TeacherClassResources",
                    new { classId = classId });
            }

            return RedirectToAction(
                "TeacherClassResources",
                new { classId = classId });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateTeacherMaterial(
    TeacherClassResourcesViewModel model)
        {
            if (!IsAuthorizedRole("Teacher", "Giáo viên"))
            {
                TempData["ErrorMessage"] =
                    "Vui lòng đăng nhập bằng tài khoản Giáo viên!";

                return RedirectToAction("Login", "Auth");
            }

            if (model == null || !model.SelectedClassId.HasValue)
            {
                return new HttpStatusCodeResult(400);
            }

            int userId = Convert.ToInt32(Session["UserId"]);
            int classId = model.SelectedClassId.Value;

            var selectedClass = db.Classes.FirstOrDefault(c =>
                c.Id == classId && c.TeacherId == userId);

            if (selectedClass == null)
            {
                return HttpNotFound();
            }

            string title = (model.NewMaterialTitle ?? "").Trim();
            string fileUrl = (model.NewMaterialFileUrl ?? "").Trim();

            Uri parsedUri;

            bool validUrl =
                Uri.TryCreate(fileUrl, UriKind.Absolute, out parsedUri) &&
                (parsedUri.Scheme == Uri.UriSchemeHttp ||
                 parsedUri.Scheme == Uri.UriSchemeHttps);

            if (string.IsNullOrWhiteSpace(title))
            {
                TempData["ErrorMessage"] =
                    "Vui lòng nhập tên tài liệu.";
            }
            else if (title.Length > 255)
            {
                TempData["ErrorMessage"] =
                    "Tên tài liệu không được vượt quá 255 ký tự.";
            }
            else if (string.IsNullOrWhiteSpace(fileUrl) ||
                     fileUrl.Length > 500 ||
                     !validUrl)
            {
                TempData["ErrorMessage"] =
                    "Đường dẫn tài liệu phải là liên kết HTTP hoặc HTTPS hợp lệ, tối đa 500 ký tự.";
            }
            else
            {
                var material = new CourseMaterial
                {
                    ClassId = classId,
                    Title = title,
                    FileUrl = fileUrl,
                    UploadedAt = DateTime.Now
                };

                db.CourseMaterials.Add(material);
                db.SaveChanges();

                TempData["SuccessMessage"] =
                    "Đã đăng tài liệu thành công!";

                return RedirectToAction(
                    "TeacherClassResources",
                    new { classId = classId });
            }

            return RedirectToAction(
                "TeacherClassResources",
                new { classId = classId });
        }
        [HttpGet]
        public ActionResult TeacherProfile()
        {
            if (!IsAuthorizedRole("Teacher", "Giáo viên"))
            {
                TempData["ErrorMessage"] =
                    "Vui lòng đăng nhập bằng tài khoản Giáo viên!";

                return RedirectToAction("Login", "Auth");
            }

            int userId = Convert.ToInt32(Session["UserId"]);

            var user = db.Users
                .Include(u => u.TeacherProfile)
                .FirstOrDefault(u => u.Id == userId);

            if (user == null)
            {
                return HttpNotFound();
            }

            var model = new TeacherProfileViewModel
            {
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,

                Specialization = user.TeacherProfile != null
                    ? user.TeacherProfile.Specialization
                    : "",

                Bio = user.TeacherProfile != null
                    ? user.TeacherProfile.Bio
                    : ""
            };

            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult TeacherProfile(TeacherProfileViewModel model)
        {
            if (!IsAuthorizedRole("Teacher", "Giáo viên"))
            {
                TempData["ErrorMessage"] =
                    "Vui lòng đăng nhập bằng tài khoản Giáo viên!";

                return RedirectToAction("Login", "Auth");
            }

            if (model == null)
            {
                return new HttpStatusCodeResult(400);
            }

            int userId = Convert.ToInt32(Session["UserId"]);

            var user = db.Users
                .Include(u => u.TeacherProfile)
                .FirstOrDefault(u => u.Id == userId);

            if (user == null)
            {
                return HttpNotFound();
            }

            // Không cho phép tài khoản hiện tại đổi sang email
            // đã được người dùng khác sử dụng.
            string email = (model.Email ?? "").Trim();

            bool emailExists = !string.IsNullOrWhiteSpace(email) &&
                db.Users.Any(u =>
                    u.Id != userId &&
                    u.Email == email);

            if (emailExists)
            {
                ModelState.AddModelError(
                    "Email",
                    "Email này đã được sử dụng bởi tài khoản khác.");
            }

            if (!ModelState.IsValid)
            {
                model.Username = user.Username;
                return View(model);
            }

            // Cập nhật thông tin tài khoản.
            user.FullName = model.FullName.Trim();
            user.Email = email;

            user.Phone = string.IsNullOrWhiteSpace(model.Phone)
                ? null
                : model.Phone.Trim();

            // Lấy hồ sơ chuyên môn hiện có hoặc tạo mới.
            var profile = user.TeacherProfile;

            if (profile == null)
            {
                profile = new TeacherProfile
                {
                    UserId = userId
                };

                db.TeacherProfiles.Add(profile);
            }

            profile.Specialization =
                string.IsNullOrWhiteSpace(model.Specialization)
                    ? null
                    : model.Specialization.Trim();

            profile.Bio =
                string.IsNullOrWhiteSpace(model.Bio)
                    ? null
                    : model.Bio.Trim();

            db.SaveChanges();

            // Cập nhật tên hiển thị trong Session nếu dự án
            // đang sử dụng Session["FullName"].
            Session["FullName"] = user.FullName;

            TempData["SuccessMessage"] =
                "Cập nhật hồ sơ giáo viên thành công!";

            return RedirectToAction("TeacherProfile");
        }









        #endregion

        #region 4. STUDENT DASHBOARD (PHẦN C)

        // GET: /Home/StudentDashboard

        [HttpGet]
        public ActionResult StudentDashboard()
        {
            if (!IsAuthorizedRole("Student", "Học viên"))
            {
                TempData["ErrorMessage"] =
                    "Vui lòng đăng nhập bằng tài khoản Học viên!";

                return RedirectToAction("Login", "Auth");
            }

            int userId = Convert.ToInt32(Session["UserId"]);

            var user = db.Users.Find(userId);

            var studentProfile = db.StudentProfiles
                .FirstOrDefault(sp => sp.UserId == userId);

            // Lấy các lớp đã duyệt hoặc đang học
            var activeEnrollments = db.Enrollments
                .Include(e => e.Class)
                .Include(e => e.Class.Course)
                .Where(e =>
                    e.StudentId == userId &&
                    (e.Status == "Active" || e.Status == "Approved"))
                .OrderByDescending(e => e.EnrollmentDate)
                .ToList();

            var enrolledClassIds = activeEnrollments
                .Select(e => e.ClassId)
                .Distinct()
                .ToList();

            var enrollmentIds = activeEnrollments
                .Select(e => e.Id)
                .ToList();

            // Lịch học sắp tới
            DateTime today = DateTime.Today;

            var upcomingSessions = db.ClassSessions
                .Include(s => s.Class)
                .Include(s => s.Class.Course)
                .Where(s =>
                    enrolledClassIds.Contains(s.ClassId) &&
                    s.SessionDate >= today)
                .OrderBy(s => s.SessionDate)
                .ThenBy(s => s.StartTime)
                .Take(5)
                .ToList();

            // Tổng số chứng chỉ
            int totalCertificates = db.Certificates
                .Count(c => c.Enrollment.StudentId == userId);

            // Tổng học phí các lớp đã duyệt/đang học
            decimal totalTuitionFee = activeEnrollments
                .Where(e => e.Class != null && e.Class.Course != null)
                .Sum(e => e.Class.Course.TuitionFee);

            // Các khoản thanh toán đã ghi nhận
            decimal totalPaid = 0m;

            if (enrollmentIds.Any())
            {
                totalPaid = db.Payments
                    .Where(p =>
                        enrollmentIds.Contains(p.EnrollmentId) &&
                        (p.PaymentStatus == "Paid" ||
                         p.PaymentStatus == "Partial" ||
                         p.PaymentStatus == "Completed"))
                    .Select(p => (decimal?)p.AmountPaid)
                    .Sum() ?? 0m;
            }

            decimal outstandingBalance =
                Math.Max(totalTuitionFee - totalPaid, 0m);

            // Thông báo gần đây
            var recentNotifications = db.Notifications
                .Where(n => n.UserId == userId)
                .OrderBy(n => n.IsRead)
                .ThenByDescending(n => n.CreatedAt)
                .Take(4)
                .ToList();

            // Kết quả đánh giá gần đây
            var recentScores = db.AssessmentScores
                .Include(a => a.Enrollment)
                .Include(a => a.Enrollment.Class)
                .Include(a => a.Enrollment.Class.Course)
                .Where(a => a.Enrollment.StudentId == userId)
                .OrderByDescending(a => a.Enrollment.EnrollmentDate)
                .Take(4)
                .ToList();

            var dashboardData = new StudentDashboardViewModel
            {
                StudentInfo = user,
                Profile = studentProfile,

                TotalEnrolledClasses = activeEnrollments.Count,
                TotalCertificates = totalCertificates,

                ActiveEnrollments = activeEnrollments,
                UpcomingSessions = upcomingSessions,

                TotalTuitionFee = totalTuitionFee,
                TotalPaid = totalPaid,
                OutstandingBalance = outstandingBalance,

                RecentNotifications = recentNotifications,
                RecentScores = recentScores
            };

            return View(dashboardData);
        }



        [HttpGet]
        public ActionResult StudentPayments()
        {
            if (!IsAuthorizedRole("Student", "Học viên"))
            {
                TempData["ErrorMessage"] =
                    "Vui lòng đăng nhập bằng tài khoản Học viên!";

                return RedirectToAction("Login", "Auth");
            }

            int userId = Convert.ToInt32(Session["UserId"]);

            var user = db.Users.Find(userId);

            // Lấy các lượt ghi danh của chính học viên này.
            var enrollments = db.Enrollments
                .Include(e => e.Class)
                .Include(e => e.Class.Course)
                .Where(e =>
                    e.StudentId == userId &&
                    e.Status != "Cancelled" &&
                    e.Status != "Rejected")
                .OrderByDescending(e => e.EnrollmentDate)
                .ToList();

            var enrollmentIds = enrollments
                .Select(e => e.Id)
                .ToList();

            // Chỉ các lớp đã duyệt hoặc đang học được tính vào học phí.
            var billableEnrollments = enrollments
                .Where(e =>
                    (e.Status == "Approved" || e.Status == "Active") &&
                    e.Class != null &&
                    e.Class.Course != null)
                .ToList();

            var billableEnrollmentIds = billableEnrollments
                .Select(e => e.Id)
                .ToList();

            // Lịch sử thanh toán của học viên hiện tại.
            var payments = db.Payments
                .Include(p => p.Enrollment)
                .Include(p => p.Enrollment.Class)
                .Include(p => p.Enrollment.Class.Course)
                .Where(p => enrollmentIds.Contains(p.EnrollmentId))
                .OrderByDescending(p => p.PaymentDate)
                .ToList();

            decimal totalTuitionFee = billableEnrollments
                .Sum(e => e.Class.Course.TuitionFee);

            // Chỉ tính các giao dịch đã thanh toán hoặc thanh toán một phần.
            decimal totalPaid = payments
                .Where(p =>
                    billableEnrollmentIds.Contains(p.EnrollmentId) &&
                    (p.PaymentStatus == "Paid" ||
                     p.PaymentStatus == "Partial" ||
                     p.PaymentStatus == "Completed"))
                .Sum(p => p.AmountPaid);

            decimal outstandingBalance =
                Math.Max(totalTuitionFee - totalPaid, 0m);

            var viewModel = new StudentBillingViewModel
            {
                StudentInfo = user,
                Enrollments = enrollments,
                PaymentHistory = payments,
                TotalTuitionFee = totalTuitionFee,
                TotalPaid = totalPaid,
                OutstandingBalance = outstandingBalance,
                PendingEnrollmentCount = enrollments.Count(
                    e => e.Status == "Pending")
            };

            return View(viewModel);
        }

        [HttpGet]
        public ActionResult StudentTimetable(int weekOffset = 0)
        {
            if (!IsAuthorizedRole("Student", "Học viên"))
            {
                TempData["ErrorMessage"] =
                    "Vui lòng đăng nhập bằng tài khoản Học viên!";

                return RedirectToAction("Login", "Auth");
            }

            int userId = Convert.ToInt32(Session["UserId"]);

            var user = db.Users.Find(userId);

            // Lấy những lớp đã được duyệt hoặc đang học của học viên.
            var classIds = db.Enrollments
                .Where(e =>
                    e.StudentId == userId &&
                    (e.Status == "Active" || e.Status == "Approved"))
                .Select(e => e.ClassId)
                .Distinct()
                .ToList();

            // Xác định ngày thứ Hai đầu tuần hiện tại.
            DateTime today = DateTime.Today;

            int daysSinceMonday =
                ((int)today.DayOfWeek + 6) % 7;

            DateTime currentMonday =
                today.AddDays(-daysSinceMonday);

            // Cho phép chuyển sang tuần trước hoặc tuần sau.
            DateTime weekStart =
                currentMonday.AddDays(7 * weekOffset);

            DateTime weekEndExclusive =
                weekStart.AddDays(7);

            // Chỉ lấy lịch học thuộc các lớp của học viên.
            var sessions = db.ClassSessions
                .Include(s => s.Class)
                .Include(s => s.Class.Course)
                .Where(s =>
                    classIds.Contains(s.ClassId) &&
                    s.SessionDate >= weekStart &&
                    s.SessionDate < weekEndExclusive)
                .OrderBy(s => s.SessionDate)
                .ThenBy(s => s.StartTime)
                .ToList();

            var viewModel = new StudentTimetableViewModel
            {
                StudentInfo = user,
                WeekStart = weekStart,
                WeekEnd = weekEndExclusive.AddDays(-1),
                WeekOffset = weekOffset,
                Sessions = sessions
            };

            return View(viewModel);
        }

        [HttpGet]
        public ActionResult StudentProgress(int? enrollmentId)
        {
            if (!IsAuthorizedRole("Student", "Học viên"))
            {
                TempData["ErrorMessage"] =
                    "Vui lòng đăng nhập bằng tài khoản Học viên!";

                return RedirectToAction("Login", "Auth");
            }

            int userId = Convert.ToInt32(Session["UserId"]);
            var user = db.Users.Find(userId);

            // Chỉ lấy các lớp thuộc học viên hiện tại.
            // Loại các đơn chưa duyệt hoặc đã bị hủy/từ chối.
            var enrollments = db.Enrollments
                .Include(e => e.Class)
                .Include(e => e.Class.Course)
                .Where(e =>
                    e.StudentId == userId &&
                    e.Status != "Pending" &&
                    e.Status != "Withdrawn" &&
                    e.Status != "Cancelled" &&
                    e.Status != "Rejected")
                .OrderByDescending(e => e.EnrollmentDate)
                .ToList();

            // Chỉ cho phép chọn một lượt ghi danh thuộc chính học viên.
            var selectedEnrollment = enrollmentId.HasValue
                ? enrollments.FirstOrDefault(
                    e => e.Id == enrollmentId.Value)
                : enrollments.FirstOrDefault();

            var viewModel = new StudentProgressViewModel
            {
                StudentInfo = user,
                Enrollments = enrollments,
                SelectedEnrollmentId = selectedEnrollment?.Id,
                SelectedEnrollment = selectedEnrollment,
                AttendanceRecords = new System.Collections.Generic.List<Attendance>(),
                TeacherFeedbacks = new System.Collections.Generic.List<TeacherFeedback>()
            };

            // Chưa có lớp được duyệt thì vẫn hiển thị trang,
            // nhưng không truy vấn điểm, điểm danh hay nhận xét.
            if (selectedEnrollment == null)
            {
                return View(viewModel);
            }

            int selectedClassId = selectedEnrollment.ClassId;
            int selectedId = selectedEnrollment.Id;

            // 1. Bảng điểm của lượt ghi danh đã chọn.
            viewModel.Score = db.AssessmentScores
                .FirstOrDefault(s => s.EnrollmentId == selectedId);

            // 2. Nhật ký điểm danh của học viên trong lớp đã chọn.
            var attendanceRecords = db.Attendances
                .Include(a => a.ClassSession)
                .Where(a =>
                    a.StudentId == userId &&
                    a.ClassSession.ClassId == selectedClassId)
                .OrderByDescending(a => a.ClassSession.SessionDate)
                .ThenByDescending(a => a.ClassSession.StartTime)
                .ToList();

            viewModel.AttendanceRecords = attendanceRecords;

            viewModel.TotalAttendanceRecords = attendanceRecords.Count;

            viewModel.PresentCount = attendanceRecords.Count(
                a => a.Status == "Present");

            viewModel.LateCount = attendanceRecords.Count(
                a => a.Status == "Late");

            viewModel.AbsentCount = attendanceRecords.Count(
                a => a.Status == "Absent");

            viewModel.ExcusedCount = attendanceRecords.Count(
                a => a.Status == "Excused");

            // Tỷ lệ có mặt: Present + Late trên số bản ghi điểm danh.
            viewModel.AttendanceRate =
                attendanceRecords.Count == 0
                    ? 0m
                    : (decimal)(
                        viewModel.PresentCount + viewModel.LateCount)
                        * 100m / attendanceRecords.Count;

            // 3. Nhận xét giáo viên.
            viewModel.TeacherFeedbacks = db.TeacherFeedbacks
                .Include(f => f.Teacher)
                .Where(f => f.EnrollmentId == selectedId)
                .OrderByDescending(f => f.CreatedAt)
                .ToList();

            // 4. Chứng chỉ nếu học viên đã được cấp.
            viewModel.Certificate = db.Certificates
                .FirstOrDefault(c => c.EnrollmentId == selectedId);

            return View(viewModel);
        }

        [HttpGet]
        public ActionResult StudentLearningResources()
        {
            if (!IsAuthorizedRole("Student", "Học viên"))
            {
                TempData["ErrorMessage"] =
                    "Vui lòng đăng nhập bằng tài khoản Học viên!";

                return RedirectToAction("Login", "Auth");
            }

            int userId = Convert.ToInt32(Session["UserId"]);
            var user = db.Users.Find(userId);

            // Lấy các lớp mà học viên đang hoặc đã tham gia.
            var classIds = db.Enrollments
                .Where(e =>
                    e.StudentId == userId &&
                    (e.Status == "Confirmed" ||
                     e.Status == "Studying" ||
                     e.Status == "Completed" ||
                     e.Status == "Active" ||
                     e.Status == "Approved"))
                .Select(e => e.ClassId)
                .Distinct()
                .ToList();

            // Bài tập thuộc các lớp của học viên.
            var assignments = db.Assignments
                .Include(a => a.Class)
                .Include(a => a.Class.Course)
                .Where(a => classIds.Contains(a.ClassId))
                .OrderBy(a => a.DueDate)
                .ToList();

            // Tài liệu của các lớp tương ứng.
            var materials = db.CourseMaterials
                .Include(m => m.Class)
                .Include(m => m.Class.Course)
                .Where(m => classIds.Contains(m.ClassId))
                .OrderByDescending(m => m.UploadedAt)
                .ToList();

            // Chỉ lấy bài nộp của học viên hiện tại.
            var assignmentIds = assignments
                .Select(a => a.Id)
                .ToList();

            var submissions = db.Submissions
                .Where(s =>
                    s.StudentId == userId &&
                    assignmentIds.Contains(s.AssignmentId))
                .OrderByDescending(s => s.SubmittedAt)
                .ToList();

            var viewModel = new StudentLearningViewModel
            {
                StudentInfo = user,
                Assignments = assignments,
                Materials = materials,
                Submissions = submissions
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SubmitLearningAssignment(
            int assignmentId,
            string fileUrl)
        {
            if (!IsAuthorizedRole("Student", "Học viên"))
            {
                TempData["ErrorMessage"] =
                    "Vui lòng đăng nhập bằng tài khoản Học viên!";

                return RedirectToAction("Login", "Auth");
            }

            int userId = Convert.ToInt32(Session["UserId"]);

            if (string.IsNullOrWhiteSpace(fileUrl))
            {
                TempData["ErrorMessage"] =
                    "Vui lòng nhập đường dẫn bài làm.";

                return RedirectToAction("StudentLearningResources");
            }

            fileUrl = fileUrl.Trim();

            if (fileUrl.Length > 500)
            {
                TempData["ErrorMessage"] =
                    "Đường dẫn không được vượt quá 500 ký tự.";

                return RedirectToAction("StudentLearningResources");
            }

            // Chỉ chấp nhận liên kết HTTP hoặc HTTPS.
            Uri parsedUri;

            bool validUrl =
                Uri.TryCreate(fileUrl, UriKind.Absolute, out parsedUri) &&
                (parsedUri.Scheme == Uri.UriSchemeHttp ||
                 parsedUri.Scheme == Uri.UriSchemeHttps);

            if (!validUrl)
            {
                TempData["ErrorMessage"] =
                    "Đường dẫn phải bắt đầu bằng http:// hoặc https://.";

                return RedirectToAction("StudentLearningResources");
            }

            // Kiểm tra bài tập có tồn tại không.
            var assignment = db.Assignments
                .FirstOrDefault(a => a.Id == assignmentId);

            if (assignment == null)
            {
                return HttpNotFound();
            }

            // Kiểm tra học viên có thuộc lớp của bài tập không.
            bool belongsToStudent = db.Enrollments.Any(e =>
                e.StudentId == userId &&
                e.ClassId == assignment.ClassId &&
                (e.Status == "Confirmed" ||
                 e.Status == "Studying" ||
                 e.Status == "Completed" ||
                 e.Status == "Active" ||
                 e.Status == "Approved"));

            if (!belongsToStudent)
            {
                return new HttpStatusCodeResult(
                    System.Net.HttpStatusCode.Forbidden);
            }

            // Tìm bài nộp mới nhất của học viên cho bài tập này.
            var existingSubmission = db.Submissions
                .Where(s =>
                    s.AssignmentId == assignmentId &&
                    s.StudentId == userId)
                .OrderByDescending(s => s.SubmittedAt)
                .FirstOrDefault();

            // Không thay thế bài đã được giáo viên chấm điểm.
            if (existingSubmission != null &&
                existingSubmission.Grade.HasValue)
            {
                TempData["ErrorMessage"] =
                    "Bài tập đã được chấm điểm, không thể thay đổi bài nộp.";

                return RedirectToAction("StudentLearningResources");
            }

            if (existingSubmission != null)
            {
                existingSubmission.FileUrl = fileUrl;
                existingSubmission.SubmittedAt = DateTime.Now;
            }
            else
            {
                var submission = new Submission
                {
                    AssignmentId = assignmentId,
                    StudentId = userId,
                    FileUrl = fileUrl,
                    SubmittedAt = DateTime.Now
                };

                db.Submissions.Add(submission);
            }

            db.SaveChanges();

            TempData["SuccessMessage"] =
                DateTime.Now > assignment.DueDate
                ? "Bài nộp muộn đã được ghi nhận."
                : "Bài tập của bạn đã được gửi thành công.";

            return RedirectToAction("StudentLearningResources");
        }

        [HttpGet]
        public ActionResult StudentProfile()
        {
            if (!IsAuthorizedRole("Student", "Học viên"))
            {
                TempData["ErrorMessage"] =
                    "Vui lòng đăng nhập bằng tài khoản Học viên!";

                return RedirectToAction("Login", "Auth");
            }

            int userId = Convert.ToInt32(Session["UserId"]);

            var user = db.Users.Find(userId);

            if (user == null)
            {
                return HttpNotFound();
            }

            // StudentProfile dùng UserId làm khóa chính.
            var profile = db.StudentProfiles.Find(userId);

            var viewModel = new StudentProfileViewModel
            {
                Username = user.Username,
                CreatedAt = user.CreatedAt,

                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,

                DateOfBirth = profile != null
                    ? profile.DateOfBirth
                    : null,

                Address = profile != null
                    ? profile.Address
                    : null
            };

            return View(viewModel);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult StudentProfile(StudentProfileViewModel model)
        {
            if (!IsAuthorizedRole("Student", "Học viên"))
            {
                TempData["ErrorMessage"] =
                    "Vui lòng đăng nhập bằng tài khoản Học viên!";

                return RedirectToAction("Login", "Auth");
            }

            int userId = Convert.ToInt32(Session["UserId"]);

            // Không lấy UserId từ biểu mẫu để xác định người được cập nhật.
            // Luôn sử dụng tài khoản đang đăng nhập.
            var user = db.Users.Find(userId);

            if (user == null)
            {
                return HttpNotFound();
            }

            // Những trường chỉ đọc phải lấy lại từ database.
            model.Username = user.Username;
            model.CreatedAt = user.CreatedAt;

            // Không cho phép đặt ngày sinh trong tương lai.
            if (model.DateOfBirth.HasValue &&
                model.DateOfBirth.Value.Date > DateTime.Today)
            {
                ModelState.AddModelError(
                    "DateOfBirth",
                    "Ngày sinh không được ở trong tương lai.");
            }

            // Kiểm tra email có thuộc tài khoản khác không.
            if (ModelState.IsValid &&
                db.Users.Any(u =>
                    u.Id != userId &&
                    u.Email == model.Email))
            {
                ModelState.AddModelError(
                    "Email",
                    "Email này đã được sử dụng bởi tài khoản khác.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Cập nhật thông tin tài khoản hiện tại.
            user.FullName = model.FullName.Trim();
            user.Email = model.Email.Trim();
            user.Phone = string.IsNullOrWhiteSpace(model.Phone)
                ? null
                : model.Phone.Trim();

            // Tìm hồ sơ học viên hiện có.
            var profile = db.StudentProfiles.Find(userId);

            if (profile == null)
            {
                // Chưa có hồ sơ thì tạo mới.
                profile = new StudentProfile
                {
                    UserId = userId,
                    DateOfBirth = model.DateOfBirth,
                    Address = string.IsNullOrWhiteSpace(model.Address)
                        ? null
                        : model.Address.Trim()
                };

                db.StudentProfiles.Add(profile);
            }
            else
            {
                profile.DateOfBirth = model.DateOfBirth;
                profile.Address = string.IsNullOrWhiteSpace(model.Address)
                    ? null
                    : model.Address.Trim();
            }

            db.SaveChanges();

            // Đồng bộ lời chào ở các khu vực đang dùng Session.
            Session["FullName"] = user.FullName;

            TempData["SuccessMessage"] =
                "Cập nhật hồ sơ cá nhân thành công.";

            return RedirectToAction("StudentProfile");
        }









        #endregion

        #region 5. TRANG THÔNG TIN BỔ TRỢ & HELPER

        public ActionResult About()
        {
            ViewBag.Message = "Giới thiệu về Trung tâm Anh ngữ";
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Thông tin liên hệ Trung tâm";
            return View();
        }

        private bool IsAuthorizedRole(params string[] allowedRoles)
        {
            if (Session["UserId"] == null) return false;
            string currentRole = Session["RoleName"]?.ToString();
            if (string.IsNullOrEmpty(currentRole)) return false;

            return allowedRoles.Any(r => r.Equals(currentRole, StringComparison.OrdinalIgnoreCase));
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