using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.Windows.Forms;

namespace AutoSchedule
{
    public class FormGroupEditor : Form
    {
        private string _connectionString;
        private int _currentGroupId = -1;

        // Элементы интерфейса
        private ListBox lstGroups;

        // Поля редактирования
        private TextBox txtGroupName;
        private NumericUpDown numStudentCount;
        private NumericUpDown numYearLearn;
        private CheckBox chkIsFullTime;
        private CheckBox chkActually;
        private ComboBox cmbMainTeacher;

        // Класс для хранения преподавателей в ComboBox
        private class TeacherItem
        {
            public int TeacherID { get; set; }
            public string FullName { get; set; }
            public override string ToString() => FullName;
        }

        private Button btnSave, btnAdd, btnDelete;
        private Label lblStatus;

        public FormGroupEditor(string connectionString)
        {
            _connectionString = connectionString;
            this.Text = "Редактор справочника: Группы";
            this.Size = new Size(680, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            // ИСПРАВЛЕНИЕ: Сначала создаем элементы интерфейса, потом загружаем в них данные!
            SetupUI();
            LoadTeachers();
            LoadGroupsList();
        }

        // --- БЕЗОПАСНОЕ ЧТЕНИЕ (Защита от DBNull) ---
        private string SafeGetString(object val) => val != DBNull.Value && val != null ? val.ToString().Trim() : "";
        private int SafeGetInt(object val, int defaultVal = 0) => val != DBNull.Value && val != null ? Convert.ToInt32(val) : defaultVal;
        private bool SafeGetBool(object val) => val != DBNull.Value && val != null ? Convert.ToBoolean(val) : false;

        private void SetupUI()
        {
            // --- ЛЕВАЯ ПАНЕЛЬ (Список) ---
            lstGroups = new ListBox
            {
                Location = new Point(10, 10),
                Size = new Size(240, 440),
                Font = new Font("Segoe UI", 11),
                DisplayMember = "Name",
                ValueMember = "ID"
            };
            lstGroups.SelectedIndexChanged += LstGroups_SelectedIndexChanged;
            this.Controls.Add(lstGroups);

            // --- ПРАВАЯ ПАНЕЛЬ (Поля ввода) ---
            int startX = 270;
            int width = 370;
            int y = 20;

            this.Controls.Add(new Label { Text = "Название группы (шифр):", Location = new Point(startX, y), AutoSize = true, Font = new Font("Segoe UI", 10) });
            txtGroupName = new TextBox { Location = new Point(startX, y + 25), Size = new Size(width, 27), Font = new Font("Segoe UI", 11) };
            this.Controls.Add(txtGroupName);

            y += 70;
            this.Controls.Add(new Label { Text = "Курс обучения:", Location = new Point(startX, y), AutoSize = true, Font = new Font("Segoe UI", 10) });
            numYearLearn = new NumericUpDown { Location = new Point(startX, y + 25), Size = new Size(120, 27), Minimum = 1, Maximum = 6, Value = 1, Font = new Font("Segoe UI", 11) };
            this.Controls.Add(numYearLearn);

            this.Controls.Add(new Label { Text = "Кол-во студентов:", Location = new Point(startX + 180, y), AutoSize = true, Font = new Font("Segoe UI", 10) });
            numStudentCount = new NumericUpDown { Location = new Point(startX + 180, y + 25), Size = new Size(120, 27), Minimum = 1, Maximum = 100, Value = 25, Font = new Font("Segoe UI", 11) };
            this.Controls.Add(numStudentCount);

            y += 70;
            this.Controls.Add(new Label { Text = "Куратор (Main Teacher):", Location = new Point(startX, y), AutoSize = true, Font = new Font("Segoe UI", 10) });
            cmbMainTeacher = new ComboBox { Location = new Point(startX, y + 25), Size = new Size(width, 27), Font = new Font("Segoe UI", 11), DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat };
            this.Controls.Add(cmbMainTeacher);

            y += 70;
            chkIsFullTime = new CheckBox { Text = "Очная форма обучения", Location = new Point(startX, y), AutoSize = true, Font = new Font("Segoe UI", 11), Cursor = Cursors.Hand };
            this.Controls.Add(chkIsFullTime);

            chkActually = new CheckBox { Text = "Актуальна (действующая)", Location = new Point(startX + 210, y), AutoSize = true, Font = new Font("Segoe UI", 11), Cursor = Cursors.Hand, Checked = true };
            this.Controls.Add(chkActually);

            y += 60;
            // --- КНОПКИ УПРАВЛЕНИЯ ---
            btnSave = new Button { Text = "💾 Сохранить", Location = new Point(startX, y), Size = new Size(180, 45), BackColor = Color.FromArgb(0, 122, 204), ForeColor = Color.White, Font = new Font("Segoe UI", 10, FontStyle.Bold), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnSave.Click += BtnSave_Click;
            this.Controls.Add(btnSave);

            btnAdd = new Button { Text = "➕ Добавить", Location = new Point(startX + 190, y), Size = new Size(180, 45), Font = new Font("Segoe UI", 10, FontStyle.Bold), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnAdd.Click += BtnAdd_Click;
            this.Controls.Add(btnAdd);

            y += 55;
            btnDelete = new Button { Text = "🗑 Удалить", Location = new Point(startX, y), Size = new Size(370, 35), ForeColor = Color.Crimson, Font = new Font("Segoe UI", 10, FontStyle.Bold), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnDelete.Click += BtnDelete_Click;
            this.Controls.Add(btnDelete);

            lblStatus = new Label { Location = new Point(startX, y + 40), AutoSize = true, ForeColor = Color.Gray, Font = new Font("Segoe UI", 9) };
            this.Controls.Add(lblStatus);
        }

        private void LoadTeachers()
        {
            cmbMainTeacher.Items.Clear();
            cmbMainTeacher.Items.Add(new TeacherItem { TeacherID = -1, FullName = "— Не назначен —" });

            try
            {
                using (var conn = new OleDbConnection(_connectionString))
                {
                    conn.Open();
                    using (var cmd = new OleDbCommand("SELECT TeacherID, FullName FROM Teachers ORDER BY FullName", conn))
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            cmbMainTeacher.Items.Add(new TeacherItem
                            {
                                TeacherID = Convert.ToInt32(r["TeacherID"]),
                                FullName = SafeGetString(r["FullName"])
                            });
                        }
                    }
                }
            }
            catch (Exception) { /* Игнорируем ошибку при пустой БД */ }
        }

        private void LoadGroupsList()
        {
            lstGroups.Items.Clear();
            try
            {
                using (var conn = new OleDbConnection(_connectionString))
                {
                    conn.Open();
                    using (var cmd = new OleDbCommand("SELECT GroupId, GroupName FROM GroupsList ORDER BY GroupName", conn))
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            int id = Convert.ToInt32(r["GroupId"]);
                            string name = SafeGetString(r["GroupName"]);
                            if (string.IsNullOrWhiteSpace(name)) name = $"[Без названия] ID: {id}";

                            lstGroups.Items.Add(new { ID = id, Name = name });
                        }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Ошибка загрузки групп: " + ex.Message); }

            if (lstGroups.Items.Count > 0) lstGroups.SelectedIndex = 0;
            else ClearFields();
        }

        private void LstGroups_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstGroups.SelectedIndex == -1) return;

            _currentGroupId = ((dynamic)lstGroups.SelectedItem).ID;
            lblStatus.Text = $"Редактирование: ID {_currentGroupId}";

            try
            {
                using (var conn = new OleDbConnection(_connectionString))
                {
                    conn.Open();
                    using (var cmd = new OleDbCommand("SELECT * FROM GroupsList WHERE GroupId = ?", conn))
                    {
                        cmd.Parameters.AddWithValue("?", _currentGroupId);
                        using (var r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                txtGroupName.Text = SafeGetString(r["GroupName"]);
                                numStudentCount.Value = SafeGetInt(r["StudentCount"], 25);
                                numYearLearn.Value = SafeGetInt(r["YearLearn"], 1);
                                chkIsFullTime.Checked = SafeGetBool(r["IsFullTime"]);
                                chkActually.Checked = SafeGetBool(r["Actually"]);

                                // Восстанавливаем куратора в выпадающем списке
                                int teacherId = SafeGetInt(r["MainTeacher"], -1);
                                cmbMainTeacher.SelectedIndex = 0; // По умолчанию "Не назначен"

                                foreach (TeacherItem item in cmbMainTeacher.Items)
                                {
                                    if (item.TeacherID == teacherId)
                                    {
                                        cmbMainTeacher.SelectedItem = item;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Ошибка чтения данных группы: " + ex.Message); }
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            lstGroups.SelectedIndex = -1;
            ClearFields();
            _currentGroupId = -1;
            lblStatus.Text = "Добавление новой группы";
            txtGroupName.Focus();
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            string gName = txtGroupName.Text.Trim();
            if (string.IsNullOrEmpty(gName))
            {
                MessageBox.Show("Введите название группы!", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int? teacherId = null;
            if (cmbMainTeacher.SelectedItem != null)
            {
                var selectedTeacher = (TeacherItem)cmbMainTeacher.SelectedItem;
                if (selectedTeacher.TeacherID != -1) teacherId = selectedTeacher.TeacherID;
            }

            try
            {
                using (var conn = new OleDbConnection(_connectionString))
                {
                    conn.Open();

                    if (_currentGroupId == -1)
                    {
                        string sql = "INSERT INTO GroupsList (GroupName, StudentCount, IsFullTime, Actually, MainTeacher, YearLearn) VALUES (?, ?, ?, ?, ?, ?)";
                        using (var cmd = new OleDbCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("?", gName);
                            cmd.Parameters.AddWithValue("?", (int)numStudentCount.Value);
                            cmd.Parameters.AddWithValue("?", chkIsFullTime.Checked);
                            cmd.Parameters.AddWithValue("?", chkActually.Checked);
                            cmd.Parameters.AddWithValue("?", teacherId.HasValue ? (object)teacherId.Value : DBNull.Value);
                            cmd.Parameters.AddWithValue("?", (int)numYearLearn.Value);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        string sql = "UPDATE GroupsList SET GroupName=?, StudentCount=?, IsFullTime=?, Actually=?, MainTeacher=?, YearLearn=? WHERE GroupId=?";
                        using (var cmd = new OleDbCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("?", gName);
                            cmd.Parameters.AddWithValue("?", (int)numStudentCount.Value);
                            cmd.Parameters.AddWithValue("?", chkIsFullTime.Checked);
                            cmd.Parameters.AddWithValue("?", chkActually.Checked);
                            cmd.Parameters.AddWithValue("?", teacherId.HasValue ? (object)teacherId.Value : DBNull.Value);
                            cmd.Parameters.AddWithValue("?", (int)numYearLearn.Value);
                            cmd.Parameters.AddWithValue("?", _currentGroupId);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }

                LoadGroupsList();
                MessageBox.Show("Данные успешно сохранены!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { MessageBox.Show("Ошибка сохранения БД: " + ex.Message); }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (_currentGroupId == -1) return;
            if (MessageBox.Show("Удалить группу?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                try
                {
                    using (var conn = new OleDbConnection(_connectionString))
                    {
                        conn.Open();
                        new OleDbCommand($"DELETE FROM GroupsList WHERE GroupId = {_currentGroupId}", conn).ExecuteNonQuery();
                    }
                    LoadGroupsList();
                }
                catch (Exception ex) { MessageBox.Show("Невозможно удалить (связанные данные): " + ex.Message); }
            }
        }

        private void ClearFields()
        {
            txtGroupName.Text = "";
            numStudentCount.Value = 25;
            numYearLearn.Value = 1;
            chkIsFullTime.Checked = true;
            chkActually.Checked = true;
            if (cmbMainTeacher.Items.Count > 0) cmbMainTeacher.SelectedIndex = 0;
        }
    }
}