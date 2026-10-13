using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace English_language_center_management_system.Models.ViewModels.Classes
{
    public class ClassEditViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn khóa học")]
        [Display(Name = "Khóa học")]
        public int CourseId { get; set; }

        [Display(Name = "Giáo viên phụ trách")]
        public int? TeacherId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên lớp học")]
        [Display(Name = "Tên lớp học")]
        public string ClassName { get; set; }

        [Display(Name = "Phòng học")]
        public string Room { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập sĩ số tối đa")]
        [Range(1, 200, ErrorMessage = "Sĩ số phải từ 1 đến 200 học viên")]
        [Display(Name = "Sĩ số tối đa")]
        public int MaxCapacity { get; set; }

        [Display(Name = "Mô tả lịch học")]
        public string ScheduleDescription { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn trạng thái")]
        [Display(Name = "Trạng thái")]
        public string Status { get; set; }
    }
}