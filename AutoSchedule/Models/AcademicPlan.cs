using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoSchedule.Models
{
    public class AcademicPlan
    {
        public int PlanID { get; set; }
        public string GroupID { get; set; } // В БД указан текстовый тип
        public int SubjectID { get; set; }
        public int Hours { get; set; }
        public int LectureInWeek { get; set; }
        public int LabsInWeek { get; set; }
        public int PracticeInWeek { get; set; }
        public int FinalControlID { get; set; }
        public int LectureTeacher { get; set; }
        public int PracticeTeacher { get; set; }
        public int Semester { get; set; }
    }
}
