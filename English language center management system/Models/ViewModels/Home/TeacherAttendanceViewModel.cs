
using System.Collections.Generic;
using English_language_center_management_system.Models;

namespace English_language_center_management_system.Models.ViewModels.Home
{
    public class TeacherAttendanceViewModel
    {
        public User TeacherInfo { get; set; }

        public List<ClassSession> AvailableSessions { get; set; }

        public int? SelectedSessionId { get; set; }

        public ClassSession SelectedSession { get; set; }

        public List<TeacherAttendanceRowViewModel> Students { get; set; }
    }

    public class TeacherAttendanceRowViewModel
    {
        public int StudentId { get; set; }

        public string FullName { get; set; }

        public string Username { get; set; }

        public string Status { get; set; }

        public string Notes { get; set; }
    }
}
