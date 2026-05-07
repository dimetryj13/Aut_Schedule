using System;
using System.Collections.Generic;
using System.Data.OleDb;
using AutoSchedule.Models;

namespace AutoSchedule
{
    public class DatabaseManager
    {
        private string connectionString;

        public DatabaseManager(string dbPath)
        {
            connectionString = $@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={dbPath};";
        }

        // --- БЕЗОПАСНЫЕ МЕТОДЫ ЧТЕНИЯ ---
        private int SafeGetInt(object value)
        {
            if (value == DBNull.Value) return 0;
            return Convert.ToInt32(value);
        }

        private string SafeGetString(object value)
        {
            if (value == DBNull.Value) return string.Empty;
            return value.ToString();
        }

        private bool SafeGetBool(object value)
        {
            if (value == DBNull.Value) return false;
            return Convert.ToBoolean(value);
        }

        private DateTime SafeGetDateTime(object value)
        {
            if (value == DBNull.Value) return DateTime.MinValue;
            return Convert.ToDateTime(value);
        }
        // --------------------------------

        public List<Classroom> GetClassrooms()
        {
            List<Classroom> classrooms = new List<Classroom>();
            using (OleDbConnection connection = new OleDbConnection(connectionString))
            {
                string query = "SELECT * FROM Classroom";
                OleDbCommand command = new OleDbCommand(query, connection);
                connection.Open();
                using (OleDbDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        classrooms.Add(new Classroom
                        {
                            RoomID = SafeGetInt(reader["RoomID"]),
                            RoomNumber = SafeGetString(reader["RoomNumber"]),
                            Capacity = SafeGetInt(reader["Capacity"]),
                            HasComputers = SafeGetBool(reader["HasComputers"])
                        });
                    }
                }
            }
            return classrooms;
        }

        public List<GroupList> GetGroups()
        {
            List<GroupList> groups = new List<GroupList>();
            using (OleDbConnection connection = new OleDbConnection(connectionString))
            {
                string query = "SELECT * FROM GroupsList";
                OleDbCommand command = new OleDbCommand(query, connection);
                connection.Open();
                using (OleDbDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        groups.Add(new GroupList
                        {
                            // Теперь читаем GroupID вместо "Код"
                            GroupId = SafeGetInt(reader["GroupID"]),
                            GroupName = SafeGetString(reader["GroupName"]),
                            StudentCount = SafeGetInt(reader["StudentCount"]),
                            IsFullTime = SafeGetBool(reader["IsFullTime"]),
                            Actually = SafeGetBool(reader["Actually"]),
                            MainTeacher = SafeGetInt(reader["MainTeacher"]),
                            YearLearn = SafeGetInt(reader["YearLearn"])
                        });
                    }
                }
            }
            return groups;
        }

        public List<TeacherDayOff> GetTeacherDaysOff()
        {
            List<TeacherDayOff> daysOff = new List<TeacherDayOff>();
            using (OleDbConnection connection = new OleDbConnection(connectionString))
            {
                string query = "SELECT * FROM TeacherDaysOff";
                OleDbCommand command = new OleDbCommand(query, connection);
                connection.Open();
                using (OleDbDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        daysOff.Add(new TeacherDayOff
                        {
                            // Читаем Id и английские названия дней
                            TeacherDayOffId = SafeGetInt(reader["TeacherDayOffId"]),
                            TeacherID = SafeGetInt(reader["TeacherName"]),
                            Mon = SafeGetBool(reader["Mon"]),
                            Tue = SafeGetBool(reader["Tue"]),
                            Wed = SafeGetBool(reader["Wed"]),
                            Thu = SafeGetBool(reader["Thu"]),
                            Fri = SafeGetBool(reader["Fri"]),
                            Sat = SafeGetBool(reader["Sat"])
                        });
                    }
                }
            }
            return daysOff;
        }

        public List<TeacherRoomPref> GetTeacherRoomPrefs()
        {
            List<TeacherRoomPref> prefs = new List<TeacherRoomPref>();
            using (OleDbConnection connection = new OleDbConnection(connectionString))
            {
                string query = "SELECT * FROM TeacherRoomPrefs";
                OleDbCommand command = new OleDbCommand(query, connection);
                connection.Open();
                using (OleDbDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        prefs.Add(new TeacherRoomPref
                        {
                            TeacherRoomPrefId = SafeGetInt(reader["TeacherRoomPrefId"]),
                            TeacherID = SafeGetInt(reader["TeacherID"]),
                            RoomID = SafeGetInt(reader["RoomNumber"]),
                            // Снова используем SafeGetInt для приоритета
                            Priority = SafeGetInt(reader["Priority"])
                        });
                    }
                }
            }
            return prefs;
        }

        public List<TeacherAvailability> GetTeacherAvailability()
        {
            List<TeacherAvailability> availabilities = new List<TeacherAvailability>();
            using (OleDbConnection connection = new OleDbConnection(connectionString))
            {
                string query = "SELECT * FROM TeacherAvailability";
                OleDbCommand command = new OleDbCommand(query, connection);
                connection.Open();
                using (OleDbDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        availabilities.Add(new TeacherAvailability
                        {
                            // Читаем Id вместо "Код"
                            TeacherAvailabilityId = SafeGetInt(reader["TeacherAvailabilityId"]),
                            TeacherID = SafeGetInt(reader["TeacherID"]),
                            DayIdx = SafeGetString(reader["DayIdx"]),
                            PairIdx = SafeGetString(reader["PairIdx"]),
                            IsAvailable = SafeGetBool(reader["IsAvailable"])
                        });
                    }
                }
            }
            return availabilities;
        }

        public List<Subject> GetSubjects()
        {
            List<Subject> subjects = new List<Subject>();
            using (OleDbConnection connection = new OleDbConnection(connectionString))
            {
                string query = "SELECT * FROM Subjects";
                OleDbCommand command = new OleDbCommand(query, connection);
                connection.Open();
                using (OleDbDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        subjects.Add(new Subject
                        {
                            SubjectID = SafeGetInt(reader["SubjectID"]),
                            SubjectName = SafeGetString(reader["SubjectName"]),
                            RequiresComputers = SafeGetBool(reader["RequiresComputers"]),
                            FixedRoom = SafeGetString(reader["FixedRoom"]),
                            ForbiddenRoom = SafeGetString(reader["ForbiddenRoom"])
                        });
                    }
                }
            }
            return subjects;
        }

        public List<Teacher> GetTeachers()
        {
            List<Teacher> teachers = new List<Teacher>();
            using (OleDbConnection connection = new OleDbConnection(connectionString))
            {
                string query = "SELECT * FROM Teachers";
                OleDbCommand command = new OleDbCommand(query, connection);
                connection.Open();
                using (OleDbDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        teachers.Add(new Teacher
                        {
                            TeacherID = SafeGetInt(reader["TeacherID"]),
                            FullName = SafeGetString(reader["FullName"]),
                            Department = SafeGetString(reader["Department"]),
                            MaxLectureGroups = SafeGetInt(reader["MaxLectureGroups"]),
                            MaxPracticeGroups = SafeGetInt(reader["MaxPracticeGroups"])
                        });
                    }
                }
            }
            return teachers;
        }
        public List<AcademicPlan> GetAcademicPlans()
        {
            List<AcademicPlan> plans = new List<AcademicPlan>();
            using (OleDbConnection connection = new OleDbConnection(connectionString))
            {
                string query = "SELECT * FROM AcademicPlan";
                OleDbCommand command = new OleDbCommand(query, connection);
                connection.Open();
                using (OleDbDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        plans.Add(new AcademicPlan
                        {
                            PlanID = SafeGetInt(reader["PlanID"]),
                            GroupID = SafeGetString(reader["GroupID"]),
                            SubjectID = SafeGetInt(reader["SubjectID"]),
                            Hours = SafeGetInt(reader["Hours"]),
                            LectureInWeek = SafeGetInt(reader["LectureInWeek"]),
                            LabsInWeek = SafeGetInt(reader["LabsInWeek"]),
                            PracticeInWeek = SafeGetInt(reader["PracticeInWeek"]),
                            FinalControlID = SafeGetInt(reader["FinalControlID"]),
                            LectureTeacher = SafeGetInt(reader["LectureTeacher"]),
                            PracticeTeacher = SafeGetInt(reader["PracticeTeacher"]),
                            Semester = SafeGetInt(reader["Semester"]),
                        });
                    }
                }
            }
            return plans;
        }

        public List<Schedule> GetSchedules()
        {
            List<Schedule> schedules = new List<Schedule>();
            using (OleDbConnection connection = new OleDbConnection(connectionString))
            {
                string query = "SELECT * FROM Schedule";
                OleDbCommand command = new OleDbCommand(query, connection);
                connection.Open();
                using (OleDbDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        schedules.Add(new Schedule
                        {
                            ScheduleID = SafeGetInt(reader["ScheduleID"]),
                            GroupName = SafeGetString(reader["GroupName"]),
                            DayOfWeek = SafeGetInt(reader["DayOfWeek"]),
                            LessonNumber = SafeGetInt(reader["PairNumber"]),
                            WeekType = SafeGetInt(reader["WeekType"]),
                            SubjectName = SafeGetString(reader["SubjectName"]),
                            TeacherName = SafeGetString(reader["TeacherName"]),
                            RoomNumber = SafeGetString(reader["RoomNumber"]),
                            LessonType = SafeGetInt(reader["LessonType"])
                        });
                    }
                }
            }
            return schedules;
        }


        public List<SessionSetting> GetSessionSettings()
        {
            List<SessionSetting> settings = new List<SessionSetting>();
            using (OleDbConnection connection = new OleDbConnection(connectionString))
            {
                string query = "SELECT * FROM SessionSettings";
                OleDbCommand command = new OleDbCommand(query, connection);
                connection.Open();
                using (OleDbDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        settings.Add(new SessionSetting
                        {
                            SettingID = SafeGetInt(reader["SettingID"]),
                            StartDate = SafeGetDateTime(reader["StartDate"]),
                            EndDate = SafeGetDateTime(reader["EndDate"]),
                            PreparationDays = SafeGetInt(reader["PreparationDays"]),
                            ExamStartTime = SafeGetDateTime(reader["ExamStartTime"])
                        });
                    }
                }
            }
            return settings;
        }

        public List<SessionSchedule> GetSessionSchedules()
        {
            List<SessionSchedule> sessionSchedules = new List<SessionSchedule>();
            using (OleDbConnection connection = new OleDbConnection(connectionString))
            {
                string query = "SELECT * FROM SessionSchedule";
                OleDbCommand command = new OleDbCommand(query, connection);
                connection.Open();
                using (OleDbDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        sessionSchedules.Add(new SessionSchedule
                        {
                            ExamID = SafeGetInt(reader["ExamID"]),
                            ExamDate = SafeGetDateTime(reader["ExamDate"]),
                            ExamTime = SafeGetDateTime(reader["ExamTime"]),
                            GroupID = SafeGetInt(reader["GroupID"]),
                            SubjectID = SafeGetInt(reader["SubjectID"]),
                            TeacherID = SafeGetInt(reader["TeacherID"]),
                            RoomID = SafeGetInt(reader["RoomID"]),
                            ControlTypeID = SafeGetInt(reader["ControlTypeID"])
                        });
                    }
                }
            }
            return sessionSchedules;
        }


    }
}