using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace English_language_center_management_system.Models.ViewModels.Courses
{
    public class CourseAdminItemViewModel
    {
        public Course Course { get; set; }
        public int TotalClassesCount { get; set; }      // Tổng số lớp thuộc khóa học
        public int ActiveClassesCount { get; set; }     // Số lớp đang mở / đang học
        public int TotalStudentsCount { get; set; }     // Tổng số học viên đăng ký
    }

    public class CourseAdminIndexViewModel
    {
        public List<CourseAdminItemViewModel> Courses { get; set; }
        public string SearchTerm { get; set; }
        public string SelectedLevel { get; set; }
        public string SelectedStatus { get; set; }      // Tất cả / Active (Hiển thị) / Hidden (Đã ẩn)
    }
}