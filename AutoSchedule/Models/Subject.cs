using System;

namespace AutoSchedule.Models
{
    public class Subject
    {
        public int SubjectID { get; set; }
        public string SubjectName { get; set; }
        public bool RequiresComputers { get; set; }
        public string FixedRoom { get; set; }      // Закрепленная аудитория
        public string ForbiddenRoom { get; set; }  // Запрещенная аудитория
    }
}