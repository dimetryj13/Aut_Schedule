using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoSchedule.Models
{
    public class SessionSchedule
    {
        public int ExamID { get; set; }
        public DateTime ExamDate { get; set; }
        public DateTime ExamTime { get; set; }
        public int GroupID { get; set; }
        public int SubjectID { get; set; }
        public int TeacherID { get; set; }
        public int RoomID { get; set; }
        public int ControlTypeID { get; set; }
    }
}
