using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace English_language_center_management_system.Models
{
    [Table("classes")]
    public class Class
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("course_id")]
        public int CourseId { get; set; }

        [Column("teacher_id")]
        public int? TeacherId { get; set; } // Có thể null khi chưa phân công giáo viên

        [Required]
        [StringLength(100)]
        [Column("class_name")]
        public string ClassName { get; set; }

        [Required]
        [StringLength(50)]
        [Column("room")]
        public string Room { get; set; }

        [Required]
        [Column("max_capacity")]
        public int MaxCapacity { get; set; }

        [StringLength(255)]
        [Column("schedule_description")]
        public string ScheduleDescription { get; set; } // Ví dụ: Thứ 2-4-6 (18:00 - 20:00)

        [Required]
        [StringLength(20)]
        [Column("status")]
        public string Status { get; set; } // Open, Full, Closed

        // Navigation properties
        [ForeignKey("CourseId")]
        public virtual Course Course { get; set; }

        [ForeignKey("TeacherId")]
        public virtual User Teacher { get; set; }

        public virtual ICollection<ClassSession> ClassSessions { get; set; }

        public virtual ICollection<Enrollment> Enrollments { get; set; }

        // Thuộc tính bổ sung cho Module 5
        public virtual ICollection<CourseMaterial> CourseMaterials { get; set; }
        public virtual ICollection<Assignment> Assignments { get; set; }
    }
}