using System;

namespace AutoSchedule.Models
{
    public class GroupList
    {
        public int GroupId { get; set; }
        public string GroupName { get; set; }
        public int StudentCount { get; set; }
        public bool IsFullTime { get; set; }
        public bool Actually { get; set; }
        public int MainTeacher { get; set; } // Куратор
        public int YearLearn { get; set; }   // Год начала обучения
    }
}