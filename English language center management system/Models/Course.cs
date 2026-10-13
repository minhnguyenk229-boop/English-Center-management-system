using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace English_language_center_management_system.Models
{
    [Table("courses")]
    public class Course
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        [Column("course_name")]
        public string CourseName { get; set; }

        [Required]
        [StringLength(50)]
        [Column("level")]
        public string Level { get; set; } // Beginner, Elementary, Intermediate, Upper Intermediate, Advanced, IELTS/TOEIC preparation

        [Column("description")]
        public string Description { get; set; }

        [Required]
        [Column("duration")]
        public int Duration { get; set; } // Số giờ học

        [Required]
        [Column("tuition_fee")]
        public decimal TuitionFee { get; set; }

        [Column("syllabus")]
        public string Syllabus { get; set; }

        // Navigation properties
        public virtual ICollection<Class> Classes { get; set; }
    }
}