using System.Collections.Generic;
using System.Linq;
using AutoSchedule.Models;

namespace AutoSchedule.Services
{
    public class ValidationService
    {
        private List<TeacherDayOff> _daysOff;
        private List<Schedule> _currentSchedule;

        public ValidationService(List<TeacherDayOff> daysOff, List<Schedule> currentSchedule)
        {
            _daysOff = daysOff;
            _currentSchedule = currentSchedule;
        }

        // Метод проверки: подходит ли аудитория для конкретной пары?
        public bool IsRoomValidForLesson(Classroom room, PoolItem lesson, GroupList group)
        {
            if (room == null || lesson == null || group == null) return false;

            // 1. Проверка вместимости (Количество студентов <= Вместимость аудитории)
            if (room.Capacity < group.StudentCount)
                return false;

            // 2. Проверка компьютеров (Если предмету нужны ПК, они должны быть в аудитории)
            if (lesson.PlanReference.Subject != null && lesson.PlanReference.Subject.RequiresComputers)
            {
                if (!room.HasComputers) return false;
            }

            // 3. Проверка "запрещенной" аудитории
            if (lesson.PlanReference.Subject != null && !string.IsNullOrEmpty(lesson.PlanReference.Subject.ForbiddenRoom))
            {
                if (lesson.PlanReference.Subject.ForbiddenRoom == room.RoomNumber)
                    return false;
            }

            // 4. Проверка "закрепленной" аудитории (Если указана, пара может идти ТОЛЬКО в ней)
            if (lesson.PlanReference.Subject != null && !string.IsNullOrEmpty(lesson.PlanReference.Subject.FixedRoom))
            {
                if (lesson.PlanReference.Subject.FixedRoom != room.RoomNumber)
                    return false;
            }

            return true;
        }

        // Метод проверки: доступен ли преподаватель в этот день и эту пару?
        // Метод проверки: доступен ли преподаватель в этот день и эту пару?
        public bool IsTeacherAvailable(Teacher teacher, int dayOfWeek, int pairNumber, int weekType)
        {
            if (teacher == null) return false;

            // 1. Проверяем график выходных преподавателя (TeacherDaysOff)
            var dayOff = _daysOff.FirstOrDefault(d => d.TeacherID == teacher.TeacherID);
            if (dayOff != null)
            {
                bool isWorkingDay = false;

                // Используем классический switch, совместимый с C# 7.3
                switch (dayOfWeek)
                {
                    case 1: isWorkingDay = dayOff.Mon; break;
                    case 2: isWorkingDay = dayOff.Tue; break;
                    case 3: isWorkingDay = dayOff.Wed; break;
                    case 4: isWorkingDay = dayOff.Thu; break;
                    case 5: isWorkingDay = dayOff.Fri; break;
                    case 6: isWorkingDay = dayOff.Sat; break;
                }

                // Если по графику у него выходной, он не доступен
                if (!isWorkingDay) return false;
            }

            // 2. Проверяем накладки в текущем расписании (не ведет ли он уже пару у других)
            bool isBusy = _currentSchedule.Any(s =>
                s.TeacherName == teacher.FullName &&
                s.DayOfWeek == dayOfWeek &&
                s.LessonNumber == pairNumber &&
                s.WeekType == weekType);

            if (isBusy) return false;

            return true;
        }


        public ValidationResult Validate(PoolItem item, int day, int pair, int week, Classroom room)
        {
            // Пока просто возвращаем "ОК", чтобы код запускался. 
            // Настоящую логику проверок напишем на следующем этапе.
            return new ValidationResult { IsValid = true, ErrorMessage = "" };
        }
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; }
    }
}