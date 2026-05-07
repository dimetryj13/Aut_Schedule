using System;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.Windows.Forms;

namespace AutoSchedule
{
    public class FormDictionaries : Form
    {
        private TabControl tabControl;
        private string _connectionString;

        // Адаптеры и таблицы для автоматической синхронизации с БД
        private OleDbDataAdapter daTeachers, daRooms, daSubjects, daGroups;
        private DataTable dtTeachers, dtRooms, dtSubjects, dtGroups;

        // Флаг, сообщающий главной форме, что данные изменились
        public bool DataChanged { get; private set; } = false;

        public FormDictionaries(string connectionString)
        {
            _connectionString = connectionString;
            this.Text = "«Редактор базы данных» (Режим Excel)";
            this.Size = new Size(850, 500);
            this.StartPosition = FormStartPosition.CenterParent;

            tabControl = new TabControl { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10) };

            // Кнопка сохранения внизу
            Button btnSave = new Button
            {
                Text = "💾 Сохранить все изменения в базу данных",
                Dock = DockStyle.Bottom,
                Height = 45,
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnSave.Click += BtnSave_Click;

            this.Controls.Add(tabControl);
            this.Controls.Add(btnSave);

            LoadDataFromDatabase();
        }

        private void LoadDataFromDatabase()
        {
            try
            {
                using (OleDbConnection conn = new OleDbConnection(_connectionString))
                {
                    // 1. ПРЕПОДАВАТЕЛИ
                    daTeachers = new OleDbDataAdapter("SELECT * FROM [Teachers]", conn);
                    new OleDbCommandBuilder(daTeachers); // Магия: сам пишет INSERT/UPDATE
                    dtTeachers = new DataTable();
                    daTeachers.Fill(dtTeachers);
                    tabControl.TabPages.Add(CreateTab("Преподаватели", dtTeachers, "TeacherID"));

                    // 2. АУДИТОРИИ (Проверь: Classrooms или Rooms)
                    daRooms = new OleDbDataAdapter("SELECT * FROM [Classrooms]", conn);
                    new OleDbCommandBuilder(daRooms);
                    dtRooms = new DataTable();
                    daRooms.Fill(dtRooms);
                    tabControl.TabPages.Add(CreateTab("Аудитории", dtRooms, "RoomID"));

                    // 3. ДИСЦИПЛИНЫ
                    daSubjects = new OleDbDataAdapter("SELECT * FROM [Subjects]", conn);
                    new OleDbCommandBuilder(daSubjects);
                    dtSubjects = new DataTable();
                    daSubjects.Fill(dtSubjects);
                    tabControl.TabPages.Add(CreateTab("Дисциплины", dtSubjects, "SubjectID"));

                    // 4. ГРУППЫ (Проверь: Groups или GroupList)
                    daGroups = new OleDbDataAdapter("SELECT * FROM [Groups]", conn);
                    new OleDbCommandBuilder(daGroups);
                    dtGroups = new DataTable();
                    daGroups.Fill(dtGroups);
                    tabControl.TabPages.Add(CreateTab("Группы", dtGroups, "GroupId"));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при подключении к таблицам БД. Проверьте названия таблиц!\n" + ex.Message, "Ошибка БД", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private TabPage CreateTab(string title, DataTable dt, string idColumnName)
        {
            TabPage tab = new TabPage(title);
            DataGridView dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                DataSource = dt,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                AllowUserToAddRows = true,  // Разрешаем добавлять
                AllowUserToDeleteRows = true // Разрешаем удалять
            };

            // Прячем колонку с ID, так как счетчик (AutoNumber) база ставит сама
            if (dgv.Columns[idColumnName] != null)
                dgv.Columns[idColumnName].Visible = false;

            tab.Controls.Add(dgv);
            return tab;
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            try
            {
                // Принудительно завершаем редактирование ячейки, если курсор еще в ней
                this.Validate();

                // Отправляем все списки разом в Access
                daTeachers.Update(dtTeachers);
                daRooms.Update(dtRooms);
                daSubjects.Update(dtSubjects);
                daGroups.Update(dtGroups);

                DataChanged = true;
                MessageBox.Show("Изменения успешно сохранены!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка сохранения. Убедитесь, что обязательные поля заполнены и нет дубликатов.\n\n" + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}