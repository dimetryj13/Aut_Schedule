using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoSchedule.Models
{
    // Класс, где вместо ID хранятся прямые ссылки на объекты
    public class EnrichedAcademicPlan
    {
        public int PlanID { get; set; }
        public int Hours { get; set; }
        public int LectureInWeek { get; set; }
        public int LabsInWeek { get; set; }
        public int PracticeInWeek { get; set; }
        public int Semester { get; set; }

        // Ссылки на реальные объекты вместо числовых идентификаторов
        public GroupList Group { get; set; }
        public Subject Subject { get; set; }
        public Teacher LectureTeacher { get; set; }
        public Teacher PracticeTeacher { get; set; }
    }
}
