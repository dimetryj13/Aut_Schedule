using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.Windows.Forms;

namespace AutoSchedule
{
    public class FormTeacherEditor : Form
    {
        private string _connectionString;
        private int _currentTeacherId = -1;
        private string _selectedRoomNum = null;

        // Элементы интерфейса
        private ListBox lstTeachers;
        private TextBox txtFullName;
        private ComboBox cmbDepartment;
        private NumericUpDown numLectureGroups, numPracticeGroups;

        // Матрица доступности
        private CheckBox[,] chkAvailability = new CheckBox[6, 6];

        // Аудитории и приоритеты
        private FlowLayoutPanel flpRooms;
        private FlowLayoutPanel flpPriority;

        // Хранилища данных
        private List<string> _allRooms = new List<string>();
        private Dictionary<string, int> _teacherRoomPriorities = new Dictionary<string, int>();
        private Dictionary<string, Button> _roomButtons = new Dictionary<string, Button>();

        private Button btnSave, btnAdd, btnDelete;
        private Label lblStatus;

        public FormTeacherEditor(string connectionString)
        {
            _connectionString = connectionString;
            this.Text = "Профиль преподавателя (Матрица доступности и Приоритеты)";
            this.Size = new Size(1100, 750);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            LoadRoomsDict();
            SetupUI();
            LoadTeachersList();
        }

        // --- МЕТОДЫ-ПРЕДОХРАНИТЕЛИ ОТ ПУСТЫХ ЯЧЕЕК (DBNull) ---
        private string SafeGetString(object val) => val != DBNull.Value && val != null ? val.ToString().Trim() : "";
        private int SafeGetInt(object val, int defaultVal = 0) => val != DBNull.Value && val != null ? Convert.ToInt32(val) : defaultVal;
        private bool SafeGetBool(object val) => val != DBNull.Value && val != null ? Convert.ToBoolean(val) : false;

        private void SetupUI()
        {
            TableLayoutPanel tlpMain = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
            tlpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 250F));
            tlpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            this.Controls.Add(tlpMain);

            lstTeachers = new ListBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 11), DisplayMember = "Name", ValueMember = "ID", Margin = new Padding(5) };
            lstTeachers.SelectedIndexChanged += LstTeachers_SelectedIndexChanged;
            tlpMain.Controls.Add(lstTeachers, 0, 0);

            TableLayoutPanel tlpEditor = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 4, ColumnCount = 1 };
            tlpEditor.RowStyles.Add(new RowStyle(SizeType.Absolute, 100F));
            tlpEditor.RowStyles.Add(new RowStyle(SizeType.Absolute, 230F));
            tlpEditor.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpEditor.RowStyles.Add(new RowStyle(SizeType.Absolute, 60F));
            tlpMain.Controls.Add(tlpEditor, 1, 0);

            // БЛОК 1: ОСНОВНЫЕ ДАННЫЕ
            GroupBox gbBasic = new GroupBox { Text = "👤 Личные данные", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            TableLayoutPanel tlpBasic = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 2 };

            tlpBasic.Controls.Add(new Label { Text = "ФИО преподавателя:", AutoSize = true, Anchor = AnchorStyles.Bottom | AnchorStyles.Left }, 0, 0);
            txtFullName = new TextBox { Width = 250, Font = new Font("Segoe UI", 11) };
            tlpBasic.Controls.Add(txtFullName, 0, 1);

            tlpBasic.Controls.Add(new Label { Text = "Кафедра:", AutoSize = true, Anchor = AnchorStyles.Bottom | AnchorStyles.Left }, 1, 0);
            cmbDepartment = new ComboBox { Width = 200, Font = new Font("Segoe UI", 11), FlatStyle = FlatStyle.Flat };
            tlpBasic.Controls.Add(cmbDepartment, 1, 1);

            tlpBasic.Controls.Add(new Label { Text = "Макс. Лекций:", AutoSize = true, Anchor = AnchorStyles.Bottom | AnchorStyles.Left }, 2, 0);
            numLectureGroups = new NumericUpDown { Width = 60, Maximum = 50, Font = new Font("Segoe UI", 11) };
            tlpBasic.Controls.Add(numLectureGroups, 2, 1);

            tlpBasic.Controls.Add(new Label { Text = "Макс. Практик:", AutoSize = true, Anchor = AnchorStyles.Bottom | AnchorStyles.Left }, 3, 0);
            numPracticeGroups = new NumericUpDown { Width = 60, Maximum = 50, Font = new Font("Segoe UI", 11) };
            tlpBasic.Controls.Add(numPracticeGroups, 3, 1);

            gbBasic.Controls.Add(tlpBasic);
            tlpEditor.Controls.Add(gbBasic, 0, 0);

            // БЛОК 2: МАТРИЦА ДОСТУПНОСТИ
            GroupBox gbMatrix = new GroupBox { Text = "📅 Матрица доступности (Дни и Пары)", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            TableLayoutPanel tlpMatrix = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 7, ColumnCount = 7, CellBorderStyle = TableLayoutPanelCellBorderStyle.Single };

            tlpMatrix.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70F));
            for (int i = 0; i < 6; i++) tlpMatrix.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16.6F));
            tlpMatrix.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            for (int i = 0; i < 6; i++) tlpMatrix.RowStyles.Add(new RowStyle(SizeType.Percent, 16.6F));

            string[] days = { "Пн", "Вт", "Ср", "Чт", "Пт", "Сб" };
            for (int d = 0; d < 6; d++) tlpMatrix.Controls.Add(new Label { Text = days[d], Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter }, d + 1, 0);

            for (int p = 0; p < 6; p++)
            {
                tlpMatrix.Controls.Add(new Label { Text = $"Пара {p + 1}", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter }, 0, p + 1);
                for (int d = 0; d < 6; d++)
                {
                    CheckBox cb = new CheckBox { CheckAlign = ContentAlignment.MiddleCenter, Dock = DockStyle.Fill, Cursor = Cursors.Hand };
                    chkAvailability[d, p] = cb;
                    tlpMatrix.Controls.Add(cb, d + 1, p + 1);
                }
            }
            gbMatrix.Controls.Add(tlpMatrix);
            tlpEditor.Controls.Add(gbMatrix, 0, 1);

            // БЛОК 3: ПАНЕЛИ АУДИТОРИЙ И ПРИОРИТЕТОВ
            GroupBox gbPrefs = new GroupBox { Text = "🏫 Предпочтения аудиторий", Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            TableLayoutPanel tlpRooms = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
            tlpRooms.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65F));
            tlpRooms.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));

            flpRooms = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(5) };
            tlpRooms.Controls.Add(flpRooms, 0, 0);

            flpPriority = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(10) };
            flpPriority.Controls.Add(new Label { Text = "Выберите аудиторию слева\nи нажмите на уровень:", AutoSize = true, Margin = new Padding(0, 0, 0, 10) });

            string[] prioNames = { "0 - Отсутствует", "1 - Низкий", "2 - Средний", "3 - Высокий", "4 - Наивысший" };
            Color[] prioColors = { Color.WhiteSmoke, Color.LightGreen, Color.MediumSeaGreen, Color.SeaGreen, Color.DarkGreen };
            Color[] textColors = { Color.Black, Color.Black, Color.White, Color.White, Color.White };

            for (int i = 0; i < 5; i++)
            {
                int prioLevel = i;
                Button btnP = new Button { Text = prioNames[i], Width = 190, Height = 35, BackColor = prioColors[i], ForeColor = textColors[i], FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
                btnP.Click += (s, e) => SetPriorityForSelectedRoom(prioLevel);
                flpPriority.Controls.Add(btnP);
            }
            tlpRooms.Controls.Add(flpPriority, 1, 0);
            gbPrefs.Controls.Add(tlpRooms);
            tlpEditor.Controls.Add(gbPrefs, 0, 2);

            // БЛОК 4: КНОПКИ
            FlowLayoutPanel flpButtons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
            btnSave = new Button { Text = "💾 Сохранить", Width = 180, Height = 45, BackColor = Color.FromArgb(0, 122, 204), ForeColor = Color.White, Font = new Font("Segoe UI", 10, FontStyle.Bold), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnSave.Click += BtnSave_Click;

            btnAdd = new Button { Text = "➕ Добавить", Width = 150, Height = 45, Font = new Font("Segoe UI", 10, FontStyle.Bold), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnAdd.Click += BtnAdd_Click;

            btnDelete = new Button { Text = "🗑 Удалить", Width = 150, Height = 45, ForeColor = Color.Crimson, Font = new Font("Segoe UI", 10, FontStyle.Bold), FlatStyle = FlatStyle.Flat, Margin = new Padding(30, 0, 0, 0), Cursor = Cursors.Hand };
            btnDelete.Click += BtnDelete_Click;

            lblStatus = new Label { AutoSize = true, ForeColor = Color.Gray, Padding = new Padding(10, 15, 0, 0) };

            flpButtons.Controls.AddRange(new Control[] { btnSave, btnAdd, btnDelete, lblStatus });
            tlpEditor.Controls.Add(flpButtons, 0, 3);
        }

        private void LoadRoomsDict()
        {
            try
            {
                using (var conn = new OleDbConnection(_connectionString))
                {
                    conn.Open();
                    using (var cmd = new OleDbCommand("SELECT RoomNumber FROM Classroom ORDER BY RoomNumber", conn))
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            string rn = SafeGetString(r["RoomNumber"]);
                            if (!string.IsNullOrEmpty(rn)) _allRooms.Add(rn);
                        }
                    }
                }
            }
            catch (Exception) { /* Игнорируем ошибку при пустой БД */ }
        }

        private void GenerateRoomButtons()
        {
            flpRooms.Controls.Clear();
            _roomButtons.Clear();

            foreach (var roomNum in _allRooms)
            {
                int currentPriority = _teacherRoomPriorities.ContainsKey(roomNum) ? _teacherRoomPriorities[roomNum] : 0;

                Button btnRoom = new Button
                {
                    Width = 80,
                    Height = 80,
                    FlatStyle = FlatStyle.Flat,
                    Cursor = Cursors.Hand,
                    Tag = roomNum
                };

                UpdateRoomButtonVisual(btnRoom, roomNum, currentPriority);

                btnRoom.Click += (s, e) =>
                {
                    _selectedRoomNum = roomNum;

                    foreach (var b in _roomButtons.Values) { b.FlatAppearance.BorderSize = 1; b.FlatAppearance.BorderColor = Color.Gray; }
                    btnRoom.FlatAppearance.BorderSize = 3;
                    btnRoom.FlatAppearance.BorderColor = Color.Blue;
                };

                _roomButtons[roomNum] = btnRoom;
                flpRooms.Controls.Add(btnRoom);
            }
        }

        private void UpdateRoomButtonVisual(Button btn, string roomNum, int priority)
        {
            btn.Text = $"Ауд.\n{roomNum}";
            if (priority > 0) btn.Text += $"\n(П: {priority})";

            Color[] bgColors = { Color.WhiteSmoke, Color.LightGreen, Color.MediumSeaGreen, Color.SeaGreen, Color.DarkGreen };
            Color[] fgColors = { Color.Black, Color.Black, Color.White, Color.White, Color.White };

            btn.BackColor = bgColors[priority];
            btn.ForeColor = fgColors[priority];
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = Color.Gray;
        }

        private void SetPriorityForSelectedRoom(int priority)
        {
            if (string.IsNullOrEmpty(_selectedRoomNum))
            {
                MessageBox.Show("Сначала выберите аудиторию слева!", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _teacherRoomPriorities[_selectedRoomNum] = priority;
            UpdateRoomButtonVisual(_roomButtons[_selectedRoomNum], _selectedRoomNum, priority);
            _roomButtons[_selectedRoomNum].FlatAppearance.BorderSize = 3;
            _roomButtons[_selectedRoomNum].FlatAppearance.BorderColor = Color.Blue;
        }

        private void LoadTeachersList()
        {
            lstTeachers.Items.Clear();
            HashSet<string> departments = new HashSet<string>();

            try
            {
                using (var conn = new OleDbConnection(_connectionString))
                {
                    conn.Open();
                    using (var cmd = new OleDbCommand("SELECT TeacherID, FullName, Department FROM Teachers ORDER BY FullName", conn))
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            int id = Convert.ToInt32(r["TeacherID"]);
                            string fullName = SafeGetString(r["FullName"]);

                            // Защита от пустых имен в списке
                            if (string.IsNullOrWhiteSpace(fullName)) fullName = $"[Без имени] ID: {id}";

                            lstTeachers.Items.Add(new { ID = id, Name = fullName });

                            string dept = SafeGetString(r["Department"]);
                            if (!string.IsNullOrWhiteSpace(dept)) departments.Add(dept);
                        }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Ошибка БД: " + ex.Message); }

            cmbDepartment.Items.Clear();
            foreach (var dep in departments) cmbDepartment.Items.Add(dep);

            if (lstTeachers.Items.Count > 0) lstTeachers.SelectedIndex = 0;
            else ClearAllFields();
        }

        private void LstTeachers_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstTeachers.SelectedIndex == -1) return;

            _currentTeacherId = ((dynamic)lstTeachers.SelectedItem).ID;
            lblStatus.Text = $"Редактирование: ID {_currentTeacherId}";
            _selectedRoomNum = null;

            try
            {
                using (var conn = new OleDbConnection(_connectionString))
                {
                    conn.Open();

                    // 1. Личные данные (БЕЗОПАСНОЕ ЧТЕНИЕ)
                    using (var cmd = new OleDbCommand("SELECT * FROM Teachers WHERE TeacherID = ?", conn))
                    {
                        cmd.Parameters.AddWithValue("?", _currentTeacherId);
                        using (var r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                txtFullName.Text = SafeGetString(r["FullName"]);
                                cmbDepartment.Text = SafeGetString(r["Department"]);
                                numLectureGroups.Value = SafeGetInt(r["MaxLectureGroups"], 1); // 1 по умолчанию
                                numPracticeGroups.Value = SafeGetInt(r["MaxPracticeGroups"], 1);
                            }
                        }
                    }

                    // 2. Матрица доступности
                    for (int d = 0; d < 6; d++) for (int p = 0; p < 6; p++) chkAvailability[d, p].Checked = false;

                    using (var cmd = new OleDbCommand("SELECT DayIdx, PairIdx, IsAvailable FROM TeacherAvailability WHERE TeacherID = ?", conn))
                    {
                        cmd.Parameters.AddWithValue("?", _currentTeacherId);
                        using (var r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                string dStr = SafeGetString(r["DayIdx"]);
                                string pStr = SafeGetString(r["PairIdx"]);

                                int dIdx = -1;
                                if (int.TryParse(dStr, out int dParsed)) dIdx = dParsed - 1;
                                else if (dStr.StartsWith("Пн")) dIdx = 0;
                                else if (dStr.StartsWith("Вт")) dIdx = 1;
                                else if (dStr.StartsWith("Ср")) dIdx = 2;
                                else if (dStr.StartsWith("Чт")) dIdx = 3;
                                else if (dStr.StartsWith("Пт")) dIdx = 4;
                                else if (dStr.StartsWith("Сб")) dIdx = 5;

                                int pIdx = -1;
                                if (int.TryParse(pStr, out int pParsed)) pIdx = pParsed - 1;

                                if (dIdx >= 0 && dIdx < 6 && pIdx >= 0 && pIdx < 6)
                                {
                                    chkAvailability[dIdx, pIdx].Checked = SafeGetBool(r["IsAvailable"]);
                                }
                            }
                        }
                    }

                    // 3. Приоритеты аудиторий
                    _teacherRoomPriorities.Clear();
                    using (var cmd = new OleDbCommand("SELECT RoomNumber, Priority FROM TeacherRoomPrefs WHERE TeacherID = ?", conn))
                    {
                        cmd.Parameters.AddWithValue("?", _currentTeacherId);
                        using (var r = cmd.ExecuteReader())
                        {
                            while (r.Read())
                            {
                                string rNum = SafeGetString(r["RoomNumber"]);
                                if (!string.IsNullOrEmpty(rNum))
                                {
                                    _teacherRoomPriorities[rNum] = SafeGetInt(r["Priority"], 0);
                                }
                            }
                        }
                    }

                    GenerateRoomButtons();
                }
            }
            catch (Exception ex) { MessageBox.Show("Ошибка чтения данных: " + ex.Message); }
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            lstTeachers.SelectedIndex = -1;
            ClearAllFields();
            _currentTeacherId = -1;
            lblStatus.Text = "Добавление нового преподавателя";
            txtFullName.Focus();
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtFullName.Text)) { MessageBox.Show("Введите ФИО!"); return; }

            try
            {
                using (var conn = new OleDbConnection(_connectionString))
                {
                    conn.Open();

                    if (_currentTeacherId == -1)
                    {
                        string sql = "INSERT INTO Teachers (FullName, Department, MaxLectureGroups, MaxPracticeGroups) VALUES (?, ?, ?, ?)";
                        using (var cmd = new OleDbCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("?", txtFullName.Text.Trim());
                            cmd.Parameters.AddWithValue("?", cmbDepartment.Text.Trim());
                            cmd.Parameters.AddWithValue("?", (int)numLectureGroups.Value);
                            cmd.Parameters.AddWithValue("?", (int)numPracticeGroups.Value);
                            cmd.ExecuteNonQuery();
                        }
                        using (var cmdId = new OleDbCommand("SELECT @@IDENTITY", conn)) _currentTeacherId = Convert.ToInt32(cmdId.ExecuteScalar());
                    }
                    else
                    {
                        string sql = "UPDATE Teachers SET FullName=?, Department=?, MaxLectureGroups=?, MaxPracticeGroups=? WHERE TeacherID=?";
                        using (var cmd = new OleDbCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("?", txtFullName.Text.Trim());
                            cmd.Parameters.AddWithValue("?", cmbDepartment.Text.Trim());
                            cmd.Parameters.AddWithValue("?", (int)numLectureGroups.Value);
                            cmd.Parameters.AddWithValue("?", (int)numPracticeGroups.Value);
                            cmd.Parameters.AddWithValue("?", _currentTeacherId);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    using (var cmdDel = new OleDbCommand("DELETE FROM TeacherAvailability WHERE TeacherID = ?", conn))
                    {
                        cmdDel.Parameters.AddWithValue("?", _currentTeacherId);
                        cmdDel.ExecuteNonQuery();
                    }
                    using (var cmdIns = new OleDbCommand("INSERT INTO TeacherAvailability (TeacherID, DayIdx, PairIdx, IsAvailable) VALUES (?, ?, ?, ?)", conn))
                    {
                        for (int d = 0; d < 6; d++)
                        {
                            for (int p = 0; p < 6; p++)
                            {
                                cmdIns.Parameters.Clear();
                                cmdIns.Parameters.AddWithValue("?", _currentTeacherId);
                                cmdIns.Parameters.AddWithValue("?", (d + 1).ToString());
                                cmdIns.Parameters.AddWithValue("?", (p + 1).ToString());
                                cmdIns.Parameters.AddWithValue("?", chkAvailability[d, p].Checked);
                                cmdIns.ExecuteNonQuery();
                            }
                        }
                    }

                    using (var cmdDel = new OleDbCommand("DELETE FROM TeacherRoomPrefs WHERE TeacherID = ?", conn))
                    {
                        cmdDel.Parameters.AddWithValue("?", _currentTeacherId);
                        cmdDel.ExecuteNonQuery();
                    }
                    using (var cmdIns = new OleDbCommand("INSERT INTO TeacherRoomPrefs (TeacherID, RoomNumber, Priority) VALUES (?, ?, ?)", conn))
                    {
                        foreach (var kvp in _teacherRoomPriorities)
                        {
                            if (kvp.Value > 0)
                            {
                                cmdIns.Parameters.Clear();
                                cmdIns.Parameters.AddWithValue("?", _currentTeacherId);
                                cmdIns.Parameters.AddWithValue("?", kvp.Key);
                                cmdIns.Parameters.AddWithValue("?", kvp.Value);
                                cmdIns.ExecuteNonQuery();
                            }
                        }
                    }
                }

                LoadTeachersList();
                MessageBox.Show("Данные успешно сохранены!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { MessageBox.Show("Ошибка БД: " + ex.Message); }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (_currentTeacherId == -1) return;
            if (MessageBox.Show("Удалить преподавателя?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                try
                {
                    using (var conn = new OleDbConnection(_connectionString))
                    {
                        conn.Open();
                        new OleDbCommand($"DELETE FROM TeacherAvailability WHERE TeacherID = {_currentTeacherId}", conn).ExecuteNonQuery();
                        new OleDbCommand($"DELETE FROM TeacherRoomPrefs WHERE TeacherID = {_currentTeacherId}", conn).ExecuteNonQuery();
                        new OleDbCommand($"DELETE FROM Teachers WHERE TeacherID = {_currentTeacherId}", conn).ExecuteNonQuery();
                    }
                    LoadTeachersList();
                }
                catch (Exception ex) { MessageBox.Show("Ошибка: " + ex.Message); }
            }
        }

        private void ClearAllFields()
        {
            txtFullName.Text = ""; cmbDepartment.Text = ""; numLectureGroups.Value = 1; numPracticeGroups.Value = 1;
            for (int d = 0; d < 6; d++) for (int p = 0; p < 6; p++) chkAvailability[d, p].Checked = false;
            _teacherRoomPriorities.Clear();
            _selectedRoomNum = null;
            GenerateRoomButtons();
        }
    }
}