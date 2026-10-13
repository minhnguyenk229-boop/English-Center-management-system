using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace English_language_center_management_system.Models.ViewModels.Enrollments
{
    public class EnrollmentCreateViewModel
    {
        [Required(ErrorMessage = "Vui lòng chọn học viên")]
        [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn học viên hợp lệ")]
        [Display(Name = "Học viên")]
        public int StudentId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn lớp học")]
        [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn lớp học hợp lệ")]
        [Display(Name = "Lớp học")]
        public int ClassId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn trạng thái")]
        [Display(Name = "Trạng thái ghi danh")]
        public string Status { get; set; } = "Approved";

        [Display(Name = "Ngày ghi danh")]
        public DateTime EnrollmentDate { get; set; } = DateTime.Now;
    }
}