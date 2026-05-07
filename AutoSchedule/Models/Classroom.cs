using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoSchedule.Models
{
    // Модель для таблицы Classroom (Аудитории)
    public class Classroom
    {
        public int RoomID { get; set; }           // Счетчик (Ключевое поле)
        public string RoomNumber { get; set; }    // Короткий текст (Номер аудитории)
        public int Capacity { get; set; }         // Числовой (Вместимость)
        public bool HasComputers { get; set; }    // Логический (Наличие компьютеров)
    }
}
