
using System;
using System.Collections.Generic;
using English_language_center_management_system.Models;

namespace English_language_center_management_system.Models.ViewModels.Home
{
    public class StudentTimetableViewModel
    {
        public User StudentInfo { get; set; }

        public DateTime WeekStart { get; set; }

        public DateTime WeekEnd { get; set; }

        public int WeekOffset { get; set; }

        public List<ClassSession> Sessions { get; set; }
    }
}
