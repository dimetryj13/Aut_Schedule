using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Drawing;
using System.Windows.Forms;

namespace AutoSchedule
{
    public class FormSubjectEditor : Form
    {
        private string _connectionString;
        private int _currentSubjectId = -1;

        // Элементы интерфейса
        private ListBox lstSubjects;

        // Поля редактирования
        private TextBox txtSubjectName;
        private CheckBox chkRequiresComputers;
        private ComboBox cmbFixedRoom;
        private ComboBox cmbForbiddenRoom;

        // Хранилище аудиторий (Номер аудитории)
        private List<string> _availableRooms = new List<string>();

        private Button btnSave, btnAdd, btnDelete;
        private Label lblStatus;

        public FormSubjectEditor(string connectionString)
        {
            _connectionString = connectionString;
            this.Text = "Редактор справочника: Дисциплины";
            this.Size = new Size(650, 400);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            LoadRooms(); // Сначала загружаем аудитории для выпадающих списков
            SetupUI();
            LoadSubjectsList();
        }

        // --- БЕЗОПАСНОЕ ЧТЕНИЕ (Защита от DBNull) ---
        private string SafeGetString(object val) => val != DBNull.Value && val != null ? val.ToString().Trim() : "";
        private bool SafeGetBool(object val) => val != DBNull.Value && val != null ? Convert.ToBoolean(val) : false;

        private void SetupUI()
        {
            // --- ЛЕВАЯ ПАНЕЛЬ (Список) ---
            lstSubjects = new ListBox
            {
                Location = new Point(10, 10),
                Size = new Size(240, 340),
                Font = new Font("Segoe UI", 11),
                DisplayMember = "Name",
                ValueMember = "ID"
            };
            lstSubjects.SelectedIndexChanged += LstSubjects_SelectedIndexChanged;
            this.Controls.Add(lstSubjects);

            // --- ПРАВАЯ ПАНЕЛЬ (Поля ввода) ---
            int startX = 270;
            int width = 340;
            int y = 20;

            this.Controls.Add(new Label { Text = "Название дисциплины:", Location = new Point(startX, y), AutoSize = true, Font = new Font("Segoe UI", 10) });
            txtSubjectName = new TextBox { Location = new Point(startX, y + 25), Size = new Size(width, 27), Font = new Font("Segoe UI", 11) };
            this.Controls.Add(txtSubjectName);

            y += 70;
            chkRequiresComputers = new CheckBox { Text = "🖥 Требуются компьютеры", Location = new Point(startX, y), AutoSize = true, Font = new Font("Segoe UI", 11), Cursor = Cursors.Hand };
            this.Controls.Add(chkRequiresComputers);

            y += 40;
            this.Controls.Add(new Label { Text = "Закрепленная аудитория (желательно):", Location = new Point(startX, y), AutoSize = true, Font = new Font("Segoe UI", 10) });
            cmbFixedRoom = new ComboBox { Location = new Point(startX, y + 25), Size = new Size(width, 27), Font = new Font("Segoe UI", 11), DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat };
            this.Controls.Add(cmbFixedRoom);

            y += 65;
            this.Controls.Add(new Label { Text = "Запрещенная аудитория (никогда):", Location = new Point(startX, y), AutoSize = true, Font = new Font("Segoe UI", 10) });
            cmbForbiddenRoom = new ComboBox { Location = new Point(startX, y + 25), Size = new Size(width, 27), Font = new Font("Segoe UI", 11), DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat };
            this.Controls.Add(cmbForbiddenRoom);

            // Заполняем списки аудиторий
            cmbFixedRoom.Items.Add("— Нет —"); // Пустой выбор
            cmbForbiddenRoom.Items.Add("— Нет —");
            foreach (var r in _availableRooms)
            {
                cmbFixedRoom.Items.Add(r);
                cmbForbiddenRoom.Items.Add(r);
            }

            y += 70;
            // --- КНОПКИ УПРАВЛЕНИЯ ---
            btnSave = new Button { Text = "💾 Сохранить", Location = new Point(startX, y), Size = new Size(160, 45), BackColor = Color.FromArgb(0, 122, 204), ForeColor = Color.White, Font = new Font("Segoe UI", 10, FontStyle.Bold), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnSave.Click += BtnSave_Click;
            this.Controls.Add(btnSave);

            btnAdd = new Button { Text = "➕ Добавить", Location = new Point(startX + 180, y), Size = new Size(160, 45), Font = new Font("Segoe UI", 10, FontStyle.Bold), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnAdd.Click += BtnAdd_Click;
            this.Controls.Add(btnAdd);

            y += 55;
            btnDelete = new Button { Text = "🗑 Удалить", Location = new Point(startX, y), Size = new Size(340, 35), ForeColor = Color.Crimson, Font = new Font("Segoe UI", 10, FontStyle.Bold), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnDelete.Click += BtnDelete_Click;
            this.Controls.Add(btnDelete);

            lblStatus = new Label { Location = new Point(startX, y + 40), AutoSize = true, ForeColor = Color.Gray, Font = new Font("Segoe UI", 9) };
            this.Controls.Add(lblStatus);
        }

        private void LoadRooms()
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
                            string rm = SafeGetString(r["RoomNumber"]);
                            if (!string.IsNullOrEmpty(rm)) _availableRooms.Add(rm);
                        }
                    }
                }
            }
            catch (Exception) { /* Игнорируем ошибку при пустой БД */ }
        }

        private void LoadSubjectsList()
        {
            lstSubjects.Items.Clear();
            try
            {
                using (var conn = new OleDbConnection(_connectionString))
                {
                    conn.Open();
                    using (var cmd = new OleDbCommand("SELECT SubjectID, SubjectName FROM Subjects ORDER BY SubjectName", conn))
                    using (var r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            int id = Convert.ToInt32(r["SubjectID"]);
                            string name = SafeGetString(r["SubjectName"]);
                            if (string.IsNullOrWhiteSpace(name)) name = $"[Без названия] ID: {id}";

                            lstSubjects.Items.Add(new { ID = id, Name = name });
                        }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Ошибка БД: " + ex.Message); }

            if (lstSubjects.Items.Count > 0) lstSubjects.SelectedIndex = 0;
            else ClearFields();
        }

        private void LstSubjects_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstSubjects.SelectedIndex == -1) return;

            _currentSubjectId = ((dynamic)lstSubjects.SelectedItem).ID;
            lblStatus.Text = $"Редактирование: ID {_currentSubjectId}";

            try
            {
                using (var conn = new OleDbConnection(_connectionString))
                {
                    conn.Open();
                    using (var cmd = new OleDbCommand("SELECT * FROM Subjects WHERE SubjectID = ?", conn))
                    {
                        cmd.Parameters.AddWithValue("?", _currentSubjectId);
                        using (var r = cmd.ExecuteReader())
                        {
                            if (r.Read())
                            {
                                txtSubjectName.Text = SafeGetString(r["SubjectName"]);
                                chkRequiresComputers.Checked = SafeGetBool(r["RequiresComputers"]);

                                string fixR = SafeGetString(r["FixedRoom"]);
                                cmbFixedRoom.Text = string.IsNullOrEmpty(fixR) ? "— Нет —" : fixR;

                                string forbR = SafeGetString(r["ForbiddenRoom"]);
                                cmbForbiddenRoom.Text = string.IsNullOrEmpty(forbR) ? "— Нет —" : forbR;
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show("Ошибка чтения данных: " + ex.Message); }
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            lstSubjects.SelectedIndex = -1;
            ClearFields();
            _currentSubjectId = -1;
            lblStatus.Text = "Добавление новой дисциплины";
            txtSubjectName.Focus();
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            string sName = txtSubjectName.Text.Trim();
            if (string.IsNullOrEmpty(sName))
            {
                MessageBox.Show("Введите название дисциплины!", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Если выбрано "— Нет —", сохраняем в БД пустую строку (или null), а не текст "— Нет —"
            string fRoom = cmbFixedRoom.Text == "— Нет —" ? "" : cmbFixedRoom.Text;
            string fbRoom = cmbForbiddenRoom.Text == "— Нет —" ? "" : cmbForbiddenRoom.Text;

            try
            {
                using (var conn = new OleDbConnection(_connectionString))
                {
                    conn.Open();

                    if (_currentSubjectId == -1)
                    {
                        string sql = "INSERT INTO Subjects (SubjectName, RequiresComputers, FixedRoom, ForbiddenRoom) VALUES (?, ?, ?, ?)";
                        using (var cmd = new OleDbCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("?", sName);
                            cmd.Parameters.AddWithValue("?", chkRequiresComputers.Checked);
                            cmd.Parameters.AddWithValue("?", fRoom);
                            cmd.Parameters.AddWithValue("?", fbRoom);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        string sql = "UPDATE Subjects SET SubjectName=?, RequiresComputers=?, FixedRoom=?, ForbiddenRoom=? WHERE SubjectID=?";
                        using (var cmd = new OleDbCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("?", sName);
                            cmd.Parameters.AddWithValue("?", chkRequiresComputers.Checked);
                            cmd.Parameters.AddWithValue("?", fRoom);
                            cmd.Parameters.AddWithValue("?", fbRoom);
                            cmd.Parameters.AddWithValue("?", _currentSubjectId);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }

                LoadSubjectsList();
                MessageBox.Show("Данные успешно сохранены!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { MessageBox.Show("Ошибка сохранения БД: " + ex.Message); }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (_currentSubjectId == -1) return;
            if (MessageBox.Show("Удалить дисциплину?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                try
                {
                    using (var conn = new OleDbConnection(_connectionString))
                    {
                        conn.Open();
                        new OleDbCommand($"DELETE FROM Subjects WHERE SubjectID = {_currentSubjectId}", conn).ExecuteNonQuery();
                    }
                    LoadSubjectsList();
                }
                catch (Exception ex) { MessageBox.Show("Невозможно удалить (связанные данные): " + ex.Message); }
            }
        }

        private void ClearFields()
        {
            txtSubjectName.Text = "";
            chkRequiresComputers.Checked = false;
            cmbFixedRoom.Text = "— Нет —";
            cmbForbiddenRoom.Text = "— Нет —";
        }
    }
}