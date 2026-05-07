using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using AutoSchedule.Models;
using AutoSchedule.Services;

namespace AutoSchedule
{
    public class FormManualEntry : Form
    {
        public PoolItem SelectedItem { get; private set; }
        public Teacher SelectedTeacher { get; private set; }
        public Classroom SelectedRoom { get; private set; }

        private DataGridView dgv;
        private ComboBox cbTeacher;
        private ComboBox cbRoom;
        private Button btnSave;
        private TableLayoutPanel tlpMain;

        public FormManualEntry(GroupList group, int day, int pair, int week,
                               List<PoolItem> poolItems, List<Teacher> teachers, List<Classroom> rooms)
        {
            this.Text = $"добавить пару: {group.GroupName}";
            this.Size = new Size(900, 550);
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimumSize = new Size(800, 450);

            // Инициализация TableLayoutPanel (1 колонка, 3 строки)
            tlpMain = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = Color.White
            };

            tlpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            tlpMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 100F));

            // --- 1. СТРОКА: ЗАГОЛОВОК ---
            string[] days = { "ПОНЕДЕЛЬНИК", "ВТОРНИК", "СРЕДА", "ЧЕТВЕРГ", "ПЯТНИЦА", "СУББОТА" };
            string weekStr = week == 1 ? "светлая" : "темная";
            Label lblHeader = new Label
            {
                Text = $"{days[day - 1]}, {pair} пара ({weekStr})",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            tlpMain.Controls.Add(lblHeader, 0, 0);

            // --- 2. СТРОКА: ТАБЛИЦА ДИСЦИПЛИН ---
            dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                GridColor = Color.LightGray
            };
            dgv.Columns.Add("Subject", "Наименование дисциплины");
            dgv.Columns.Add("Type", "Вид занятия");
            dgv.Columns.Add("Placed", "Расставлено");
            dgv.Columns.Add("Required", "План");
            dgv.Columns.Add("Teacher", "Преподаватель");

            foreach (var item in poolItems)
            {
                int placed = item.TotalCount - item.RemainingCount;
                string typeStr = item.LessonType == 0 ? "Лекция" : item.LessonType == 1 ? "Лабораторная" : "Практика";

                int idx = dgv.Rows.Add(item.PlanReference.Subject.SubjectName, typeStr, placed, item.TotalCount, item.AssignedTeacher.FullName);
                dgv.Rows[idx].Tag = item;

                // --- ЛОГИКА РАСЦВЕТКИ СТРОК ---
                Color rowColor;
                if (item.RemainingCount <= 0)
                {
                    rowColor = Color.FromArgb(245, 245, 245); // Серый для завершенных
                }
                else
                {
                    // Цвета как в основной шахматке
                    switch (item.LessonType)
                    {
                        case 0: rowColor = Color.FromArgb(220, 255, 220); break; // Зеленый (Лекция)
                        case 1: rowColor = Color.FromArgb(220, 240, 255); break; // Голубой (Лаб)
                        case 2: rowColor = Color.FromArgb(255, 225, 225); break; // Красный (Прак)
                        default: rowColor = Color.White; break;
                    }
                }
                dgv.Rows[idx].DefaultCellStyle.BackColor = rowColor;
            }

            dgv.SelectionChanged += Dgv_SelectionChanged;
            tlpMain.Controls.Add(dgv, 0, 1);

            // --- 3. СТРОКА: УПРАВЛЕНИЕ ---
            Panel pnlBottom = new Panel { Dock = DockStyle.Fill };

            Label lblTeacher = new Label { Text = "«Требуется замена? Выберите другого преподавателя»", Location = new Point(15, 10), AutoSize = true, Font = new Font("Segoe UI", 9) };
            cbTeacher = new ComboBox { Location = new Point(15, 30), Width = 320, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10) };

            Label lblRoom = new Label { Text = "«Выберите аудиторию:»", Location = new Point(360, 10), AutoSize = true, Font = new Font("Segoe UI", 9) };
            cbRoom = new ComboBox { Location = new Point(360, 30), Width = 180, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10) };

            btnSave = new Button
            {
                Text = "Добавить занятие",
                Location = new Point(570, 22),
                Width = 180,
                Height = 40,
                Enabled = false,
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            btnSave.Click += BtnSave_Click;

            pnlBottom.Controls.Add(lblTeacher);
            pnlBottom.Controls.Add(cbTeacher);
            pnlBottom.Controls.Add(lblRoom);
            pnlBottom.Controls.Add(cbRoom);
            pnlBottom.Controls.Add(btnSave);
            tlpMain.Controls.Add(pnlBottom, 0, 2);

            this.Controls.Add(tlpMain);

            cbTeacher.Items.AddRange(teachers.Select(t => new TeacherComboItem(t)).ToArray());
            cbRoom.Items.AddRange(rooms.Select(r => new RoomComboItem(r)).ToArray());
        }

        private void Dgv_SelectionChanged(object sender, EventArgs e)
        {
            if (dgv.SelectedRows.Count > 0)
            {
                var item = dgv.SelectedRows[0].Tag as PoolItem;
                if (item != null)
                {
                    for (int i = 0; i < cbTeacher.Items.Count; i++)
                    {
                        if (((TeacherComboItem)cbTeacher.Items[i]).Teacher.TeacherID == item.AssignedTeacher.TeacherID)
                        {
                            cbTeacher.SelectedIndex = i; break;
                        }
                    }
                    btnSave.Enabled = true;
                }
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (dgv.SelectedRows.Count == 0 || cbRoom.SelectedItem == null)
            {
                MessageBox.Show("Обязательно выберите дисциплину в таблице и укажите аудиторию!");
                return;
            }

            SelectedItem = dgv.SelectedRows[0].Tag as PoolItem;
            SelectedTeacher = ((TeacherComboItem)cbTeacher.SelectedItem).Teacher;
            SelectedRoom = ((RoomComboItem)cbRoom.SelectedItem).Room;

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private class TeacherComboItem
        {
            public Teacher Teacher { get; }
            public TeacherComboItem(Teacher t) { Teacher = t; }
            public override string ToString() => Teacher.FullName;
        }
        private class RoomComboItem
        {
            public Classroom Room { get; }
            public RoomComboItem(Classroom r) { Room = r; }
            public override string ToString() => Room.RoomNumber;
        }
    }
}