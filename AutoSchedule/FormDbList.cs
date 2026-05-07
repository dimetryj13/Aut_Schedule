using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace AutoSchedule
{
    public partial class FormDbList : Form
    {
        // Путь выбранной базы для передачи в главное окно
        public string SelectedPath { get; private set; }

        // Выносим путь к папке на уровень класса, чтобы его видели ВСЕ методы
        private string dbFolderPath;

        public FormDbList()
        {
            InitializeComponent();

            // Инициализируем путь к нашей папке
            dbFolderPath = Path.Combine(Application.StartupPath, "Databases");

            // Настройка ListView
            listViewDbs.View = View.Details;
            listViewDbs.FullRowSelect = true;
            if (listViewDbs.Columns.Count == 0)
            {
                listViewDbs.Columns.Add("Название", 150);
                listViewDbs.Columns.Add("Дата изменения", 120);
                listViewDbs.Columns.Add("Путь", 300);
            }

            RefreshList();
        }

        // Метод для обновления отображения списка
        private void RefreshList()
        {
            listViewDbs.Items.Clear();

            if (Directory.Exists(dbFolderPath))
            {
                // Читаем файлы прямо из папки и сортируем по дате
                var files = Directory.GetFiles(dbFolderPath, "*.accdb")
                                     .OrderByDescending(f => File.GetLastWriteTime(f));

                foreach (string path in files)
                {
                    FileInfo info = new FileInfo(path);
                    ListViewItem item = new ListViewItem(Path.GetFileNameWithoutExtension(path));
                    item.SubItems.Add(info.LastWriteTime.ToString("dd.MM.yyyy HH:mm"));
                    item.SubItems.Add(path);
                    item.Tag = path;
                    listViewDbs.Items.Add(item);
                }
            }
        }

        private void btnSelect_Click(object sender, EventArgs e)
        {
            if (listViewDbs.SelectedItems.Count > 0)
            {
                SelectedPath = listViewDbs.SelectedItems[0].Tag.ToString();
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите базу из списка!");
            }
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (listViewDbs.SelectedItems.Count > 0)
            {
                string path = listViewDbs.SelectedItems[0].Tag.ToString();

                // Теперь мы просто спрашиваем, удалить ли файл, так как истории больше нет
                DialogResult dialogResult = MessageBox.Show(
                    "Вы хотите удалить файл базы данных с компьютера навсегда?",
                    "Удаление БД", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                if (dialogResult == DialogResult.Yes)
                {
                    try
                    {
                        File.Delete(path); // Удаляем физически
                        RefreshList(); // Обновляем экран (файл сам исчезнет)
                        MessageBox.Show("Файл успешно удален.");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Ошибка при удалении: " + ex.Message);
                    }
                }
            }
        }

        private void btnDuplicate_Click(object sender, EventArgs e)
        {
            if (listViewDbs.SelectedItems.Count > 0)
            {
                string originalPath = listViewDbs.SelectedItems[0].Tag.ToString();
                string newFileName = Path.GetFileNameWithoutExtension(originalPath) + "_Копия.accdb";

                // Теперь кнопка дублирования видит dbFolderPath
                string targetPath = Path.Combine(dbFolderPath, newFileName);

                try
                {
                    File.Copy(originalPath, targetPath, true);
                    RefreshList(); // Сразу обновляем экран
                    MessageBox.Show("Дубликат создан!");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка копирования: " + ex.Message);
                }
            }
        }

        private void listViewDbs_SelectedIndexChanged(object sender, EventArgs e)
        {
        }
    }
}