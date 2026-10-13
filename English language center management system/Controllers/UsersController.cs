using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web.Mvc;
using English_language_center_management_system.Models;
using English_language_center_management_system.Models.ViewModels.Users;

namespace English_language_center_management_system.Controllers
{
    public class UsersController : Controller
    {
        private EnglishCenterDbContext db = new EnglishCenterDbContext();

        #region 1. DANH SÁCH NGƯỜI DÙNG (INDEX)

        // GET: Users
        public ActionResult Index(string searchTerm, int? roleId)
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập vào chức năng này!";
                return RedirectToAction("Login", "Auth");
            }

            var usersQuery = db.Users
                .Include(u => u.Role)
                .Include(u => u.StudentProfile)
                .Include(u => u.TeacherProfile)
                .AsQueryable();

            // Tìm kiếm theo Username, FullName hoặc Email
            if (!string.IsNullOrEmpty(searchTerm))
            {
                usersQuery = usersQuery.Where(u =>
                    u.Username.Contains(searchTerm) ||
                    u.FullName.Contains(searchTerm) ||
                    u.Email.Contains(searchTerm) ||
                    u.Phone.Contains(searchTerm));
            }

            // Lọc theo Vai trò (RoleId)
            if (roleId.HasValue && roleId.Value > 0)
            {
                usersQuery = usersQuery.Where(u => u.RoleId == roleId.Value);
            }

            var viewModel = new UserIndexViewModel
            {
                Users = usersQuery.OrderByDescending(u => u.CreatedAt).ToList(),
                SearchTerm = searchTerm,
                SelectedRoleId = roleId,
                Roles = db.Roles.ToList()
            };

            return View(viewModel);
        }

        #endregion

        #region 2. XEM CHI TIẾT (DETAILS)

        // GET: Users/Details/5
        public ActionResult Details(int? id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            User user = db.Users
                .Include(u => u.Role)
                .Include(u => u.StudentProfile)
                .Include(u => u.TeacherProfile)
                .FirstOrDefault(u => u.Id == id);

            if (user == null)
            {
                return HttpNotFound();
            }

            return View(user);
        }

        #endregion

        #region 3. THÊM MỚI NGƯỜI DÙNG (CREATE)

        // GET: Users/Create
        public ActionResult Create()
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            ViewBag.RoleId = new SelectList(db.Roles, "Id", "Name");
            return View();
        }

        // POST: Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(UserCreateViewModel model)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            if (ModelState.IsValid)
            {
                // Kiểm tra trùng Username / Email
                if (db.Users.Any(u => u.Username == model.Username))
                {
                    ModelState.AddModelError("Username", "Tên đăng nhập này đã tồn tại.");
                }
                if (db.Users.Any(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "Email này đã được sử dụng.");
                }

                if (ModelState.IsValid)
                {
                    var user = new User
                    {
                        Username = model.Username,
                        PasswordHash = HashPassword(model.Password),
                        FullName = model.FullName,
                        Email = model.Email,
                        Phone = model.Phone,
                        RoleId = model.RoleId,
                        CreatedAt = DateTime.Now
                    };

                    db.Users.Add(user);
                    db.SaveChanges(); // Lưu để có user.Id

                    // Tự động khởi tạo Hồ sơ dựa theo Vai trò đã chọn
                    var role = db.Roles.Find(model.RoleId);
                    if (role != null)
                    {
                        string roleName = role.Name.ToLower();
                        if (roleName.Contains("student") || roleName.Contains("học viên"))
                        {
                            db.StudentProfiles.Add(new StudentProfile { UserId = user.Id });
                            db.SaveChanges();
                        }
                        else if (roleName.Contains("teacher") || roleName.Contains("giáo viên"))
                        {
                            db.TeacherProfiles.Add(new TeacherProfile { UserId = user.Id });
                            db.SaveChanges();
                        }
                    }

                    TempData["SuccessMessage"] = "Tạo tài khoản người dùng thành công!";
                    return RedirectToAction("Index");
                }
            }

            ViewBag.RoleId = new SelectList(db.Roles, "Id", "Name", model.RoleId);
            return View(model);
        }

        #endregion

        #region 4. CẬP NHẬT NGƯỜI DÙNG (EDIT)

        // GET: Users/Edit/5
        public ActionResult Edit(int? id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            User user = db.Users.Find(id);
            if (user == null)
            {
                return HttpNotFound();
            }

            var viewModel = new UserEditViewModel
            {
                Id = user.Id,
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                RoleId = user.RoleId
            };

            PopulateRoleOptions(user.RoleId);
            return View(viewModel);
        }

        // POST: Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(UserEditViewModel model)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            if (ModelState.IsValid)
            {
                var user = db.Users.Find(model.Id);
                if (user == null)
                {
                    return HttpNotFound();
                }

                // Kiểm tra xem Email mới có bị đụng hàng với User khác không
                if (db.Users.Any(u => u.Email == model.Email && u.Id != model.Id))
                {
                    ModelState.AddModelError("Email", "Email này đã được tài khoản khác sử dụng.");
                }

                if (ModelState.IsValid)
                {
                    user.FullName = model.FullName;
                    user.Email = model.Email;
                    user.Phone = model.Phone;
                    user.RoleId = model.RoleId;

                    // Nếu Admin nhập mật khẩu mới -> Mã hóa và cập nhật Mật khẩu
                    if (!string.IsNullOrEmpty(model.NewPassword))
                    {
                        user.PasswordHash = HashPassword(model.NewPassword);
                    }

                    db.Entry(user).State = EntityState.Modified;
                    db.SaveChanges();

                    TempData["SuccessMessage"] = "Cập nhật thông tin người dùng thành công!";
                    return RedirectToAction("Index");
                }
            }

            PopulateRoleOptions(model.RoleId);
            return View(model);
        }

        #endregion

        #region 5. XÓA NGƯỜI DÙNG (DELETE)

        // GET: Users/Delete/5
        public ActionResult Delete(int? id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            User user = db.Users
                .Include(u => u.Role)
                .FirstOrDefault(u => u.Id == id);

            if (user == null)
            {
                return HttpNotFound();
            }

            return View(user);
        }

        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            if (!IsAdmin()) return RedirectToAction("Login", "Auth");

            User user = db.Users.Find(id);
            if (user != null)
            {
                // Xóa Hồ sơ liên quan nếu có trước khi xóa User
                var studentProfile = db.StudentProfiles.FirstOrDefault(sp => sp.UserId == id);
                if (studentProfile != null) db.StudentProfiles.Remove(studentProfile);

                var teacherProfile = db.TeacherProfiles.FirstOrDefault(tp => tp.UserId == id);
                if (teacherProfile != null) db.TeacherProfiles.Remove(teacherProfile);

                db.Users.Remove(user);
                db.SaveChanges();

                TempData["SuccessMessage"] = "Đã xóa tài khoản thành công!";
            }

            return RedirectToAction("Index");
        }

        #endregion

        #region 6. HELPER METHODS
        private void PopulateRoleOptions(int? selectedRoleId = null)
        {
            var roles = db.Roles.ToList();

            ViewBag.RoleId = roles.Select(role =>
            {
                string name = (role.Name ?? "").Trim();
                string displayName = name;
                string normalized = name.ToLowerInvariant();

                if (normalized.Contains("admin") ||
                    normalized.Contains("quản trị"))
                {
                    displayName = "Quản trị";
                }
                else if (normalized.Contains("teacher") ||
                         normalized.Contains("giáo viên"))
                {
                    displayName = "Giáo viên";
                }
                else if (normalized.Contains("student") ||
                         normalized.Contains("học sinh") ||
                         normalized.Contains("học viên"))
                {
                    displayName = "Học sinh";
                }

                return new SelectListItem
                {
                    Value = role.Id.ToString(),
                    Text = displayName,
                    Selected = selectedRoleId.HasValue &&
                                role.Id == selectedRoleId.Value
                };
            }).ToList();
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

        private string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password)) return string.Empty;

            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
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