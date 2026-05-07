using System;

namespace AutoSchedule.Models
{
    public class TeacherRoomPref
    {
        public int TeacherRoomPrefId { get; set; }
        public int TeacherID { get; set; }
        public int RoomID { get; set; }
        public int Priority { get; set; } // ВЕРНУЛИ int, так как в базе это число 0-3
    }
}