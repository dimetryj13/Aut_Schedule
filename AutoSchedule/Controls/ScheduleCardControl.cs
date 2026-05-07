using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using AutoSchedule.Models;
using AutoSchedule.Services;

namespace AutoSchedule.Controls
{
    public partial class ScheduleCardControl : UserControl
    {
        // Событие обычного клика по карточке
        public event EventHandler CardClicked;

        // Флаг выделения для "Метода 2"
        public bool IsSelectedForPlacement { get; set; } = false;

        // Точка клика для отличия клика от перетаскивания
        private Point _mouseDownLocation;
        public PoolItem ItemData { get; private set; }

        private List<Classroom> _availableRooms = new List<Classroom>();
        private HashSet<int> _priorityRoomIds = new HashSet<int>();

        private int _currentRoomIndex = -1;
        private bool _isHovered = false;
        private ToolTip _toolTip;
        private Color _cardColor;

        public event EventHandler<Classroom> RoomSelectionChanged;

        public ScheduleCardControl(PoolItem item)
        {
            InitializeComponent();
            ItemData = item;

            this.Width = 240;
            this.Height = 60;
            this.DoubleBuffered = true;
            this.Cursor = Cursors.Hand;
            this.BackColor = Color.Transparent;

            // --- ФОРМИРОВАНИЕ ПОЛНОЙ ПОДСКАЗКИ ---
            _toolTip = new ToolTip { AutoPopDelay = 8000, InitialDelay = 400, ReshowDelay = 100 };

            string rawSubjectName = ItemData?.PlanReference?.Subject?.SubjectName ?? "Без названия";
            string groupName = ItemData?.PlanReference?.Group?.GroupName ?? "Неизвестная группа";
            string semesterStr = ItemData?.PlanReference?.Semester.ToString() ?? "?";

            string typeStr = "Неизвестно";
            if (ItemData != null)
            {
                if (ItemData.LessonType == 0) typeStr = "Лекция";
                else if (ItemData.LessonType == 1) typeStr = "Лабораторная";
                else if (ItemData.LessonType == 2) typeStr = "Практика";
            }

            // Итоговый текст во всплывающем окне
            string fullTooltipText = $"{rawSubjectName} ({typeStr})\nГруппа: {groupName}\nСеместр: {semesterStr}";
            _toolTip.SetToolTip(this, fullTooltipText);

            SetCardColor();
            ApplyRoundedCorners(8);

            this.MouseWheel += ScheduleCardControl_MouseWheel;

            // --- ДОПИСАТЬ В КОНЕЦ КОНСТРУКТОРА ---
            // Запоминаем, где нажали
            this.MouseDown += (s, e) => {
                if (e.Button == MouseButtons.Left)
                {
                    _mouseDownLocation = e.Location;
                }
            };

            // Если зажали и потянули мышь (больше чем на 3 пикселя) — это перетаскивание
            this.MouseMove += (s, e) => {
                if (e.Button == MouseButtons.Left)
                {
                    if (Math.Abs(e.X - _mouseDownLocation.X) > 3 || Math.Abs(e.Y - _mouseDownLocation.Y) > 3)
                    {
                        this.ItemData.SelectedRoom = this.SelectedRoom;
                        this.DoDragDrop(this.ItemData, DragDropEffects.Move);
                    }
                }
            };

            // Если просто кликнули и отпустили — это выделение
            this.MouseClick += (s, e) => {
                if (e.Button == MouseButtons.Left)
                {
                    CardClicked?.Invoke(this, EventArgs.Empty);
                }
            };
        }

        private void SetCardColor()
        {
            switch (ItemData.LessonType)
            {
                case 0: _cardColor = Color.LightGreen; break;
                case 1: _cardColor = Color.LightSkyBlue; break;
                case 2: _cardColor = Color.LightCoral; break;
                default: _cardColor = Color.LightGray; break;
            }
        }

        private void ApplyRoundedCorners(int radius)
        {
            GraphicsPath path = new GraphicsPath();
            path.AddArc(0, 0, radius, radius, 180, 90);
            path.AddArc(this.Width - radius, 0, radius, radius, 270, 90);
            path.AddArc(this.Width - radius, this.Height - radius, radius, radius, 0, 90);
            path.AddArc(0, this.Height - radius, radius, radius, 90, 90);
            path.CloseAllFigures();
            this.Region = new Region(path);
        }

        // --- ДАЕМ ДОСТУП К ДАННЫМ ДЛЯ ГЛАВНОЙ ФОРМЫ ---
        public IReadOnlyList<Classroom> AvailableRooms => _availableRooms;
        public HashSet<int> PriorityRoomIds => _priorityRoomIds;
        public Classroom SelectedRoom => (_currentRoomIndex >= 0 && _currentRoomIndex < _availableRooms.Count) ? _availableRooms[_currentRoomIndex] : null;

        public void LoadRooms(List<Classroom> allRooms, List<int> priorityRoomIds)
        {
            _priorityRoomIds = new HashSet<int>(priorityRoomIds);

            // СОРТИРОВКА: Сначала приоритетные, затем по длине названия (чтобы "2" было перед "10"), затем по алфавиту
            _availableRooms = allRooms
                .OrderByDescending(r => _priorityRoomIds.Contains(r.RoomID))
                .ThenBy(r => r.RoomNumber.Length)
                .ThenBy(r => r.RoomNumber)
                .ToList();

            if (_priorityRoomIds.Count > 3) _currentRoomIndex = -2;
            else if (_priorityRoomIds.Count == 0) _currentRoomIndex = -1;
            else if (_availableRooms.Count > 0)
            {
                // Так как приоритетные теперь 100% стоят в самом начале списка (индекс 0), мы просто выбираем первую
                _currentRoomIndex = 0;
            }
            this.Invalidate();
        }
        private void ScheduleCardControl_MouseWheel(object sender, MouseEventArgs e)
        {
            if (_availableRooms.Count == 0) return;

            if (e is HandledMouseEventArgs hme) hme.Handled = true;

            int minIndex = _priorityRoomIds.Count > 3 ? -2 : -1;

            if (e.Delta > 0)
            {
                _currentRoomIndex++;
                if (_currentRoomIndex >= _availableRooms.Count) _currentRoomIndex = minIndex;
            }
            else
            {
                _currentRoomIndex--;
                if (_currentRoomIndex < minIndex) _currentRoomIndex = _availableRooms.Count - 1;
            }

            Classroom selectedRoom = (_currentRoomIndex >= 0 && _currentRoomIndex < _availableRooms.Count)
                ? _availableRooms[_currentRoomIndex] : null;

            RoomSelectionChanged?.Invoke(this, selectedRoom);
            this.Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            using (SolidBrush bgBrush = new SolidBrush(_cardColor))
            {
                e.Graphics.FillRectangle(bgBrush, this.ClientRectangle);
            }

            Rectangle roomRect = new Rectangle(this.Width - 40, 0, 40, this.Height);
            using (SolidBrush roomBgBrush = new SolidBrush(Color.FromArgb(60, 0, 0, 0)))
            {
                e.Graphics.FillRectangle(roomBgBrush, roomRect);
            }

            string roomText = "";
            Brush textBrush = Brushes.Black;

            if (_currentRoomIndex == -2)
            {
                roomText = "...";
                textBrush = Brushes.White;
            }
            else if (_currentRoomIndex == -1)
            {
                roomText = "?";
                textBrush = Brushes.LightGray;
            }
            else if (_currentRoomIndex >= 0 && _currentRoomIndex < _availableRooms.Count)
            {
                var room = _availableRooms[_currentRoomIndex];
                roomText = room.RoomNumber;
                if (_priorityRoomIds.Contains(room.RoomID)) textBrush = Brushes.White;
            }

            Font roomFont = new Font("Segoe UI", 10, FontStyle.Bold);
            StringFormat centerFormat = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            e.Graphics.DrawString(roomText, roomFont, textBrush, roomRect, centerFormat);

            StringFormat textCenterFormat = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
                Trimming = StringTrimming.EllipsisCharacter
            };

            RectangleF subjectRect = new RectangleF(0, 4, this.Width - 40, 32);
            string rawSubjectName = ItemData?.PlanReference?.Subject?.SubjectName ?? "Без названия";
            Font subjectFont = new Font("Segoe UI", 8.5f, FontStyle.Regular);

            SizeF textSize = e.Graphics.MeasureString(rawSubjectName, subjectFont, (int)subjectRect.Width, textCenterFormat);
            string displaySubjectName = rawSubjectName;

            if (textSize.Height > subjectRect.Height)
            {
                string[] words = rawSubjectName.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < words.Length; i++)
                {
                    if (words[i].Length > 5) words[i] = words[i].Substring(0, 4) + ".";
                }
                displaySubjectName = string.Join(" ", words);
            }

            e.Graphics.DrawString(displaySubjectName, subjectFont, Brushes.Black, subjectRect, textCenterFormat);

            // Внутри метода OnPaint, перед отрисовкой преподавателя:
            string countText = $"{ItemData.TotalCount - ItemData.RemainingCount} из {ItemData.TotalCount}";
            Font countFont = new Font("Segoe UI", 7, FontStyle.Bold);
            e.Graphics.DrawString(countText, countFont, Brushes.Gray, new PointF(5, this.Height - 15));

            RectangleF teacherRect = new RectangleF(0, 36, this.Width - 40, 20);
            string teacherText = ItemData?.AssignedTeacher?.FullName ?? "Не назначен";
            Font teacherFont = new Font("Segoe UI", 7.5f, FontStyle.Italic);
            e.Graphics.DrawString(teacherText, teacherFont, Brushes.DarkSlateGray, teacherRect, textCenterFormat);

            if (_isHovered)
            {
                using (Pen hoverPen = new Pen(Color.FromArgb(180, Color.White), 2))
                {
                    e.Graphics.DrawRectangle(hoverPen, 1, 1, this.Width - 2, this.Height - 2);
                }
            }

            // --- ДОБАВИТЬ ЭТО В КОНЕЦ МЕТОДА OnPaint ---
            if (IsSelectedForPlacement)
            {
                using (Pen selectedPen = new Pen(Color.Red, 3))
                {
                    e.Graphics.DrawRectangle(selectedPen, 1, 1, this.Width - 2, this.Height - 2);
                }
            }
            else if (_isHovered)
            {
                using (Pen hoverPen = new Pen(Color.FromArgb(180, Color.White), 2))
                {
                    e.Graphics.DrawRectangle(hoverPen, 1, 1, this.Width - 2, this.Height - 2);
                }
            }
        }

        // --- МАГИЯ ИСПРАВЛЕНИЯ БАГА С ПРЫЖКОМ И ИСЧЕЗНОВЕНИЕМ СКРОЛЛА ---
        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _isHovered = true;

            // Запоминаем текущую позицию скролла перед тем, как взять фокус
            var parentPanel = this.Parent as FlowLayoutPanel;
            Point scrollPos = parentPanel != null ? parentPanel.AutoScrollPosition : Point.Empty;

            this.Focus();

            // Возвращаем скролл на место (WinForms требует положительных координат)
            if (parentPanel != null)
            {
                parentPanel.AutoScrollPosition = new Point(Math.Abs(scrollPos.X), Math.Abs(scrollPos.Y));
            }

            this.Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _isHovered = false;

            // ВАЖНО: Мы отдаем фокус самой ФОРМЕ, а не панели. 
            // Это решает баг с "выпаданием" интерфейса в серый экран при прокрутке!
            this.FindForm()?.Focus();

            this.Invalidate();
        }

        private void ScheduleCardControl_Load(object sender, EventArgs e)
        {

        }
    }
}