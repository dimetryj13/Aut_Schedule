using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.Windows.Forms;

namespace AutoSchedule
{
    public class FormDictionaries : Form
    {
        private TabControl tabControl;
        private Panel topPanel;
        private string _connectionString;

        private class TableContext
        {
            public DataTable Table { get; set; }
            public OleDbDataAdapter Adapter { get; set; }
        }
        private Dictionary<TabPage, TableContext> _tablesMap = new Dictionary<TabPage, TableContext>();

        public bool DataChanged { get; private set; } = false;
        private bool _isClosing = false;

        public FormDictionaries(string connectionString)
        {
            _connectionString = connectionString;
            this.Text = "Редактор справочников БД";
            this.Size = new Size(1200, 700);
            this.StartPosition = FormStartPosition.CenterParent;

            InitializeUI();
            LoadDataFromDatabase();

            this.FormClosing += FormDictionaries_FormClosing;
        }

        private void InitializeUI()
        {
            topPanel = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.WhiteSmoke };

            Button btnAdd = CreateToolbarButton("➕ Добавить запись", Color.ForestGreen);
            btnAdd.Click += (s, e) => {
                var grid = GetActiveGrid();
                if (grid != null && grid.Rows.Count > 0)
                {
                    // Перемещаем фокус на самую нижнюю (новую) строку для безопасного ввода
                    grid.CurrentCell = grid.Rows[grid.Rows.Count - 1].Cells[1];
                    grid.BeginEdit(true);
                }
            };

            Button btnDelete = CreateToolbarButton("🗑 Удалить", Color.Crimson);
            btnDelete.Click += (s, e) => {
                var grid = GetActiveGrid();
                if (grid != null && grid.CurrentRow != null && !grid.CurrentRow.IsNewRow)
                    grid.Rows.Remove(grid.CurrentRow);
            };

            Button btnRevert = CreateToolbarButton("↺ Отменить изменения", Color.OrangeRed);
            btnRevert.Click += (s, e) => RevertCurrentTab();

            Button btnSaveCurrent = CreateToolbarButton("💾 Сохранить таблицу", Color.FromArgb(0, 122, 204));
            btnSaveCurrent.Click += (s, e) => SaveCurrentTab();

            FlowLayoutPanel flowPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            flowPanel.Controls.AddRange(new Control[] { btnSaveCurrent, btnAdd, btnDelete, btnRevert });
            topPanel.Controls.Add(flowPanel);

            tabControl = new TabControl { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10), DrawMode = TabDrawMode.OwnerDrawFixed };
            tabControl.DrawItem += TabControl_DrawItem;
            tabControl.Deselecting += TabControl_Deselecting;

            this.Controls.Add(tabControl);
            this.Controls.Add(topPanel);
        }

        private Button CreateToolbarButton(string text, Color foreColor)
        {
            return new Button { Text = text, AutoSize = true, Height = 35, FlatStyle = FlatStyle.Flat, ForeColor = foreColor, Font = new Font("Segoe UI", 10, FontStyle.Bold), Cursor = Cursors.Hand };
        }

        private void LoadDataFromDatabase()
        {
            try
            {
                var (daClassrooms, dtClassrooms) = InitAdapter("SELECT * FROM Classroom");
                var (daTeachers, dtTeachers) = InitAdapter("SELECT * FROM Teachers");
                var (daSubjects, dtSubjects) = InitAdapter("SELECT * FROM Subjects");
                var (daGroups, dtGroups) = InitAdapter("SELECT * FROM GroupsList");
                var (daAvailability, dtAvailability) = InitAdapter("SELECT * FROM TeacherAvailability");
                var (daDaysOff, dtDaysOff) = InitAdapter("SELECT * FROM TeacherDaysOff");
                var (daRoomPrefs, dtRoomPrefs) = InitAdapter("SELECT * FROM TeacherRoomPrefs");

                TabPage tabClassrooms = CreateTab("Аудитории", dtClassrooms, daClassrooms, "RoomID");
                RenameColumns(GetGrid(tabClassrooms), new Dictionary<string, string> { { "RoomNumber", "Номер аудитории" }, { "Capacity", "Вместимость" }, { "HasComputers", "Есть ПК" } });

                TabPage tabSubjects = CreateTab("Дисциплины", dtSubjects, daSubjects, "SubjectID");
                ReplaceWithComboBox(GetGrid(tabSubjects), "FixedRoom", dtClassrooms, "RoomNumber", "RoomNumber", "Закреп. аудитория");
                ReplaceWithComboBox(GetGrid(tabSubjects), "ForbiddenRoom", dtClassrooms, "RoomNumber", "RoomNumber", "Запрещ. аудитория");
                RenameColumns(GetGrid(tabSubjects), new Dictionary<string, string> { { "SubjectName", "Название дисциплины" }, { "RequiresComputers", "Требуются ПК" } });

                TabPage tabTeachers = CreateTab("Преподаватели", dtTeachers, daTeachers, "TeacherID");
                RenameColumns(GetGrid(tabTeachers), new Dictionary<string, string> { { "FullName", "ФИО" }, { "Department", "Кафедра" }, { "MaxLectureGroups", "Макс. групп (Лекции)" }, { "MaxPracticeGroups", "Макс. групп (Практика)" } });

                TabPage tabGroups = CreateTab("Группы", dtGroups, daGroups, "GroupId");
                ReplaceWithComboBox(GetGrid(tabGroups), "MainTeacher", dtTeachers, "TeacherID", "FullName", "Куратор");
                RenameColumns(GetGrid(tabGroups), new Dictionary<string, string> { { "GroupName", "Название группы" }, { "StudentCount", "Студентов" }, { "IsFullTime", "Очная форма" }, { "Actually", "Актуальна" }, { "YearLearn", "Курс" } });

                TabPage tabAvailability = CreateTab("Доступность", dtAvailability, daAvailability, "TeacherAvailabilityId");
                ReplaceWithComboBox(GetGrid(tabAvailability), "TeacherID", dtTeachers, "TeacherID", "FullName", "Преподаватель");
                ReplaceWithStaticComboBox(GetGrid(tabAvailability), "DayIdx", new[] { "Понедельник", "Вторник", "Среда", "Четверг", "Пятница", "Суббота" }, "День недели");
                ReplaceWithStaticComboBox(GetGrid(tabAvailability), "PairIdx", new[] { "1", "2", "3", "4", "5", "6" }, "Номер пары");
                RenameColumns(GetGrid(tabAvailability), new Dictionary<string, string> { { "IsAvailable", "Доступен" } });

                TabPage tabDaysOff = CreateTab("Выходные", dtDaysOff, daDaysOff, "TeacherDayOffId");
                ReplaceWithComboBox(GetGrid(tabDaysOff), "TeacherName", dtTeachers, "TeacherID", "FullName", "Преподаватель");
                RenameColumns(GetGrid(tabDaysOff), new Dictionary<string, string> { { "Mon", "Пн" }, { "Tue", "Вт" }, { "Wed", "Ср" }, { "Thu", "Чт" }, { "Fri", "Пт" }, { "Sat", "Сб" } });

                TabPage tabRoomPrefs = CreateTab("Приоритеты аудиторий", dtRoomPrefs, daRoomPrefs, "TeacherRoomPrefId");
                ReplaceWithComboBox(GetGrid(tabRoomPrefs), "TeacherID", dtTeachers, "TeacherID", "FullName", "Преподаватель");
                ReplaceWithComboBox(GetGrid(tabRoomPrefs), "RoomNumber", dtClassrooms, "RoomID", "RoomNumber", "Аудитория");

                DataTable dtPriority = new DataTable();
                dtPriority.Columns.Add("Val", typeof(int));
                dtPriority.Columns.Add("Display", typeof(string));
                dtPriority.Rows.Add(0, "0 - Отсутствует"); dtPriority.Rows.Add(1, "1 - Низкий");
                dtPriority.Rows.Add(2, "2 - Средний"); dtPriority.Rows.Add(3, "3 - Высокий"); dtPriority.Rows.Add(4, "4 - Наивысший");
                ReplaceWithComboBox(GetGrid(tabRoomPrefs), "Priority", dtPriority, "Val", "Display", "Приоритет");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки: " + ex.Message, "Ошибка БД", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private (OleDbDataAdapter, DataTable) InitAdapter(string query)
        {
            OleDbDataAdapter da = new OleDbDataAdapter(query, _connectionString);
            da.MissingSchemaAction = MissingSchemaAction.AddWithKey;
            OleDbCommandBuilder cb = new OleDbCommandBuilder(da) { QuotePrefix = "[", QuoteSuffix = "]", ConflictOption = ConflictOption.OverwriteChanges };
            DataTable dt = new DataTable();
            da.Fill(dt);

            dt.RowChanged += (s, e) => tabControl.Invalidate();
            dt.RowDeleted += (s, e) => tabControl.Invalidate();

            return (da, dt);
        }

        private TabPage CreateTab(string title, DataTable dt, OleDbDataAdapter da, string primaryKeyColumn)
        {
            // --- ФИКС №1: БЕЗОПАСНАЯ АВТОНУМЕРАЦИЯ ДЛЯ НОВЫХ СТРОК ---
            if (dt.Columns.Contains(primaryKeyColumn))
            {
                dt.Columns[primaryKeyColumn].AutoIncrement = true;
                dt.Columns[primaryKeyColumn].AutoIncrementSeed = -1; // Присваиваем временные ID (-1, -2...), пока Access не выдаст настоящие
                dt.Columns[primaryKeyColumn].AutoIncrementStep = -1;
            }

            TabPage tab = new TabPage(title);
            DataGridView dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                DataSource = dt,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                AllowUserToAddRows = true // Разрешаем добавление снизу
            };

            // НОВОЕ: Включаем мгновенное редактирование по одному клику (списки будут выпадать сразу)
            dgv.EditMode = DataGridViewEditMode.EditOnEnter;
            // НОВОЕ: Подключаем обработчик колесика мыши
            dgv.MouseWheel += Dgv_MouseWheel;

            dgv.DataError += (s, e) => { e.ThrowException = false; };
            dgv.CellValidating += Dgv_CellValidating;
            dgv.DataBindingComplete += (s, e) => { if (dgv.Columns.Contains(primaryKeyColumn)) dgv.Columns[primaryKeyColumn].Visible = false; };

            // --- ФИКС №2: ИСКЛЮЧАЕМ DBNULL ПРИ ДОБАВЛЕНИИ НОВЫХ ЗАПИСЕЙ ---
            dgv.DefaultValuesNeeded += (s, e) =>
            {
                foreach (DataGridViewColumn col in dgv.Columns)
                {
                    if (col is DataGridViewComboBoxColumn cmb)
                    {
                        // Если это список из базы данных, берем первый элемент
                        if (cmb.DataSource is DataTable lookup && lookup.Rows.Count > 0)
                            e.Row.Cells[col.Index].Value = lookup.Rows[0][cmb.ValueMember];
                        // Если статический список (например, дни недели)
                        else if (cmb.Items.Count > 0)
                            e.Row.Cells[col.Index].Value = cmb.Items[0];
                    }
                    else if (dt.Columns.Contains(col.DataPropertyName))
                    {
                        // Для галочек ставим false, для чисел ставим 0
                        Type type = dt.Columns[col.DataPropertyName].DataType;
                        if (type == typeof(bool)) e.Row.Cells[col.Index].Value = false;
                        else if (type == typeof(int) || type == typeof(short)) e.Row.Cells[col.Index].Value = 0;
                    }
                }
            };

            tab.Controls.Add(dgv);
            tabControl.TabPages.Add(tab);

            _tablesMap[tab] = new TableContext { Table = dt, Adapter = da };
            return tab;
        }

        // --- МАГИЯ КОЛЕСИКА МЫШИ ---
        private void Dgv_MouseWheel(object sender, MouseEventArgs e)
        {
            DataGridView dgv = sender as DataGridView;
            if (dgv == null) return;

            // Определяем, над какой ячейкой сейчас находится курсор мыши
            var hit = dgv.HitTest(e.X, e.Y);
            if (hit.Type == DataGridViewHitTestType.Cell && hit.RowIndex >= 0 && hit.ColumnIndex >= 0)
            {
                var cell = dgv.Rows[hit.RowIndex].Cells[hit.ColumnIndex];

                // Запрещаем менять ID или пустую строку добавления
                if (cell.ReadOnly || dgv.Rows[hit.RowIndex].IsNewRow) return;

                // Переводим фокус на ячейку под мышкой
                dgv.CurrentCell = cell;

                // Блокируем стандартную вертикальную прокрутку всей таблицы
                if (e is HandledMouseEventArgs handledArgs) handledArgs.Handled = true;

                int step = e.Delta > 0 ? 1 : -1; // Куда крутим: вверх (1) или вниз (-1)
                string colName = dgv.Columns[hit.ColumnIndex].Name;
                string[] numericCols = { "Capacity", "MaxLectureGroups", "MaxPracticeGroups", "StudentCount", "YearLearn" };

                // 1. ЕСЛИ ЭТО ЧИСЛО -> Увеличиваем/Уменьшаем
                if (Array.Exists(numericCols, c => c == colName))
                {
                    int currentVal = 0;
                    if (cell.Value != DBNull.Value && cell.Value != null)
                        int.TryParse(cell.Value.ToString(), out currentVal);

                    int newVal = currentVal + step;
                    if (newVal < 0) newVal = 0; // Защита от отрицательных чисел

                    cell.Value = newVal;
                    dgv.NotifyCurrentCellDirty(true);
                }
                // 2. ЕСЛИ ЭТО ГАЛОЧКА (Логический тип) -> Переключаем
                else if (cell.ValueType == typeof(bool) || cell is DataGridViewCheckBoxCell)
                {
                    bool currentVal = false;
                    if (cell.Value != DBNull.Value && cell.Value != null)
                        bool.TryParse(cell.Value.ToString(), out currentVal);

                    cell.Value = !currentVal;
                    dgv.NotifyCurrentCellDirty(true);
                }
                // 3. ЕСЛИ ЭТО ВЫПАДАЮЩИЙ СПИСОК -> Перебираем элементы
                else if (dgv.Columns[hit.ColumnIndex] is DataGridViewComboBoxColumn cmb)
                {
                    // Если данные берутся из БД (Аудитории, Преподаватели)
                    if (cmb.DataSource is DataTable dtLookup && dtLookup.Rows.Count > 0)
                    {
                        int currentIndex = -1;
                        if (cell.Value != DBNull.Value && cell.Value != null)
                        {
                            for (int i = 0; i < dtLookup.Rows.Count; i++)
                            {
                                if (dtLookup.Rows[i][cmb.ValueMember].Equals(cell.Value))
                                {
                                    currentIndex = i; break;
                                }
                            }
                        }

                        int newIndex = currentIndex - step; // Крутим вниз = следующий элемент
                        if (newIndex >= dtLookup.Rows.Count) newIndex = 0;
                        if (newIndex < 0) newIndex = dtLookup.Rows.Count - 1;

                        cell.Value = dtLookup.Rows[newIndex][cmb.ValueMember];
                        dgv.NotifyCurrentCellDirty(true);
                    }
                    // Если это статический список (Дни недели, Пары)
                    else if (cmb.Items.Count > 0)
                    {
                        int currentIndex = cmb.Items.IndexOf(cell.Value);
                        int newIndex = currentIndex - step;

                        if (newIndex >= cmb.Items.Count) newIndex = 0;
                        if (newIndex < 0) newIndex = cmb.Items.Count - 1;

                        cell.Value = cmb.Items[newIndex];
                        dgv.NotifyCurrentCellDirty(true);
                    }
                }
            }
        }
        private void TabControl_DrawItem(object sender, DrawItemEventArgs e)
        {
            TabPage page = tabControl.TabPages[e.Index];
            bool hasChanges = _tablesMap.ContainsKey(page) && _tablesMap[page].Table.GetChanges() != null;

            e.Graphics.FillRectangle(new SolidBrush(page.BackColor), e.Bounds);
            Brush textBrush = hasChanges ? Brushes.Red : Brushes.Black;
            StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            e.Graphics.DrawString(hasChanges ? page.Text + " *" : page.Text, e.Font, textBrush, e.Bounds, sf);
        }

        private void TabControl_Deselecting(object sender, TabControlCancelEventArgs e)
        {
            if (_isClosing || e.TabPage == null) return;
            var context = _tablesMap[e.TabPage];
            if (context.Table.GetChanges() != null)
            {
                var result = MessageBox.Show($"Данные в таблице '{e.TabPage.Text}' были изменены.\n\nДа - Сохранить\nНет - Отменить изменения\nОтмена - Игнорировать (останется красной)", "Несохраненные изменения", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
                if (result == DialogResult.Yes) SaveTable(context);
                else if (result == DialogResult.No) { context.Table.RejectChanges(); tabControl.Invalidate(); }
            }
        }

        private void FormDictionaries_FormClosing(object sender, FormClosingEventArgs e)
        {
            _isClosing = true;
            bool hasAnyChanges = false;
            foreach (var ctx in _tablesMap.Values) if (ctx.Table.GetChanges() != null) hasAnyChanges = true;

            if (hasAnyChanges)
            {
                var result = MessageBox.Show("Есть несохраненные таблицы!\n\nДа - Сохранить всё и выйти\nНет - Отменить всё и выйти\nОтмена - Вернуться к редактированию", "Внимание", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Exclamation);
                if (result == DialogResult.Yes) { ForceEndEdit(); foreach (var ctx in _tablesMap.Values) SaveTable(ctx, silent: true); DataChanged = true; }
                else if (result == DialogResult.Cancel) { e.Cancel = true; _isClosing = false; }
            }
        }

        private void Dgv_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            DataGridView dgv = sender as DataGridView;
            string colName = dgv.Columns[e.ColumnIndex].Name;
            string[] numericCols = { "Capacity", "MaxLectureGroups", "MaxPracticeGroups", "StudentCount", "YearLearn" };

            if (Array.Exists(numericCols, c => c == colName))
            {
                if (int.TryParse(e.FormattedValue.ToString(), out int val) && val < 0)
                {
                    MessageBox.Show("Значение не может быть меньше 0. Установлен 0.", "Ограничение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    dgv.EditingControl.Text = "0";
                }
            }
        }

        private void SaveCurrentTab()
        {
            ForceEndEdit();
            if (tabControl.SelectedTab != null && _tablesMap.TryGetValue(tabControl.SelectedTab, out var context)) SaveTable(context);
        }

        private void RevertCurrentTab()
        {
            ForceEndEdit();
            if (tabControl.SelectedTab != null && _tablesMap.TryGetValue(tabControl.SelectedTab, out var context))
            {
                context.Table.RejectChanges();
                tabControl.Invalidate();
            }
        }

        private void SaveTable(TableContext ctx, bool silent = false)
        {
            try
            {
                if (ctx.Table.GetChanges() != null)
                {
                    ctx.Adapter.Update(ctx.Table);
                    ctx.Table.AcceptChanges();
                    DataChanged = true;
                    tabControl.Invalidate();
                    if (!silent) MessageBox.Show("Таблица успешно сохранена!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка сохранения: " + ex.Message, "Ошибка БД", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ForceEndEdit()
        {
            this.Validate();
            var grid = GetActiveGrid();
            if (grid != null) { if (grid.IsCurrentCellInEditMode) grid.CommitEdit(DataGridViewDataErrorContexts.Commit); grid.EndEdit(); }
        }

        private DataGridView GetGrid(TabPage tab) => tab.Controls.Count > 0 ? tab.Controls[0] as DataGridView : null;
        private DataGridView GetActiveGrid() => tabControl.SelectedTab?.Controls[0] as DataGridView;

        private void ReplaceWithComboBox(DataGridView dgv, string columnName, DataTable lookupTable, string valueMember, string displayMember, string headerText)
        {
            if (dgv.Columns.Contains(columnName))
            {
                int columnIndex = dgv.Columns[columnName].Index;
                dgv.Columns.Remove(columnName);
                dgv.Columns.Insert(columnIndex, new DataGridViewComboBoxColumn { Name = columnName, DataPropertyName = columnName, DataSource = lookupTable, ValueMember = valueMember, DisplayMember = displayMember, HeaderText = headerText, DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing, FlatStyle = FlatStyle.Flat });
            }
        }

        private void ReplaceWithStaticComboBox(DataGridView dgv, string columnName, string[] items, string headerText)
        {
            if (dgv.Columns.Contains(columnName))
            {
                int columnIndex = dgv.Columns[columnName].Index;
                dgv.Columns.Remove(columnName);
                var cmb = new DataGridViewComboBoxColumn { Name = columnName, DataPropertyName = columnName, HeaderText = headerText, DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing, FlatStyle = FlatStyle.Flat };
                cmb.Items.AddRange(items);
                dgv.Columns.Insert(columnIndex, cmb);
            }
        }

        private void RenameColumns(DataGridView dgv, Dictionary<string, string> map)
        {
            foreach (var kvp in map) if (dgv.Columns.Contains(kvp.Key)) dgv.Columns[kvp.Key].HeaderText = kvp.Value;
        }
    }
}