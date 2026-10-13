using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace English_language_center_management_system.Models
{
    [Table("teacher_profiles")]
    public class TeacherProfile
    {
        [Key, ForeignKey("User")]
        [Column("user_id")]
        public int UserId { get; set; }

        [StringLength(100)]
        [Column("specialization")]
        public string Specialization { get; set; }

        [Column("bio", TypeName = "text")]
        public string Bio { get; set; }

        // Navigation property (1 - 1 với User)
        public virtual User User { get; set; }
    }
}