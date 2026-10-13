using System.ComponentModel.DataAnnotations;

namespace English_language_center_management_system.Models.ViewModels.Home
{
    public class TeacherProfileViewModel
    {
        public string Username { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ và tên.")]
        [StringLength(100, ErrorMessage = "Họ tên tối đa 100 ký tự.")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập email.")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
        [StringLength(100, ErrorMessage = "Email tối đa 100 ký tự.")]
        [Display(Name = "Địa chỉ email")]
        public string Email { get; set; }

        [StringLength(20, ErrorMessage = "Số điện thoại tối đa 20 ký tự.")]
        [Display(Name = "Số điện thoại")]
        public string Phone { get; set; }

        [StringLength(100, ErrorMessage = "Chuyên môn tối đa 100 ký tự.")]
        [Display(Name = "Chuyên môn giảng dạy")]
        public string Specialization { get; set; }

        [StringLength(4000, ErrorMessage = "Phần giới thiệu tối đa 4000 ký tự.")]
        [Display(Name = "Giới thiệu bản thân")]
        public string Bio { get; set; }
    }
}