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

        private OleDbDataAdapter daClassrooms, daGroups, daSubjects, daTeachers, daAvailability, daDaysOff, daRoomPrefs;
        private DataTable dtClassrooms, dtGroups, dtSubjects, dtTeachers, dtAvailability, dtDaysOff, dtRoomPrefs;

        public bool DataChanged { get; private set; } = false;

        // ИСПРАВЛЕНО: Добавлен конструктор с параметром
        public FormDictionaries(string connectionString)
        {
            _connectionString = connectionString;
            this.Text = "Редактор справочников БД";
            this.Size = new Size(950, 550);
            this.StartPosition = FormStartPosition.CenterParent;

            tabControl = new TabControl { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10) };

            Button btnSave = new Button
            {
                Text = "💾 Сохранить изменения во всех таблицах",
                Dock = DockStyle.Bottom,
                Height = 45,
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat
            };
            btnSave.Click += BtnSave_Click;

            this.Controls.Add(tabControl);
            this.Controls.Add(btnSave);

            // ИСПРАВЛЕНО: Загрузка вызывается только после инициализации _connectionString
            LoadDataFromDatabase();
        }

        private void LoadDataFromDatabase()
        {
            try
            {
                using (OleDbConnection conn = new OleDbConnection(_connectionString))
                {
                    // Используем точные названия таблиц и ID из твоего видео и DatabaseManager.cs
                    daClassrooms = new OleDbDataAdapter("SELECT * FROM Classroom", conn);
                    new OleDbCommandBuilder(daClassrooms);
                    dtClassrooms = new DataTable();
                    daClassrooms.Fill(dtClassrooms);
                    tabControl.TabPages.Add(CreateTab("Аудитории", dtClassrooms, "RoomID"));

                    daGroups = new OleDbDataAdapter("SELECT * FROM GroupsList", conn);
                    new OleDbCommandBuilder(daGroups);
                    dtGroups = new DataTable();
                    daGroups.Fill(dtGroups);
                    tabControl.TabPages.Add(CreateTab("Группы", dtGroups, "GroupId"));

                    daSubjects = new OleDbDataAdapter("SELECT * FROM Subjects", conn);
                    new OleDbCommandBuilder(daSubjects);
                    dtSubjects = new DataTable();
                    daSubjects.Fill(dtSubjects);
                    tabControl.TabPages.Add(CreateTab("Дисциплины", dtSubjects, "SubjectID"));

                    daTeachers = new OleDbDataAdapter("SELECT * FROM Teachers", conn);
                    new OleDbCommandBuilder(daTeachers);
                    dtTeachers = new DataTable();
                    daTeachers.Fill(dtTeachers);
                    tabControl.TabPages.Add(CreateTab("Преподаватели", dtTeachers, "TeacherID"));

                    daAvailability = new OleDbDataAdapter("SELECT * FROM TeacherAvailability", conn);
                    new OleDbCommandBuilder(daAvailability);
                    dtAvailability = new DataTable();
                    daAvailability.Fill(dtAvailability);
                    tabControl.TabPages.Add(CreateTab("Доступность", dtAvailability, "TeacherAvailabilityId"));

                    daDaysOff = new OleDbDataAdapter("SELECT * FROM TeacherDaysOff", conn);
                    new OleDbCommandBuilder(daDaysOff);
                    dtDaysOff = new DataTable();
                    daDaysOff.Fill(dtDaysOff);
                    tabControl.TabPages.Add(CreateTab("Выходные", dtDaysOff, "TeacherDayOffId"));

                    daRoomPrefs = new OleDbDataAdapter("SELECT * FROM TeacherRoomPrefs", conn);
                    new OleDbCommandBuilder(daRoomPrefs);
                    dtRoomPrefs = new DataTable();
                    daRoomPrefs.Fill(dtRoomPrefs);
                    tabControl.TabPages.Add(CreateTab("Приоритеты аудиторий", dtRoomPrefs, "TeacherRoomPrefId"));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки данных: " + ex.Message);
            }
        }

        private TabPage CreateTab(string title, DataTable dt, string primaryKeyColumn)
        {
            TabPage tab = new TabPage(title);
            DataGridView dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                DataSource = dt,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                AllowUserToAddRows = true
            };

            dgv.DataBindingComplete += (s, e) =>
            {
                if (dgv.Columns.Contains(primaryKeyColumn))
                    dgv.Columns[primaryKeyColumn].Visible = false;
            };

            tab.Controls.Add(dgv);
            return tab;
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            try
            {
                this.Validate();
                daClassrooms.Update(dtClassrooms);
                daGroups.Update(dtGroups);
                daSubjects.Update(dtSubjects);
                daTeachers.Update(dtTeachers);
                daAvailability.Update(dtAvailability);
                daDaysOff.Update(dtDaysOff);
                daRoomPrefs.Update(dtRoomPrefs);

                DataChanged = true;
                MessageBox.Show("Все изменения успешно сохранены!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при сохранении: " + ex.Message);
            }
        }
    }
}