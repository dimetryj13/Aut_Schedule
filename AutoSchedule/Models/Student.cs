using System;

namespace AutoSchedule.Models
{
    public class Student
    {
        public int StudentID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Patronymic { get; set; }
        public int GroupID { get; set; }
        public int MainTeacher { get; set; }
        public int YearLearn { get; set; }
    }
}