using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using AutoSchedule.Models;
using AutoSchedule.Services;

namespace AutoSchedule
{
    public partial class Form1 : Form
    {
        public string selectedPath = null;
        // Переменная для хранения выбранной аудитории по клику
        private Classroom _activeRoomForPlacement = null;
        // Переменная для "Метода 2" (Клик-Вставка)
        private PoolItem _activeCardForPlacement = null;
        // --- ПОЛЯ ДАННЫХ ---
        string connectionString = "";
        string dbFolderPath = Path.Combine(Application.StartupPath, "Databases");

        // Переменные для визуального Drag and Drop
        private PoolItem _draggedItem = null;
        private Classroom _draggedRoom = null;
        private Point _dragPosition = Point.Empty;

        // Хранилища данных
        List<Classroom> classrooms = new List<Classroom>();
        List<GroupList> groups = new List<GroupList>();
        List<Subject> subjects = new List<Subject>();
        List<Teacher> teachers = new List<Teacher>();
        List<AcademicPlan> academicPlans = new List<AcademicPlan>();
        List<TeacherDayOff> teacherDaysOff = new List<TeacherDayOff>();
        List<Schedule> schedules = new List<Schedule>();
        List<TeacherRoomPref> teacherRoomPrefs = new List<TeacherRoomPref>();
        List<EnrichedAcademicPlan> enrichedPlans = new List<EnrichedAcademicPlan>();
        // Словарь для связи ID аудитории с её визуальным индикатором
        private Dictionary<int, Controls.RoomIndicatorControl> _roomIndicators = new Dictionary<int, Controls.RoomIndicatorControl>();

        // Сервисы
        AssignmentPool globalPool = new AssignmentPool();
        ValidationService globalValidator;
        // Переменная для самой шахматки
        private DataGridView dgvSchedule;

        // --- ПЕРЕМЕННЫЕ МАСШТАБИРОВАНИЯ ---
        private float _zoomLevel = 1.0f;
        private const float ZOOM_STEP = 0.1f;
        private const float MIN_ZOOM = 0.5f;
        private const float MAX_ZOOM = 2.0f;

        // Базовые размеры (эталонные)
        private const int BASE_ROW_HEIGHT = 33; // 33px на каждую неделю (в сумме пара = 66px)
        private const int BASE_HEADER_HEIGHT = 40;
        private const float BASE_FONT_SIZE_DAY = 12f;
        private const float BASE_FONT_SIZE_PAIR = 12f;
        private const int BASE_WIDTH_DAY = 40;
        private const int BASE_WIDTH_PAIR = 40;
        private const int BASE_WIDTH_SUBJ = 200; // Колонка дисциплины
        private const int BASE_WIDTH_ROOM = 50;  // Новая колонка аудитории

        // Кисти для рисования границ ячеек
        private Pen _gridPen = new Pen(Color.LightGray, 1);
        private Pen _thickPen = new Pen(Color.Black, 2);

        // Переменная для хранения текущей выбранной группы
        private Models.GroupList _selectedGroup = null;

        // Состояние выбора
        private int _selectedSemester = 0; // 0 - не выбран
        public Form1()
        {
            InitializeComponent();
            SetDoubleBuffered(flpAssignments);
            SetDoubleBuffered(flpRoomIndicators);
            SetDoubleBuffered(pnlScheduleContainer);
            // Включаем двойную буферизацию для плавной отрисовки
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer, true);
            
           
        
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Устанавливаем значения по умолчанию
            lblGroupSelect.Text = "Группа: не выбрано";
            lblSemesterSelect.Text = "Семестр: не выбрано";
            // Создаем папку БД, если её нет
            if (!Directory.Exists(dbFolderPath)) Directory.CreateDirectory(dbFolderPath);

            // Пока мы не сделали новые кнопки в Toolbar, можно временно вызвать загрузку 
            // какой-то конкретной базы для теста, если она есть в папке:
            // string testDb = Path.Combine(dbFolderPath, "University.accdb");
            // if (File.Exists(testDb)) LoadDatabaseData(testDb);
        }

        public void LoadDatabaseData(string dbPath)
        {
            try
            {
                DatabaseManager dbManager = new DatabaseManager(dbPath);

                // 1. Загрузка из БД
                classrooms = dbManager.GetClassrooms();
                groups = dbManager.GetGroups();
                subjects = dbManager.GetSubjects();
                teachers = dbManager.GetTeachers();
                academicPlans = dbManager.GetAcademicPlans();
                teacherDaysOff = dbManager.GetTeacherDaysOff();
                schedules = dbManager.GetSchedules();
                teacherRoomPrefs = dbManager.GetTeacherRoomPrefs();

                // 2. Связывание (Mapping)
                DataMappingService mappingService = new DataMappingService(groups, subjects, teachers);
                enrichedPlans = mappingService.MapAcademicPlans(academicPlans);

                // 3. Инициализация пула карточек
                globalPool.Initialize(enrichedPlans);

                // 4. Инициализация валидатора
                globalValidator = new ValidationService(teacherDaysOff, schedules);

                // 5. ВИЗУАЛИЗАЦИЯ: Выводим карточки в левую панель
                PopulateAssignmentCards();

                int totalCards = globalPool.GetAvailableItems().Count;

                // Генерируем сетку
                GenerateScheduleGrid();

                // --- ВИЗУАЛИЗАЦИЯ: Выводим индикаторы аудиторий вниз ---
                MessageBox.Show($"База загружена. Сгенерировано карточек: {totalCards}");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке: " + ex.Message);
            }
        }

        // --- НОВЫЙ МЕТОД: Отрисовка карточек в левой панели ---
        // --- НОВЫЙ МЕТОД: Отрисовка карточек с прогресс-баром ---
        private void PopulateAssignmentCards()
        {
            flpAssignments.Visible = false;
            flpAssignments.SuspendLayout();
            flpAssignments.Controls.Clear();

            if (_selectedGroup == null || _selectedSemester == 0)
            {
                flpRoomIndicators.Visible = false;
                string hint = _selectedGroup == null ? "группу" : "семестр";
                Label lblPlaceholder = new Label
                {
                    Text = $"Выберите {hint} на панели управления,\nчтобы отобразить карточки.",
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font("Segoe UI", 9, FontStyle.Italic),
                    ForeColor = Color.Gray
                };
                flpAssignments.Controls.Add(lblPlaceholder);
                flpAssignments.ResumeLayout();
                flpAssignments.Visible = true;
                return;
            }



            // ПРОВЕРКА: Выбраны ли оба параметра?
            if (_selectedGroup == null || _selectedSemester == 0)
            {
                Label lblPlaceholder = new Label
                {
                    Text = "Выберите группу и семестр\nна панели управления.",
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font("Segoe UI", 9, FontStyle.Italic),
                    ForeColor = Color.Gray
                };
                flpAssignments.Controls.Add(lblPlaceholder);
                flpAssignments.ResumeLayout();
                flpAssignments.Visible = true;
                return;
            }

            // ЕСЛИ ГРУППА ВЫБРАНА
            flpRoomIndicators.Visible = true; // <--- ПОКАЗЫВАЕМ ПАНЕЛЬ АУДИТОРИЙ

            // Если кнопки еще не загружены - загружаем
            if (_roomIndicators.Count == 0) LoadAllRoomsToPanel();

            if (globalPool == null) return;
            var allItems = globalPool.GetAvailableItems();

            // --- УМНАЯ ФИЛЬТРАЦИЯ ---
            var availableItems = allItems
                // 1. Совпадение по группе
                .Where(i => i != null &&
                       string.Equals(i.PlanReference?.Group?.GroupName?.Trim(), _selectedGroup.GroupName?.Trim(), StringComparison.OrdinalIgnoreCase))
                // 2. Проверка семестра на четность
                .Where(i =>
                {
                    int dbSem = i.PlanReference.Semester;

                    // Если выбрали Осенний (_selectedSemester == 1), пропускаем нечетные (1, 3, 5, 7)
                    if (_selectedSemester == 1) return dbSem % 2 != 0;

                    // Если выбрали Весенний (_selectedSemester == 2), пропускаем четные (2, 4, 6, 8)
                    if (_selectedSemester == 2) return dbSem % 2 == 0;

                    return false;
                })
                .ToList();
            if (availableItems.Count > 0)
            {
                foreach (var item in availableItems)
                {
                    var card = new Controls.ScheduleCardControl(item);

                    List<int> priorityRoomIds = new List<int>();
                    if (teacherRoomPrefs != null)
                        priorityRoomIds = teacherRoomPrefs.Where(p => p != null && p.TeacherID == item.AssignedTeacher?.TeacherID).Select(p => p.RoomID).ToList();

                    card.LoadRooms(classrooms, priorityRoomIds);
                    card.MouseEnter += (s, e) => { ShowRoomsForCard(card); };
                   //card.MouseLeave += (s, e) => { ClearRoomIndicators(); };
                    card.RoomSelectionChanged += (s, room) => UpdateRoomHighlight(room);

                    // --- ДОБАВИТЬ ЛОГИКУ ВЫДЕЛЕНИЯ ---
                    // Внутри цикла foreach (var item in availableItems)
                    card.CardClicked += (s, args) => {
                        // 1. ЛОГИКА СБРОСА ВЫДЕЛЕНИЯ АУДИТОРИЙ (Тот самый совет)
                        if (_activeRoomForPlacement != null)
                        {
                            _activeRoomForPlacement = null; // Обнуляем выбранную комнату

                            // Проходим по всем кнопкам в панели аудиторий и снимаем красные рамки
                            foreach (Control control in flpRoomIndicators.Controls)
                            {
                                if (control is Controls.RoomIndicatorControl indicator)
                                {
                                    indicator.IsSelectedForPlacement = false;
                                    indicator.Invalidate(); // Принудительно перерисовываем, чтобы рамка исчезла
                                }
                            }
                        }

                        // 2. СТАНДАРТНАЯ ЛОГИКА ВЫБОРА КАРТОЧКИ (Метод 2)
                        if (_activeCardForPlacement == card.ItemData)
                        {
                            // Если кликнули на уже выделенную карточку — снимаем выделение
                            _activeCardForPlacement = null;
                            card.IsSelectedForPlacement = false;
                        }
                        else
                        {
                            // Снимаем рамки со всех остальных карточек на панели дисциплин
                            foreach (Control c in flpAssignments.Controls)
                            {
                                if (c is Controls.ScheduleCardControl otherCard)
                                {
                                    otherCard.IsSelectedForPlacement = false;
                                    otherCard.Invalidate();
                                }
                            }

                            // Выделяем текущую карточку
                            _activeCardForPlacement = card.ItemData;
                            _activeCardForPlacement.SelectedRoom = card.SelectedRoom; // Подхватываем выбранную на карточке комнату
                            card.IsSelectedForPlacement = true;
                        }

                        card.Invalidate(); // Перерисовываем текущую карточку (появится/исчезнет красная рамка)
                    };

                    flpAssignments.Controls.Add(card);
                }
            }
            else
            {
                Label lblEmpty = new Label { Text = "В этом семестре у группы нет занятий.", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, ForeColor = Color.Gray };
                flpAssignments.Controls.Add(lblEmpty);
            }

            flpAssignments.ResumeLayout();
            flpAssignments.Visible = true;
        }
        private void tlpMainLayout_Paint(object sender, PaintEventArgs e) { }

        private void btnOpenDb_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Access Database (*.accdb)|*.accdb|Старые базы Access (*.mdb)|*.mdb";
            openFileDialog.Title = "Выберите файл базы данных";

            // Если пользователь выбрал файл и нажал ОК
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                selectedPath = openFileDialog.FileName;

                // Настраиваем глобальную строку подключения
                connectionString = $@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={selectedPath};";

                // Шаг 4: Вызываем наш главный метод загрузки, который всё обработает и нарисует карточки
                LoadDatabaseData(selectedPath);
            }
        }
        // Показывает индикаторы только для наведенной карточки
        // Показывает индикаторы только для наведенной карточки
        private void ShowRoomsForCard(Controls.ScheduleCardControl card)
        {
            // 1. Сначала гасим подсветку со ВСЕХ аудиторий
            foreach (var indicator in _roomIndicators.Values)
                indicator.Highlight(false);

            // 2. Включаем рамки только для тех аудиторий, которые в приоритете у этой карточки
            foreach (var room in card.AvailableRooms)
            {
                if (card.PriorityRoomIds.Contains(room.RoomID) && _roomIndicators.ContainsKey(room.RoomID))
                {
                    _roomIndicators[room.RoomID].Highlight(true);
                }
            }

            // 3. Если у карточки уже выбрана какая-то конкретная аудитория - скроллим к ней
            var selected = card.SelectedRoom;
            if (selected != null && _roomIndicators.ContainsKey(selected.RoomID))
            {
                _roomIndicators[selected.RoomID].Highlight(true);
                flpRoomIndicators.ScrollControlIntoView(_roomIndicators[selected.RoomID]);
            }
        }
        // НОВЫЙ МЕТОД: Загружает все комнаты в панель один раз
        private void LoadAllRoomsToPanel()
        {
            flpRoomIndicators.SuspendLayout();
            flpRoomIndicators.Controls.Clear();
            _roomIndicators.Clear();

            int targetHeight = Math.Max(25, flpRoomIndicators.Height - SystemInformation.HorizontalScrollBarHeight - 15);

            // Загружаем все аудитории по порядку
            foreach (var room in classrooms.OrderBy(r => r.RoomNumber))
            {
                var indicator = new Controls.RoomIndicatorControl(room, false);
                indicator.Height = targetHeight;

                // --- ДОБАВЛЯЕМ ЛОГИКУ КЛИКА ПО АУДИТОРИИ ---
                indicator.RoomClicked += (s, args) => {
                    // Снимаем выделение с карточек занятий (если были)
                    if (_activeCardForPlacement != null)
                    {
                        _activeCardForPlacement = null;
                        foreach (Controls.ScheduleCardControl c in flpAssignments.Controls)
                        {
                            c.IsSelectedForPlacement = false;
                            c.Invalidate();
                        }
                    }

                    // Если кликнули на уже выделенную аудиторию - снимаем выделение
                    if (_activeRoomForPlacement == indicator.RoomData)
                    {
                        _activeRoomForPlacement = null;
                        indicator.IsSelectedForPlacement = false;
                    }
                    else
                    {
                        // Снимаем выделение с других аудиторий
                        foreach (Controls.RoomIndicatorControl ind in flpRoomIndicators.Controls)
                        {
                            ind.IsSelectedForPlacement = false;
                            ind.Invalidate();
                        }
                        // Выделяем текущую
                        _activeRoomForPlacement = indicator.RoomData;
                        indicator.IsSelectedForPlacement = true;
                    }
                    indicator.Invalidate(); // Перерисовываем
                };
                _roomIndicators.Add(room.RoomID, indicator);
                flpRoomIndicators.Controls.Add(indicator);
            }
            flpRoomIndicators.ResumeLayout();
        }

        private void ClearRoomIndicators()
        {
            flpRoomIndicators.Controls.Clear();
            _roomIndicators.Clear();
        }

        // --- НОВЫЙ МЕТОД: Генерация главной сетки (Шахматки) ---
        private void GenerateScheduleGrid()
        {
            if (groups == null) return;
            var activeGroups = groups.Where(g => g != null && g.Actually).ToList();
            if (activeGroups.Count == 0) return;

            pnlScheduleContainer.SuspendLayout();
            pnlScheduleContainer.Controls.Clear();

            dgvSchedule = new DataGridView();
            dgvSchedule.Dock = DockStyle.Fill;
            dgvSchedule.AllowUserToAddRows = false;
            dgvSchedule.RowHeadersVisible = false;
            dgvSchedule.BackgroundColor = Color.White;
            dgvSchedule.AllowDrop = true;
            dgvSchedule.ReadOnly = true;

            SetDoubleBuffered(dgvSchedule);

            dgvSchedule.Columns.Add("colDay", "День");
            dgvSchedule.Columns.Add("colPair", "Пара");
            dgvSchedule.Columns["colDay"].Frozen = true;
            dgvSchedule.Columns["colPair"].Frozen = true;

            // ГЕНЕРАЦИЯ ДВОЙНЫХ КОЛОНОК (Дисциплина + Ауд)
            foreach (var group in activeGroups)
            {
                string gName = string.IsNullOrEmpty(group.GroupName) ? "Без названия" : group.GroupName;

                int idxSubj = dgvSchedule.Columns.Add($"g_{group.GroupId}_subj", gName);
                dgvSchedule.Columns[idxSubj].SortMode = DataGridViewColumnSortMode.NotSortable;
                dgvSchedule.Columns[idxSubj].Tag = group; // <--- ВАЖНО: Запоминаем группу

                int idxRoom = dgvSchedule.Columns.Add($"g_{group.GroupId}_room", "Ауд.");
                dgvSchedule.Columns[idxRoom].SortMode = DataGridViewColumnSortMode.NotSortable;
                dgvSchedule.Columns[idxRoom].Tag = group; // <--- ВАЖНО: Запоминаем группу
            }

            dgvSchedule.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvSchedule.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            dgvSchedule.ColumnHeadersHeight = 40;
            dgvSchedule.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgvSchedule.EnableHeadersVisualStyles = false;

            // ГЕНЕРАЦИЯ 72 СТРОК (Светлая и Темная неделя)
            string[] days = { "ПОНЕДЕЛЬНИК", "ВТОРНИК", "СРЕДА", "ЧЕТВЕРГ", "ПЯТНИЦА", "СУББОТА" };
            for (int d = 0; d < 6; d++)
            {
                for (int p = 1; p <= 6; p++)
                {
                    int r1 = dgvSchedule.Rows.Add();
                    dgvSchedule.Rows[r1].Cells["colDay"].Value = days[d];
                    dgvSchedule.Rows[r1].Cells["colPair"].Value = p.ToString();
                    dgvSchedule.Rows[r1].Tag = new Tuple<int, int, int>(d + 1, p, 1); // 1 = Светлая

                    int r2 = dgvSchedule.Rows.Add();
                    dgvSchedule.Rows[r2].Cells["colDay"].Value = days[d];
                    dgvSchedule.Rows[r2].Cells["colPair"].Value = p.ToString();
                    dgvSchedule.Rows[r2].Tag = new Tuple<int, int, int>(d + 1, p, 2); // 2 = Темная

                    for (int c = 2; c < dgvSchedule.Columns.Count; c++)
                    {
                        dgvSchedule.Rows[r1].Cells[c].Style.BackColor = Color.White;
                        dgvSchedule.Rows[r2].Cells[c].Style.BackColor = Color.FromArgb(245, 245, 245);
                    }
                }
            }

            dgvSchedule.CellPainting += DgvSchedule_CellPainting;
            dgvSchedule.Paint += DgvSchedule_Paint;
            dgvSchedule.MouseWheel += DgvSchedule_MouseWheel;
            dgvSchedule.Scroll += (s, ev) => dgvSchedule.Invalidate();

            // СОБЫТИЕ КЛИКА ПО ГРУППЕ
            dgvSchedule.ColumnHeaderMouseClick += DgvSchedule_ColumnHeaderMouseClick;

            dgvSchedule.DragEnter += DgvSchedule_DragEnter;
            dgvSchedule.DragDrop += DgvSchedule_DragDrop;
            dgvSchedule.CellMouseClick += DgvSchedule_CellMouseClick;
            dgvSchedule.CellMouseDoubleClick += DgvSchedule_CellMouseDoubleClick;

            dgvSchedule.DragOver += DgvSchedule_DragOver;
            dgvSchedule.DragLeave += DgvSchedule_DragLeave;

            ApplyZoom();

            pnlScheduleContainer.Controls.Add(dgvSchedule);
            pnlScheduleContainer.ResumeLayout();
        }
        private void DgvSchedule_MouseWheel(object sender, MouseEventArgs e)
        {
            if (Control.ModifierKeys == Keys.Control)
            {
                // Масштабирование
                if (e.Delta > 0) _zoomLevel += ZOOM_STEP;
                else _zoomLevel -= ZOOM_STEP;

                _zoomLevel = Math.Max(MIN_ZOOM, Math.Min(MAX_ZOOM, _zoomLevel));
                ApplyZoom();
                ((HandledMouseEventArgs)e).Handled = true;
            }
            else if (Control.ModifierKeys == Keys.Shift)
            {
                // Горизонтальная прокрутка
                if (e.Delta > 0)
                {
                    // Ограничение левой границы (индексы 0 и 1 закреплены)
                    if (dgvSchedule.FirstDisplayedScrollingColumnIndex > 2)
                        dgvSchedule.FirstDisplayedScrollingColumnIndex--;
                }
                else
                {
                    // Ограничение правой границы
                    if (dgvSchedule.FirstDisplayedScrollingColumnIndex >= 2 &&
                        dgvSchedule.FirstDisplayedScrollingColumnIndex < dgvSchedule.ColumnCount - 1)
                    {
                        dgvSchedule.FirstDisplayedScrollingColumnIndex++;
                    }
                }
                ((HandledMouseEventArgs)e).Handled = true;
            }
        }
        private void ApplyZoom()
        {
            if (dgvSchedule == null) return;
            dgvSchedule.SuspendLayout();

            dgvSchedule.ColumnHeadersHeight = (int)(BASE_HEADER_HEIGHT * _zoomLevel);
            foreach (DataGridViewRow row in dgvSchedule.Rows)
                row.Height = (int)(BASE_ROW_HEIGHT * _zoomLevel);

            dgvSchedule.Columns["colDay"].Width = (int)(BASE_WIDTH_DAY * _zoomLevel);
            dgvSchedule.Columns["colPair"].Width = (int)(BASE_WIDTH_PAIR * _zoomLevel);

            // Масштабируем: четные колонки - предмет (200px), нечетные - аудитория (50px)
            for (int i = 2; i < dgvSchedule.Columns.Count; i += 2)
            {
                dgvSchedule.Columns[i].Width = (int)(BASE_WIDTH_SUBJ * _zoomLevel);
                if (i + 1 < dgvSchedule.Columns.Count)
                    dgvSchedule.Columns[i + 1].Width = (int)(BASE_WIDTH_ROOM * _zoomLevel);
            }

            dgvSchedule.ResumeLayout();
            dgvSchedule.Invalidate();
        }
        private Label CreateHeaderLabel(string text)
        {
            return new Label
            {
                Text = text,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = Color.FromArgb(240, 240, 240)
            };
        }
        public static void SetDoubleBuffered(Control control)
        {
            typeof(Control).GetProperty("DoubleBuffered",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance)
                ?.SetValue(control, true, null);
        }

        private void DgvSchedule_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0) return;

            bool isHeaderCol = (e.ColumnIndex == 0 || e.ColumnIndex == 1);

            // 1. ЗАЛИВКА ФОНА (Подсветка выбранной группы)
            if (!isHeaderCol && _selectedGroup != null)
            {
                var colGroup = dgvSchedule.Columns[e.ColumnIndex].Tag as Models.GroupList;
                if (colGroup != null && colGroup.GroupName == _selectedGroup.GroupName)
                {
                    bool isDarkWeek = e.RowIndex % 2 != 0;
                    Color highlightColor = isDarkWeek ? Color.FromArgb(210, 230, 255) : Color.AliceBlue;
                    using (SolidBrush bgBrush = new SolidBrush(highlightColor))
                        e.Graphics.FillRectangle(bgBrush, e.CellBounds);
                }
                else e.PaintBackground(e.CellBounds, true);
            }
            else e.PaintBackground(e.CellBounds, !isHeaderCol);

            // --- НОВОЕ: УМНАЯ ПОДСВЕТКА ДОСТУПНОСТИ ---
            PoolItem itemToPlace = _activeCardForPlacement ?? _draggedItem;
            if (itemToPlace != null && !isHeaderCol && e.RowIndex >= 0 && _selectedGroup != null)
            {
                var colGroup = dgvSchedule.Columns[e.ColumnIndex].Tag as Models.GroupList;
                var timeInfo = dgvSchedule.Rows[e.RowIndex].Tag as Tuple<int, int, int>;

                // Проверяем доступность только для колонки текущей выбранной группы
                if (colGroup != null && timeInfo != null && colGroup.GroupId == _selectedGroup.GroupId)
                {
                    int day = timeInfo.Item1;
                    int pair = timeInfo.Item2;
                    int week = timeInfo.Item3;

                    // Проверяем через ValidationService
                    var valResult = globalValidator.Validate(itemToPlace, day, pair, week, itemToPlace.SelectedRoom);

                    // Если есть накладка (занят преподаватель, аудитория или день)
                    if (!valResult.IsValid)
                    {
                        // Рисуем легкую красную штриховку или заливку
                        using (SolidBrush errorBrush = new SolidBrush(Color.FromArgb(80, 255, 100, 100)))
                        {
                            e.Graphics.FillRectangle(errorBrush, e.CellBounds);
                        }
                    }
                    else
                    {
                        // Если свободно - рисуем легкую зеленую заливку
                        using (SolidBrush okBrush = new SolidBrush(Color.FromArgb(60, 100, 255, 100)))
                        {
                            e.Graphics.FillRectangle(okBrush, e.CellBounds);
                        }
                    }
                }
            }

            // --- 2. ОТРИСОВКА ЗАПЛАНИРОВАННОГО ЗАНЯТИЯ (НОВОЕ!) ---
            if (!isHeaderCol && e.RowIndex >= 0)
            {
                var colGroup = dgvSchedule.Columns[e.ColumnIndex].Tag as Models.GroupList;
                var timeInfo = dgvSchedule.Rows[e.RowIndex].Tag as Tuple<int, int, int>;

                if (colGroup != null && timeInfo != null)
                {
                    int day = timeInfo.Item1;
                    int pair = timeInfo.Item2;
                    int week = timeInfo.Item3;

                    // Ищем, есть ли пара в этом слоте для этой группы
                    // ВНИМАНИЕ: Если у тебя в Schedule.cs свойство называется не LessonNumber, а PairNumber, исправь здесь!
                    var lesson = schedules.FirstOrDefault(s =>
                        s.GroupId == colGroup.GroupId &&
                        s.DayOfWeek == day &&
                        s.LessonNumber == pair &&
                        s.WeekType == week);

                    if (lesson != null)
                    {
                        bool isSubjectCol = e.ColumnIndex % 2 == 0; // Четные колонки - предмет, нечетные - ауд.

                        // Закрашиваем саму карточку внутри ячейки (светло-зеленым)
                        Rectangle innerRect = new Rectangle(e.CellBounds.X + 1, e.CellBounds.Y + 1, e.CellBounds.Width - 3, e.CellBounds.Height - 3);
                        Color typeColor;
                        switch (lesson.LessonType)
                        {
                            case 0: typeColor = Color.LightGreen; break;
                            case 1: typeColor = Color.LightSkyBlue; break;
                            case 2: typeColor = Color.LightCoral; break;
                            default: typeColor = Color.FromArgb(210, 245, 210); break;
                        }

                        using (SolidBrush cardBrush = new SolidBrush(Color.FromArgb(180, typeColor))) // 180 для легкой прозрачности
                        {
                            e.Graphics.FillRectangle(cardBrush, innerRect);
                        }

                        // Рисуем текст в зависимости от колонки
                        if (isSubjectCol)
                        {
                            var subj = subjects.FirstOrDefault(s => s.SubjectID == lesson.SubjectID);
                            var teacher = teachers.FirstOrDefault(t => t.TeacherID == lesson.TeacherID);

                            string text = $"{subj?.SubjectName ?? "..."}\n{teacher?.FullName ?? "..."}";
                            TextRenderer.DrawText(e.Graphics, text, new Font("Segoe UI", 8), innerRect, Color.Black,
                                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak);
                        }
                        else
                        {
                            var room = classrooms.FirstOrDefault(r => r.RoomID == lesson.RoomID);
                            string text = room?.RoomNumber ?? "";
                            TextRenderer.DrawText(e.Graphics, text, new Font("Segoe UI", 9, FontStyle.Bold), innerRect, Color.Black,
                                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                        }
                    }
                }
            }

            // 3. ОТРИСОВКА ГРАНИЦ
            e.Graphics.DrawLine(_gridPen, e.CellBounds.Right - 1, e.CellBounds.Top, e.CellBounds.Right - 1, e.CellBounds.Bottom);

            if (!isHeaderCol) e.Graphics.DrawLine(_gridPen, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1);
            else
            {
                if (e.ColumnIndex == 0 && (e.RowIndex + 1) % 12 == 0) // Конец дня
                    e.Graphics.DrawLine(_gridPen, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1);
                else if (e.ColumnIndex == 1 && (e.RowIndex + 1) % 2 == 0) // Конец пары
                    e.Graphics.DrawLine(_gridPen, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right, e.CellBounds.Bottom - 1);
            }

            // Контент мы нарисовали сами, блокируем стандартную отрисовку DataGridView
            e.Handled = true;
        }

        private void DgvSchedule_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 2) return;

            // --- МЕТОД 2: ЛЕВЫЙ КЛИК (ВСТАВКА ИЛИ ЗАМЕНА АУДИТОРИИ) ---
            if (e.Button == MouseButtons.Left)
            {
                // Если ставим дисциплину
                if (_activeCardForPlacement != null)
                {
                    TryPlaceLesson(_activeCardForPlacement, e.RowIndex);
                    _activeCardForPlacement = null;
                    PopulateAssignmentCards();
                    return;
                }
                // Если просто меняем аудиторию в существующей паре
                else if (_activeRoomForPlacement != null)
                {
                    TryUpdateRoom(_activeRoomForPlacement, e.RowIndex, e.ColumnIndex);

                    // Снимаем выделение после успешного клика
                    _activeRoomForPlacement = null;
                    foreach (Controls.RoomIndicatorControl ind in flpRoomIndicators.Controls)
                    {
                        ind.IsSelectedForPlacement = false;
                        ind.Invalidate();
                    }
                    return;
                }
            }
            // Только правая кнопка мыши и только по учебным ячейкам (индекс >= 2)
            if (e.Button == MouseButtons.Right && e.RowIndex >= 0 && e.ColumnIndex >= 2)
            {
                var colGroup = dgvSchedule.Columns[e.ColumnIndex].Tag as Models.GroupList;
                var timeInfo = dgvSchedule.Rows[e.RowIndex].Tag as Tuple<int, int, int>;

                if (colGroup != null && timeInfo != null)
                {
                    int day = timeInfo.Item1;
                    int pair = timeInfo.Item2;
                    int week = timeInfo.Item3;

                    // Ищем занятие (снова проверяй LessonNumber / PairNumber)
                    var lesson = schedules.FirstOrDefault(s =>
                        s.GroupId == colGroup.GroupId &&
                        s.DayOfWeek == day &&
                        s.LessonNumber == pair &&
                        s.WeekType == week);

                    if (lesson != null)
                    {
                        if (MessageBox.Show("Очистить эту ячейку и вернуть занятие в пул?", "Удаление", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                        {
                            // 1. Удаляем из расписания
                            schedules.Remove(lesson);

                            // 2. ИЩЕМ ВО ВСЕМ ПУЛЕ (GetAllItems) и СВЕРЯЕМ ТИП ЗАНЯТИЯ (LessonType)
                            var poolItem = globalPool.GetAllItems()
                                .FirstOrDefault(p => p.PlanReference.Subject.SubjectID == lesson.SubjectID &&
                                                     p.AssignedTeacher.TeacherID == lesson.TeacherID &&
                                                     p.PlanReference.Group.GroupId == lesson.GroupId &&
                                                     p.LessonType == lesson.LessonType);

                            if (poolItem != null) poolItem.RemainingCount++;

                            LogAction("Удаление", "Занятие", $"{day} день, {pair} пара");

                            dgvSchedule.Invalidate();
                            PopulateAssignmentCards();
                        }
                    }
                }
            }
        }

        private void DgvSchedule_Paint(object sender, PaintEventArgs e)
        {
            if (dgvSchedule == null || dgvSchedule.Rows.Count == 0) return;
            int firstRow = dgvSchedule.FirstDisplayedScrollingRowIndex;
            if (firstRow < 0) return;
            int lastRow = firstRow + dgvSchedule.DisplayedRowCount(true);

            for (int i = firstRow; i <= lastRow && i < dgvSchedule.Rows.Count; i++)
            {
                if ((i + 1) % 12 == 0)
                {
                    var rect = dgvSchedule.GetRowDisplayRectangle(i, true);
                    if (rect.Height > 0) e.Graphics.DrawLine(_thickPen, rect.Left, rect.Bottom - 1, rect.Right, rect.Bottom - 1);
                }
            }

            var rectNav = dgvSchedule.GetColumnDisplayRectangle(1, true);
            if (rectNav.Width > 0) e.Graphics.DrawLine(_thickPen, rectNav.Right - 1, 0, rectNav.Right - 1, dgvSchedule.Height);

            // Разделители групп
            for (int c = 3; c < dgvSchedule.Columns.Count; c += 2)
            {
                var rectCol = dgvSchedule.GetColumnDisplayRectangle(c, true);
                if (rectCol.Width > 0) e.Graphics.DrawLine(_thickPen, rectCol.Right - 1, 0, rectCol.Right - 1, dgvSchedule.Height);
            }

            Font fontDay = new Font("Segoe UI", BASE_FONT_SIZE_DAY * _zoomLevel, FontStyle.Bold);
            Font fontPair = new Font("Segoe UI", BASE_FONT_SIZE_PAIR * _zoomLevel, FontStyle.Bold);

            // Отрисовка Дней
            int startDayBlock = (firstRow / 12) * 12;
            for (int i = startDayBlock; i <= lastRow; i += 12)
            {
                if (i >= dgvSchedule.Rows.Count) break;
                Rectangle rectDay = GetMergedRectangle(dgvSchedule, 0, i, 12);
                if (rectDay.Height > 20)
                {
                    string dayText = dgvSchedule.Rows[i].Cells["colDay"].Value?.ToString() ?? "";
                    DrawVerticalText(e.Graphics, dgvSchedule, dayText, fontDay, rectDay);
                }
            }

            // Отрисовка Пар
            int startPairBlock = (firstRow / 2) * 2;
            for (int i = startPairBlock; i <= lastRow; i += 2)
            {
                if (i >= dgvSchedule.Rows.Count) break;
                Rectangle rectPair = GetMergedRectangle(dgvSchedule, 1, i, 2);
                if (rectPair.Height > 10)
                {
                    string pairText = dgvSchedule.Rows[i].Cells["colPair"].Value?.ToString() ?? "";
                    TextRenderer.DrawText(e.Graphics, pairText, fontPair, rectPair, Color.Black, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
            }

            // --- ОТРИСОВКА ЦВЕТНОЙ ПОДСКАЗКИ ПРИ ПЕРЕТАСКИВАНИИ ---
            if (_draggedItem != null || _draggedRoom != null)
            {
                string dragText = "";
                Color bgColor = Color.White;
                int rectHeight = 45;

                // Если тащим занятие (Определяем цвет)
                if (_draggedItem != null)
                {
                    dragText = $"{_draggedItem.DisplayName}\n{_draggedItem.AssignedTeacher?.FullName}";
                    switch (_draggedItem.LessonType)
                    {
                        case 0: bgColor = Color.LightGreen; break;
                        case 1: bgColor = Color.LightSkyBlue; break;
                        case 2: bgColor = Color.LightCoral; break;
                        default: bgColor = Color.LightGray; break;
                    }
                }
                // Если тащим аудиторию
                else if (_draggedRoom != null)
                {
                    dragText = $"Ауд. {_draggedRoom.RoomNumber}";
                    bgColor = Color.FromArgb(240, 240, 240); // Серый цвет кнопок
                    rectHeight = 30; // Плашка поменьше
                }

                Rectangle dragRect = new Rectangle(_dragPosition.X + 15, _dragPosition.Y + 15, 180, rectHeight);

                // Делаем цвет слегка прозрачным (Альфа-канал 210)
                using (SolidBrush dragBrush = new SolidBrush(Color.FromArgb(210, bgColor.R, bgColor.G, bgColor.B)))
                {
                    e.Graphics.FillRectangle(dragBrush, dragRect);
                }
                e.Graphics.DrawRectangle(Pens.Gray, dragRect);

                TextRenderer.DrawText(e.Graphics, dragText, new Font("Segoe UI", 8, FontStyle.Bold), dragRect, Color.Black,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak);
            }
        }

        private Rectangle GetMergedRectangle(DataGridView grid, int colIndex, int startRow, int count)
        {
            Rectangle res = Rectangle.Empty;
            for (int k = 0; k < count; k++)
            {
                int rowIndex = startRow + k;
                if (rowIndex >= grid.Rows.Count) break;
                Rectangle r = grid.GetCellDisplayRectangle(colIndex, rowIndex, false);
                if (r.Width == 0) continue;
                if (res == Rectangle.Empty) res = r;
                else res = Rectangle.Union(res, r);
            }
            return res;
        }

        // Метод обновления подсветки аудиторий в карусели
        private void UpdateRoomHighlight(Models.Classroom selectedRoom)
        {
            // Сначала гасим все рамки
            foreach (var indicator in _roomIndicators.Values) 
                indicator.Highlight(false);

            // Если выбрана конкретная аудитория (не "?" и не "..."), подсвечиваем её
            if (selectedRoom != null && _roomIndicators.ContainsKey(selectedRoom.RoomID))
            {
                var activeIndicator = _roomIndicators[selectedRoom.RoomID];
                activeIndicator.Highlight(true);
                
                // Прокручиваем карусель к выбранной аудитории
                flpRoomIndicators.ScrollControlIntoView(activeIndicator); 
            }
        }

        private void DrawVerticalText(Graphics g, DataGridView grid, string text, Font font, Rectangle rect)
        {
            var state = g.Save();
            float cx = rect.X + rect.Width / 2f;
            float cy = rect.Y + rect.Height / 2f;
            g.SetClip(new Rectangle(0, 0, grid.Width, grid.Height));
            g.TranslateTransform(cx, cy);
            g.RotateTransform(-90); // Поворот текста
            using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                g.DrawString(text, font, Brushes.Black, 0, 0, sf);
            }
            g.Restore(state);
        }

        // Единый метод для смены группы из любого места интерфейса
        // Единый метод для смены группы
        private void SelectGroupInternal(Models.GroupList group)
        {
            _selectedGroup = group;

            // СТРОКУ _selectedSemester = 0; МЫ УДАЛИЛИ!
            // Теперь выбранный семестр сохраняется при переключении групп.

            lblGroupSelect.Text = "Группа: " + (_selectedGroup?.GroupName ?? "не выбрано");

            UpdateColumnHighlights();
            PopulateAssignmentCards();
        }

        // Теперь обновляем обработчик клика по колонке DataGridView
        private void DgvSchedule_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex < 2) return;

            var clickedCol = dgvSchedule.Columns[e.ColumnIndex];
            var group = clickedCol.Tag as Models.GroupList;

            if (group != null)
            {
                // Если кликнули на ту же группу — снимаем выбор, иначе выбираем новую
                if (_selectedGroup != null && _selectedGroup.GroupName == group.GroupName)
                    SelectGroupInternal(null);
                else
                    SelectGroupInternal(group);
            }
        }
        private void UpdateColumnHighlights()
        {
            for (int i = 2; i < dgvSchedule.Columns.Count; i++)
            {
                var colGroup = dgvSchedule.Columns[i].Tag as Models.GroupList;
                bool isSelected = (_selectedGroup != null && colGroup != null && colGroup.GroupName == _selectedGroup.GroupName);
                dgvSchedule.Columns[i].HeaderCell.Style.BackColor = isSelected ? Color.LightSkyBlue : Color.FromArgb(240, 240, 240);
            }
            dgvSchedule.Invalidate();
        }

        // --- ОБРАБОТЧИК КНОПКИ ВЫБОРА ГРУППЫ ---
        private void btnSelectGroup_Click(object sender, EventArgs e)
        {
            if (groups == null || groups.Count == 0) return;

            ContextMenuStrip menu = new ContextMenuStrip();
            foreach (var g in groups.Where(g => g.Actually))
            {
                var item = menu.Items.Add(g.GroupName);
                item.Tag = g;
                item.Click += (s, args) =>
                {
                    _selectedGroup = (Models.GroupList)((ToolStripMenuItem)s).Tag;
                    UpdateColumnHighlights();
                    PopulateAssignmentCards();
                };
            }
            menu.Show(Cursor.Position);
        }



        // --- КЛИК ПО НАЗВАНИЮ ГРУППЫ ---
        // --- ВЫБОР ГРУППЫ ЧЕРЕЗ ЛЕЙБЛ ---
        private void lblGroupSelect_Click(object sender, EventArgs e)
        {
            if (groups == null || groups.Count == 0) return;

            ContextMenuStrip menu = new ContextMenuStrip();
            foreach (var g in groups.Where(g => g.Actually))
            {
                var item = menu.Items.Add(g.GroupName);
                item.Tag = g;
                item.Click += (s, args) =>
                {
                    var selected = (Models.GroupList)((ToolStripMenuItem)s).Tag;
                    SelectGroupInternal(selected); // Вызываем единый метод выбора
                };
            }
            menu.Show(lblGroupSelect, new Point(0, lblGroupSelect.Height));
        }

        // --- ВЫБОР СЕМЕСТРА ---
        // --- ВЫБОР СЕМЕСТРА (Теперь не зависит от группы) ---
        private void lblSemesterSelect_Click(object sender, EventArgs e)
        {
            ContextMenuStrip menu = new ContextMenuStrip();

            // Передаем 1 как код нечетного семестра (Осенний: 1, 3, 5, 7)
            var itemAutumn = menu.Items.Add("Осенний семестр (Нечетный)");
            itemAutumn.Click += (s, args) => SetSemester(1, "Осенний");

            // Передаем 2 как код четного семестра (Весенний: 2, 4, 6, 8)
            var itemSpring = menu.Items.Add("Весенний семестр (Четный)");
            itemSpring.Click += (s, args) => SetSemester(2, "Весенний");

            menu.Show(Cursor.Position);
        }

        private void SetSemester(int semMode, string semName)
        {
            _selectedSemester = semMode;
            lblSemesterSelect.Text = $"Семестр: {semName}";
            PopulateAssignmentCards();
        }

        // --- ДОПИСАТЬ НОВЫЕ МЕТОДЫ В КЛАСС Form1 ---

        private void DgvSchedule_DragEnter(object sender, DragEventArgs e)
        {
            if (_selectedGroup == null) { e.Effect = DragDropEffects.None; return; }

            if (e.Data.GetDataPresent(typeof(PoolItem)))
            {
                e.Effect = DragDropEffects.Move;
                _draggedItem = e.Data.GetData(typeof(PoolItem)) as PoolItem;
                _draggedRoom = null;
            }
            else if (e.Data.GetDataPresent(typeof(Classroom)))
            {
                e.Effect = DragDropEffects.Link;
                _draggedRoom = e.Data.GetData(typeof(Classroom)) as Classroom;
                _draggedItem = null;
            }
            else e.Effect = DragDropEffects.None;
        }
        private void DgvSchedule_DragDrop(object sender, DragEventArgs e)
        {
            // СБРОС визуальных подсказок
            _draggedItem = null;
            _draggedRoom = null;

            Point clientPoint = dgvSchedule.PointToClient(new Point(e.X, e.Y));
            var hit = dgvSchedule.HitTest(clientPoint.X, clientPoint.Y);
            if (hit.RowIndex < 0) return;

            if (e.Data.GetData(typeof(PoolItem)) is PoolItem draggedItem)
                TryPlaceLesson(draggedItem, hit.RowIndex);
            else if (e.Data.GetData(typeof(Classroom)) is Classroom droppedRoom)
                TryUpdateRoom(droppedRoom, hit.RowIndex, hit.ColumnIndex);

            dgvSchedule.Invalidate();
        }
        // НОВЫЙ МЕТОД: Замена только аудитории в ячейке
        private void TryUpdateRoom(Classroom room, int rowIndex, int colIndex)
        {
            if (colIndex < 2) return; // Игнорируем клики по дням и парам

            var colGroup = dgvSchedule.Columns[colIndex].Tag as Models.GroupList;
            var timeInfo = dgvSchedule.Rows[rowIndex].Tag as Tuple<int, int, int>;

            if (colGroup != null && timeInfo != null)
            {
                // Ищем занятие в расписании
                var lesson = schedules.FirstOrDefault(s =>
                    s.GroupId == colGroup.GroupId &&
                    s.DayOfWeek == timeInfo.Item1 &&
                    s.LessonNumber == timeInfo.Item2 &&
                    s.WeekType == timeInfo.Item3);

                if (lesson != null)
                {
                    lesson.RoomID = room.RoomID; // Просто перезаписываем ID комнаты
                    LogAction("Смена ауд.", $"{room.RoomNumber}", $"{timeInfo.Item1} день, {timeInfo.Item2} пара");
                    dgvSchedule.Invalidate(); // Перерисовываем графику
                }
            }
        }
        private void DgvSchedule_DragOver(object sender, DragEventArgs e)
        {
            // Работает, если тащим либо предмет, либо аудиторию
            if (_draggedItem != null || _draggedRoom != null)
            {
                _dragPosition = dgvSchedule.PointToClient(new Point(e.X, e.Y));
                dgvSchedule.Invalidate();
            }
        }
        // Новый метод для быстрой смены аудитории



        // НОВЫЙ МЕТОД: Если увели мышь за пределы таблицы
        private void DgvSchedule_DragLeave(object sender, EventArgs e)
        {
            _draggedItem = null;
            _draggedRoom = null;
            dgvSchedule.Invalidate();
        }
        private void TryPlaceLesson(PoolItem item, int rowIndex)
        {
            if (_selectedGroup == null) return;

            // Получаем координаты времени из ячейки
            var timeInfo = dgvSchedule.Rows[rowIndex].Tag as Tuple<int, int, int>;
            int day = timeInfo.Item1;
            int pair = timeInfo.Item2;
            int weekType = timeInfo.Item3;

            // --- ЛОГИКА ЗАМЕНЫ (НОВОЕ) ---
            // Ищем, нет ли уже занятия в этой ячейке для этой группы
            var existingLesson = schedules.FirstOrDefault(s =>
                s.GroupId == _selectedGroup.GroupId &&
                s.DayOfWeek == day &&
                s.LessonNumber == pair &&
                s.WeekType == weekType);

            if (existingLesson != null)
            {
                // 1. Возвращаем старое занятие в пул (увеличиваем счетчик)
                // Ищем в пуле элемент, который соответствует старому занятию
                var oldPoolItem = globalPool.GetItemForSchedule(existingLesson);
                if (oldPoolItem != null)
                {
                    oldPoolItem.RemainingCount++;
                }

                // 2. Удаляем старое занятие из списка расписания
                schedules.Remove(existingLesson);
                LogAction("Замена", "Старое занятие удалено", $"{day} день, {pair} пара");
            }

            // --- ПРОВЕРКА НОВОГО ЗАНЯТИЯ ---
            var result = globalValidator.Validate(item, day, pair, weekType, item.SelectedRoom);
            if (!result.IsValid)
            {
                var dialog = MessageBox.Show(
                    $"Внимание! Нарушение ограничений:\n{result.ErrorMessage}\n\nВсе равно поставить?",
                    "Конфликт", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (dialog == DialogResult.No)
                {
                    // Если пользователь отказался заменять, и мы уже удалили старое — 
                    // тут можно либо оставить ячейку пустой, либо (лучше) не удалять старое до подтверждения.
                    return;
                }
            }

            // --- УСТАНОВКА НОВОГО ЗАНЯТИЯ ---
            Schedule newLesson = new Schedule
            {
                GroupId = _selectedGroup.GroupId,
                SubjectID = item.PlanReference.Subject.SubjectID,
                TeacherID = item.AssignedTeacher.TeacherID,
                RoomID = item.SelectedRoom?.RoomID ?? 0,
                DayOfWeek = day,
                LessonNumber = pair,
                WeekType = weekType,
                LessonType = item.LessonType
            };

            schedules.Add(newLesson);
            item.RemainingCount--;

            LogAction("Добавление", item.DisplayName, $"{day} день, {pair} пара");

            // --- ОБНОВЛЕНИЕ ИНТЕРФЕЙСА (ВАЖНО) ---
            dgvSchedule.Invalidate(); // Принудительная перерисовка всей сетки
            PopulateAssignmentCards(); // Обновление счетчиков в левой панели
        }
        private void LogAction(string actionName, string objName, string timeStr)
        {
            try
            {
                string logPath = Path.Combine(Application.StartupPath, "history_log.txt");
                int actionNum = File.Exists(logPath) ? File.ReadAllLines(logPath).Length + 1 : 1;
                string dateStr = DateTime.Now.ToString("dd.MM.yyyy");
                string nowTime = DateTime.Now.ToString("HH:mm:ss");
                string logLine = $"{actionNum};{actionName};{objName};{timeStr};{nowTime};{dateStr}";
                File.AppendAllLines(logPath, new[] { logLine });
            }
            catch { }
        }

        private void DgvSchedule_CellMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 2) return;
            if (e.Button != MouseButtons.Left) return;

            var colGroup = dgvSchedule.Columns[e.ColumnIndex].Tag as Models.GroupList;
            var timeInfo = dgvSchedule.Rows[e.RowIndex].Tag as Tuple<int, int, int>;

            if (colGroup != null && timeInfo != null)
            {
                // 1. Блокируем вызов, если ячейка уже занята
                var lesson = schedules.FirstOrDefault(s =>
                    s.GroupId == colGroup.GroupId &&
                    s.DayOfWeek == timeInfo.Item1 &&
                    s.LessonNumber == timeInfo.Item2 &&
                    s.WeekType == timeInfo.Item3);

                if (lesson != null)
                {
                    MessageBox.Show("«Эта ячейка уже занята. Сначала удалите текущее занятие правым кликом.»", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // 2. Достаем все карточки конкретно для этой группы
                var groupPool = globalPool.GetAllItems()
                    .Where(i => i.PlanReference.Group.GroupId == colGroup.GroupId)
                    .ToList();

                if (groupPool.Count == 0) return;

                // 3. Открываем наше новое окно ручного ввода
                using (FormManualEntry form = new FormManualEntry(colGroup, timeInfo.Item1, timeInfo.Item2, timeInfo.Item3, groupPool, teachers, classrooms))
                {
                    if (form.ShowDialog() == DialogResult.OK)
                    {
                        var selectedItem = form.SelectedItem;

                        // Запоминаем оригинальные настройки пула, чтобы не сломать карточку навсегда
                        var originalTeacher = selectedItem.AssignedTeacher;
                        var originalRoom = selectedItem.SelectedRoom;

                        // Применяем то, что пользователь выбрал в выпадающих списках формы
                        selectedItem.AssignedTeacher = form.SelectedTeacher;
                        selectedItem.SelectedRoom = form.SelectedRoom;

                        // Ставим пару через наш основной надежный метод!
                        TryPlaceLesson(selectedItem, e.RowIndex);

                        // Возвращаем пулу исходного преподавателя (чтобы следующая пара по умолчанию была правильной)
                        selectedItem.AssignedTeacher = originalTeacher;
                        selectedItem.SelectedRoom = originalRoom;
                    }
                }
            }
        }

        private void RefreshAllData()
        {
            // 1. Очищаем старые списки, чтобы данные не дублировались
            classrooms.Clear();
            groups.Clear();
            subjects.Clear();
            teachers.Clear();
            academicPlans.Clear();
            teacherDaysOff.Clear();
            teacherRoomPrefs.Clear();
            enrichedPlans.Clear();
            _roomIndicators.Clear();

            // 2. Вызываем твою логику загрузки из БД
            // Убедись, что вызываешь те же методы, что и при старте программы (в Form1_Load)
            LoadDatabaseData(selectedPath);
            // 3. Обновляем визуальные элементы
            PopulateAssignmentCards(); // Перерисовывает карточки слева
            LoadAllRoomsToPanel();     // Перерисовывает кнопки аудиторий внизу
            dgvSchedule.Invalidate();  // Перерисовывает саму шахматку
        }

        private void btnHistory_Click(object sender, EventArgs e)
        {
            new FormHistory().ShowDialog();
        }




        private void btnDicts_Click(object sender, EventArgs e)
        {
            // Создаем форму справочников
            // Передаем твою строку подключения (она у тебя объявлена в полях Form1)
            using (FormDictionaries dictForm = new FormDictionaries(connectionString))
            {
                // Открываем как модальное окно
                dictForm.ShowDialog();

                // Проверяем, были ли сохранены изменения (свойство DataChanged мы добавили в FormDictionaries)
                if (dictForm.DataChanged)
                {
                    // Если данные в базе изменились — вызываем полное обновление
                    RefreshAllData();

                    MessageBox.Show("Данные успешно обновлены!", "Синхронизация",
                                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
    }
}