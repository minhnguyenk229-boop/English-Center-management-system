using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace English_language_center_management_system.Models
{
    [Table("class_sessions")]
    public class ClassSession
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("class_id")]
        public int ClassId { get; set; }

        [Required]
        [Column("session_date", TypeName = "date")]
        public DateTime SessionDate { get; set; }

        [Required]
        [Column("start_time")]
        public TimeSpan StartTime { get; set; } // Kiểu TimeSpan tương ứng với TIME trong SQL

        [Required]
        [Column("end_time")]
        public TimeSpan EndTime { get; set; }

        [StringLength(50)]
        [Column("room")]
        public string Room { get; set; } // Phòng học (nếu thay đổi so với phòng mặc định của lớp)

        // Navigation property
        [ForeignKey("ClassId")]
        public virtual Class Class { get; set; }
        public virtual ICollection<Attendance> Attendances { get; set; }
    }
}