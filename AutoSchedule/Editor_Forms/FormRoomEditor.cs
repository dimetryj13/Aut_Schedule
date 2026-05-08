using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Drawing;
using System.Windows.Forms;

namespace AutoSchedule
{
    public class FormRoomEditor : Form
    {
        private string _connectionString;

        // Внутренний класс для хранения данных аудитории в памяти
        private class RoomItem
        {
            public int RoomID { get; set; }
            public string RoomNumber { get; set; }
            public int Capacity { get; set; }
            public bool HasComputers { get; set; }

            // То, что будет отображаться в ListBox
            public override string ToString() => RoomNumber;
        }

        private List<RoomItem> _rooms = new List<RoomItem>();
        private RoomItem _selectedRoom = null;

        // Элементы интерфейса
        private ListBox lstRooms;
        private TextBox txtNumber;
        private NumericUpDown numCapacity;
        private CheckBox chkComputers;
        private Button btnSave, btnAdd, btnDelete;
        private Label lblStatus;

        public FormRoomEditor(string connectionString)
        {
            _connectionString = connectionString;
            this.Text = "Редактор справочника: Аудитории";
            this.Size = new Size(600, 420);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            SetupUI();
            LoadRooms();
        }

        private void SetupUI()
        {
            // 1. Список слева
            lstRooms = new ListBox
            {
                Location = new Point(10, 10),
                Size = new Size(200, 360),
                Font = new Font("Segoe UI", 11)
            };
            lstRooms.SelectedIndexChanged += LstRooms_SelectedIndexChanged;
            this.Controls.Add(lstRooms);

            // 2. Панель редактирования справа
            int x = 230, y = 20, w = 320;

            this.Controls.Add(new Label { Text = "Номер аудитории:", Location = new Point(x, y), AutoSize = true, Font = new Font("Segoe UI", 10) });
            txtNumber = new TextBox { Location = new Point(x, y + 25), Size = new Size(w, 27), Font = new Font("Segoe UI", 11) };
            this.Controls.Add(txtNumber);

            y += 70;

            this.Controls.Add(new Label { Text = "Вместимость (чел):", Location = new Point(x, y), AutoSize = true, Font = new Font("Segoe UI", 10) });
            numCapacity = new NumericUpDown
            {
                Location = new Point(x, y + 25),
                Size = new Size(120, 27),
                Minimum = 0,
                Maximum = 1000,
                Value = 30,
                Font = new Font("Segoe UI", 11)
            };
            this.Controls.Add(numCapacity);

            chkComputers = new CheckBox
            {
                Text = "🖥 Компьютерный класс",
                Location = new Point(x + 140, y + 25),
                AutoSize = true,
                Font = new Font("Segoe UI", 10),
                Cursor = Cursors.Hand
            };
            this.Controls.Add(chkComputers);

            y += 80;

            // Кнопки управления
            btnSave = new Button
            {
                Text = "💾 Сохранить",
                Location = new Point(x, y),
                Size = new Size(150, 45),
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnSave.Click += BtnSave_Click;
            this.Controls.Add(btnSave);

            btnAdd = new Button
            {
                Text = "➕ Добавить",
                Location = new Point(x + 170, y),
                Size = new Size(150, 45),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnAdd.Click += BtnAdd_Click;
            this.Controls.Add(btnAdd);

            y += 65;

            btnDelete = new Button
            {
                Text = "🗑 Удалить выбранную аудиторию",
                Location = new Point(x, y),
                Size = new Size(320, 35),
                ForeColor = Color.Crimson,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnDelete.Click += BtnDelete_Click;
            this.Controls.Add(btnDelete);

            // Статус
            lblStatus = new Label { Location = new Point(x, y + 50), AutoSize = true, ForeColor = Color.Gray, Font = new Font("Segoe UI", 9) };
            this.Controls.Add(lblStatus);
        }

        // --- ЛОГИКА БАЗЫ ДАННЫХ ---
        private void LoadRooms()
        {
            _rooms.Clear();
            lstRooms.Items.Clear();

            try
            {
                using (var conn = new OleDbConnection(_connectionString))
                {
                    conn.Open();
                    using (var cmd = new OleDbCommand("SELECT RoomID, RoomNumber, Capacity, HasComputers FROM Classroom ORDER BY RoomNumber", conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var room = new RoomItem
                            {
                                RoomID = Convert.ToInt32(reader["RoomID"]),
                                RoomNumber = reader["RoomNumber"].ToString(),
                                Capacity = Convert.ToInt32(reader["Capacity"]),
                                HasComputers = Convert.ToBoolean(reader["HasComputers"])
                            };
                            _rooms.Add(room);
                            lstRooms.Items.Add(room);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки из БД: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            if (lstRooms.Items.Count > 0) lstRooms.SelectedIndex = 0;
            else ClearFields();
        }

        private void LstRooms_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstRooms.SelectedIndex == -1) return;

            _selectedRoom = (RoomItem)lstRooms.SelectedItem;

            txtNumber.Text = _selectedRoom.RoomNumber;
            numCapacity.Value = _selectedRoom.Capacity;
            chkComputers.Checked = _selectedRoom.HasComputers;

            lblStatus.Text = $"Редактирование: ID {_selectedRoom.RoomID}";
            btnSave.Text = "💾 Сохранить";
            btnSave.BackColor = Color.FromArgb(0, 122, 204); // Синий
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            lstRooms.SelectedIndex = -1;
            ClearFields();
            _selectedRoom = null;

            lblStatus.Text = "Режим: Добавление новой аудитории";
            btnSave.Text = "💾 Создать";
            btnSave.BackColor = Color.SeaGreen; // Зеленый для новой записи
            txtNumber.Focus();
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            string number = txtNumber.Text.Trim();
            if (string.IsNullOrEmpty(number))
            {
                MessageBox.Show("Введите номер аудитории!", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using (var conn = new OleDbConnection(_connectionString))
                {
                    conn.Open();

                    if (_selectedRoom == null)
                    {
                        // INSERT - Создаем новую запись
                        string sql = "INSERT INTO Classroom (RoomNumber, Capacity, HasComputers) VALUES (?, ?, ?)";
                        using (var cmd = new OleDbCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("?", number);
                            cmd.Parameters.AddWithValue("?", (int)numCapacity.Value);
                            cmd.Parameters.AddWithValue("?", chkComputers.Checked);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        // UPDATE - Обновляем существующую
                        string sql = "UPDATE Classroom SET RoomNumber=?, Capacity=?, HasComputers=? WHERE RoomID=?";
                        using (var cmd = new OleDbCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("?", number);
                            cmd.Parameters.AddWithValue("?", (int)numCapacity.Value);
                            cmd.Parameters.AddWithValue("?", chkComputers.Checked);
                            cmd.Parameters.AddWithValue("?", _selectedRoom.RoomID);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }

                LoadRooms();
                MessageBox.Show("Данные успешно сохранены!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка сохранения: " + ex.Message, "Ошибка БД", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // FormRoomEditor
            // 
            this.ClientSize = new System.Drawing.Size(282, 253);
            this.Name = "FormRoomEditor";
            this.Text = "Ta";
            this.ResumeLayout(false);

        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (_selectedRoom == null) return;

            var result = MessageBox.Show($"Вы точно хотите удалить аудиторию '{_selectedRoom.RoomNumber}'?\nЭто действие нельзя отменить.",
                                         "Подтверждение удаления", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result == DialogResult.Yes)
            {
                try
                {
                    using (var conn = new OleDbConnection(_connectionString))
                    {
                        conn.Open();
                        var cmd = new OleDbCommand("DELETE FROM Classroom WHERE RoomID = ?", conn);
                        cmd.Parameters.AddWithValue("?", _selectedRoom.RoomID);
                        cmd.ExecuteNonQuery();
                    }
                    LoadRooms();
                }
                catch (Exception ex)
                {
                    // Защита от удаления, если на аудиторию уже ссылается расписание
                    MessageBox.Show("Не удалось удалить аудиторию. Возможно, она уже используется в расписании или приоритетах.\n\n" + ex.Message,
                                    "Ошибка целостности", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void ClearFields()
        {
            txtNumber.Text = "";
            numCapacity.Value = 30;
            chkComputers.Checked = false;
        }
    }
}