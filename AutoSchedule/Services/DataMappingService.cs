using AutoSchedule.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoSchedule.Services
{
    public class DataMappingService
    {
        private Dictionary<string, GroupList> _groupsDict;
        private Dictionary<int, Subject> _subjectsDict;
        private Dictionary<int, Teacher> _teachersDict;

        // Сохраняем полный список для поиска по имени
        private List<GroupList> _allGroups;

        public DataMappingService(List<GroupList> groups, List<Subject> subjects, List<Teacher> teachers)
        {
            _allGroups = groups;

            // Trim() обрезает невидимые пробелы, которые Access часто добавляет к тексту
            _groupsDict = groups.ToDictionary(g => g.GroupId.ToString().Trim(), g => g);
            _subjectsDict = subjects.ToDictionary(s => s.SubjectID, s => s);
            _teachersDict = teachers.ToDictionary(t => t.TeacherID, t => t);
        }

        public List<EnrichedAcademicPlan> MapAcademicPlans(List<AcademicPlan> rawPlans)
        {
            List<EnrichedAcademicPlan> enrichedPlans = new List<EnrichedAcademicPlan>();

            foreach (var raw in rawPlans)
            {
                var enriched = new EnrichedAcademicPlan
                {
                    PlanID = raw.PlanID,
                    Hours = raw.Hours,
                    LectureInWeek = raw.LectureInWeek,
                    LabsInWeek = raw.LabsInWeek,
                    PracticeInWeek = raw.PracticeInWeek,
                    Semester = raw.Semester
                };

                // --- БРОНЕБОЙНАЯ ПРИВЯЗКА ГРУППЫ ---
                if (!string.IsNullOrEmpty(raw.GroupID))
                {
                    string rawId = raw.GroupID.Trim(); // Убираем возможные пробелы из БД

                    // Попытка 1: Ищем по классическому ID
                    if (_groupsDict.ContainsKey(rawId))
                    {
                        enriched.Group = _groupsDict[rawId];
                    }
                    // Попытка 2: Если в БД в поле GroupID записано НАЗВАНИЕ (например, "КР-121")
                    else
                    {
                        var groupByName = _allGroups.FirstOrDefault(g =>
                            string.Equals(g.GroupName?.Trim(), rawId, StringComparison.OrdinalIgnoreCase));

                        if (groupByName != null)
                            enriched.Group = groupByName;
                    }
                }

                // Безопасно привязываем предмет
                if (_subjectsDict.ContainsKey(raw.SubjectID))
                    enriched.Subject = _subjectsDict[raw.SubjectID];

                // Безопасно привязываем преподавателей
                if (_teachersDict.ContainsKey(raw.LectureTeacher))
                    enriched.LectureTeacher = _teachersDict[raw.LectureTeacher];

                if (_teachersDict.ContainsKey(raw.PracticeTeacher))
                    enriched.PracticeTeacher = _teachersDict[raw.PracticeTeacher];

                enrichedPlans.Add(enriched);
            }

            return enrichedPlans;
        }
    }
}