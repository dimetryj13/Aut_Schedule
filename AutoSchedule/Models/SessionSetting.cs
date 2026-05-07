using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoSchedule.Models
{
    public class SessionSetting
    {
        public int SettingID { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int PreparationDays { get; set; }
        public DateTime ExamStartTime { get; set; }
    }
}
