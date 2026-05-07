using System;

namespace AutoSchedule.Models
{
    public class Schedule
    {
        public int ScheduleID { get; set; }

        // --- ДОБАВЛЕНО: ID группы для связи ---
        public int GroupId { get; set; }

        public int SubjectID { get; set; }
        public int TeacherID { get; set; }
        public int RoomID { get; set; }
        public int DayOfWeek { get; set; }

        // --- ПЕРЕИМЕНОВАНО: Из PairNumber в LessonNumber ---
        public int LessonNumber { get; set; }

        public int WeekType { get; set; }

        // Поля для отображения имен (уже были в вашем коде)
        public string GroupName { get; set; }
        public string SubjectName { get; set; }
        public string TeacherName { get; set; }
        public string RoomNumber { get; set; }

        public int LessonType { get; set; }
    }
}