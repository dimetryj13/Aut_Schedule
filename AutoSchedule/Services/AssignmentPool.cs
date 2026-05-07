using System.Collections.Generic;
using System.Linq;
using AutoSchedule.Models;

namespace AutoSchedule.Services
{
    // Класс, описывающий одну "карточку", которую мы будем перетаскивать
    public class PoolItem
    {
        public EnrichedAcademicPlan PlanReference { get; set; }

        // 0 - Лекция, 1 - Лабораторная, 2 - Практика (согласно твоей БД)
        public int LessonType { get; set; }

        public Teacher AssignedTeacher { get; set; }
        public int RemainingCount { get; set; }

        public int TotalCount { get; set; }

        public Classroom SelectedRoom { get; set; }

        // Удобное свойство для вывода текста в левой панели (например: "Экономика (Лек)")
        public string DisplayName
        {
            get
            {
                string typeStr = LessonType == 0 ? "Лек" : LessonType == 1 ? "Лаб" : "Прак";
                string subjectName = PlanReference.Subject != null ? PlanReference.Subject.SubjectName : "Неизвестный предмет";
                return $"{subjectName} ({typeStr})";
            }
        }
    }

    // Сам диспетчер нагрузки
    public class AssignmentPool
    {
        private List<PoolItem> _items = new List<PoolItem>();
        public List<PoolItem> GetAllItems() => _items;

        // Метод генерации пула карточек на основе обогащенного плана
        public void Initialize(List<EnrichedAcademicPlan> plans)
        {
            _items.Clear();

            foreach (var plan in plans)
            {
                // Если есть лекции, создаем карточку лекций
                if (plan.LectureInWeek > 0)
                {
                    _items.Add(new PoolItem
                    {
                        PlanReference = plan,
                        LessonType = 0,
                        AssignedTeacher = plan.LectureTeacher,
                        RemainingCount = plan.LectureInWeek,
                        TotalCount = plan.LectureInWeek
                    });
                }

                // Если есть лабораторные, создаем карточку лаб
                if (plan.LabsInWeek > 0)
                {
                    _items.Add(new PoolItem
                    {
                        PlanReference = plan,
                        LessonType = 1,
                        AssignedTeacher = plan.PracticeTeacher, // Для лаб обычно берется препод практики
                        RemainingCount = plan.LabsInWeek,
                        TotalCount = plan.LabsInWeek,
                    });
                }

                // Если есть практики, создаем карточку практик
                if (plan.PracticeInWeek > 0)
                {
                    _items.Add(new PoolItem
                    {
                        PlanReference = plan,
                        LessonType = 2,
                        AssignedTeacher = plan.PracticeTeacher,
                        RemainingCount = plan.PracticeInWeek,
                        TotalCount = plan.PracticeInWeek
                    });
                }
            }
        }

        // Получить список только тех карточек, которые еще нужно распределить
        public List<PoolItem> GetAvailableItems()
        {
            return _items.Where(i => i.RemainingCount > 0).ToList();
        }

        // Получить карточки для конкретной группы (по названию группы)
        public List<PoolItem> GetAvailableItemsForGroup(string groupName)
        {
            // Обращаемся не к GroupID, а к загруженному объекту Group и его свойству GroupName
            return _items.Where(i => i.RemainingCount > 0 &&
                                     i.PlanReference.Group != null &&
                                     i.PlanReference.Group.GroupName == groupName).ToList();
        }

        // Списать одну пару (вызывается при Drop-е в шахматку)
        public void Consume(PoolItem item)
        {
            if (item.RemainingCount > 0)
                item.RemainingCount--;
        }

        // Вернуть одну пару (вызывается, если пользователь удалил пару из расписания)
        public void Restore(PoolItem item)
        {
            item.RemainingCount++;
        }

        public PoolItem GetItemForSchedule(Schedule s)
        {
            // Ищем в пуле карточку, которая совпадает с занятием по предмету, группе и учителю
            return _items.FirstOrDefault(i =>
                i.PlanReference.Subject.SubjectID == s.SubjectID &&
                i.PlanReference.Group.GroupId == s.GroupId &&
                i.AssignedTeacher.TeacherID == s.TeacherID);
        }
    }
}