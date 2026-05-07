using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoSchedule.Models
{
    public class TeacherDayOff
    {
        public int TeacherDayOffId { get; set; } // В БД поле «Код»
        public int TeacherID { get; set; } // В БД поле «TeacherName» с числовым типом
        public bool Mon { get; set; }
        public bool Tue { get; set; }
        public bool Wed { get; set; }
        public bool Thu { get; set; }
        public bool Fri { get; set; }
        public bool Sat { get; set; }
    }
}
