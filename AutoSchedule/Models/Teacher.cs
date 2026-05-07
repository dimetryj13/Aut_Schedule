using System;

namespace AutoSchedule.Models
{
    public class Teacher
    {
        public int TeacherID { get; set; }
        public string FullName { get; set; }
        public string Department { get; set; }
        public int MaxLectureGroups { get; set; }  // Макс. групп на лекции
        public int MaxPracticeGroups { get; set; } // Макс. групп на практике
    }
}