using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoSchedule.Models
{
    public class TeacherAvailability
    {
        public int TeacherAvailabilityId { get; set; }           // Поле "Код" в БД
        public int TeacherID { get; set; }
        public string DayIdx { get; set; }
        public string PairIdx { get; set; }
        public bool IsAvailable { get; set; }
    }
}
