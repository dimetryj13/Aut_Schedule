using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace AutoSchedule
{
    public class FormHistory : Form
    {
        private DataGridView dgvLogs;
        private Button btnClear;
        private string logPath = Path.Combine(Application.StartupPath, "history_log.txt");

        public FormHistory()
        {
            this.Text = "История действий";
            this.Size = new Size(850, 450);
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimumSize = new Size(600, 300);

            // Используем TableLayoutPanel для правильного масштабирования
            TableLayoutPanel tlp = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1,
                BackColor = Color.White
            };
            tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlp.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));

            // Настройка таблицы логов
            dgvLogs = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                GridColor = Color.LightGray
            };

            // Создаем колонки, соответствующие формату нашей записи в Form1.cs
            dgvLogs.Columns.Add("Num", "№");
            dgvLogs.Columns.Add("Action", "Действие");
            dgvLogs.Columns.Add("Object", "Объект");
            dgvLogs.Columns.Add("Details", "Детали");
            dgvLogs.Columns.Add("Time", "Время");
            dgvLogs.Columns.Add("Date", "Дата");

            dgvLogs.Columns["Num"].Width = 40;
            dgvLogs.Columns["Action"].Width = 100;
            dgvLogs.Columns["Time"].Width = 80;
            dgvLogs.Columns["Date"].Width = 80;

            tlp.Controls.Add(dgvLogs, 0, 0);

            // Нижняя панель с кнопкой очистки
            Panel pnlBottom = new Panel { Dock = DockStyle.Fill };
            btnClear = new Button
            {
                Text = "Очистить историю",
                Location = new Point(15, 10),
                Size = new Size(180, 30),
                BackColor = Color.FromArgb(220, 53, 69), // Приятный красный цвет (Danger)
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            btnClear.FlatAppearance.BorderSize = 0;
            btnClear.Click += BtnClear_Click;

            pnlBottom.Controls.Add(btnClear);
            tlp.Controls.Add(pnlBottom, 0, 1);

            this.Controls.Add(tlp);

            // Загружаем данные из файла при открытии
            LoadLogs();
        }

        private void LoadLogs()
        {
            dgvLogs.Rows.Clear();
            if (File.Exists(logPath))
            {
                string[] lines = File.ReadAllLines(logPath);
                foreach (var line in lines)
                {
                    // Мы разделяли лог точкой с запятой ';'
                    var parts = line.Split(';');

                    // Безопасное добавление (если лог старого или нового формата)
                    if (parts.Length == 6)
                    {
                        int idx = dgvLogs.Rows.Add(parts[0], parts[1], parts[2], parts[3], parts[4], parts[5]);

                        // Раскрашиваем текст в зависимости от действия
                        if (parts[1] == "Добавление") dgvLogs.Rows[idx].Cells[1].Style.ForeColor = Color.Green;
                        if (parts[1] == "Удаление") dgvLogs.Rows[idx].Cells[1].Style.ForeColor = Color.Red;
                        if (parts[1] == "Замена" || parts[1].Contains("Смена")) dgvLogs.Rows[idx].Cells[1].Style.ForeColor = Color.Orange;
                    }
                    else if (parts.Length == 5) // Запасной вариант для старых логов
                    {
                        dgvLogs.Rows.Add(parts[0], parts[1], parts[2], "-", parts[3], parts[4]);
                    }
                }

                // Автоматически прокручиваем таблицу в самый низ (к последним действиям)
                if (dgvLogs.Rows.Count > 0)
                    dgvLogs.FirstDisplayedScrollingRowIndex = dgvLogs.Rows.Count - 1;
            }
        }

        private void BtnClear_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Вы уверены, что хотите удалить всю историю действий? Это действие нельзя отменить.",
                "Очистка", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                if (File.Exists(logPath))
                    File.Delete(logPath); // Физически удаляем файл
                LoadLogs(); // Обновляем (очищаем) таблицу
            }
        }
    }
}