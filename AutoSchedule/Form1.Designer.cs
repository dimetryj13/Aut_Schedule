namespace AutoSchedule
{
    partial class Form1
    {
        /// <summary>
        /// Обязательная переменная конструктора.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Освободить все используемые ресурсы.
        /// </summary>
        /// <param name="disposing">истинно, если управляемый ресурс должен быть удален; иначе ложно.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Код, автоматически созданный конструктором форм Windows

        /// <summary>
        /// Требуемый метод для поддержки конструктора — не изменяйте 
        /// содержимое этого метода с помощью редактора кода.
        /// </summary>
        private void InitializeComponent()
        {
            this.tlpMainLayout = new System.Windows.Forms.TableLayoutPanel();
            this.flpAssignments = new System.Windows.Forms.FlowLayoutPanel();
            this.pnlScheduleContainer = new System.Windows.Forms.Panel();
            this.flpRoomIndicators = new System.Windows.Forms.FlowLayoutPanel();
            this.pnlToolbar = new System.Windows.Forms.Panel();
            this.btnHistory = new System.Windows.Forms.Button();
            this.lblSemesterSelect = new System.Windows.Forms.Label();
            this.lblGroupSelect = new System.Windows.Forms.Label();
            this.btnOpenDb = new System.Windows.Forms.Button();
            this.btnDicts = new System.Windows.Forms.Button();
            this.tlpMainLayout.SuspendLayout();
            this.pnlToolbar.SuspendLayout();
            this.SuspendLayout();
            // 
            // tlpMainLayout
            // 
            this.tlpMainLayout.ColumnCount = 2;
            this.tlpMainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 19F));
            this.tlpMainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 81F));
            this.tlpMainLayout.Controls.Add(this.flpAssignments, 0, 0);
            this.tlpMainLayout.Controls.Add(this.pnlScheduleContainer, 1, 1);
            this.tlpMainLayout.Controls.Add(this.flpRoomIndicators, 1, 2);
            this.tlpMainLayout.Controls.Add(this.pnlToolbar, 1, 0);
            this.tlpMainLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpMainLayout.Location = new System.Drawing.Point(0, 0);
            this.tlpMainLayout.Name = "tlpMainLayout";
            this.tlpMainLayout.RowCount = 3;
            this.tlpMainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 13F));
            this.tlpMainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 78F));
            this.tlpMainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 9F));
            this.tlpMainLayout.Size = new System.Drawing.Size(800, 450);
            this.tlpMainLayout.TabIndex = 0;
            this.tlpMainLayout.Paint += new System.Windows.Forms.PaintEventHandler(this.tlpMainLayout_Paint);
            // 
            // flpAssignments
            // 
            this.flpAssignments.AutoScroll = true;
            this.flpAssignments.BackColor = System.Drawing.SystemColors.ControlLight;
            this.flpAssignments.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.flpAssignments.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flpAssignments.Location = new System.Drawing.Point(10, 10);
            this.flpAssignments.Margin = new System.Windows.Forms.Padding(10);
            this.flpAssignments.Name = "flpAssignments";
            this.tlpMainLayout.SetRowSpan(this.flpAssignments, 3);
            this.flpAssignments.Size = new System.Drawing.Size(132, 430);
            this.flpAssignments.TabIndex = 0;
            // 
            // pnlScheduleContainer
            // 
            this.pnlScheduleContainer.BackColor = System.Drawing.SystemColors.Window;
            this.pnlScheduleContainer.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlScheduleContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlScheduleContainer.Location = new System.Drawing.Point(155, 61);
            this.pnlScheduleContainer.Name = "pnlScheduleContainer";
            this.pnlScheduleContainer.Size = new System.Drawing.Size(642, 345);
            this.pnlScheduleContainer.TabIndex = 1;
            // 
            // flpRoomIndicators
            // 
            this.flpRoomIndicators.AutoScroll = true;
            this.flpRoomIndicators.BackColor = System.Drawing.SystemColors.WindowFrame;
            this.flpRoomIndicators.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.flpRoomIndicators.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flpRoomIndicators.Location = new System.Drawing.Point(155, 412);
            this.flpRoomIndicators.Name = "flpRoomIndicators";
            this.flpRoomIndicators.Padding = new System.Windows.Forms.Padding(10);
            this.flpRoomIndicators.Size = new System.Drawing.Size(642, 35);
            this.flpRoomIndicators.TabIndex = 2;
            // 
            // pnlToolbar
            // 
            this.pnlToolbar.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlToolbar.Controls.Add(this.btnDicts);
            this.pnlToolbar.Controls.Add(this.btnHistory);
            this.pnlToolbar.Controls.Add(this.lblSemesterSelect);
            this.pnlToolbar.Controls.Add(this.lblGroupSelect);
            this.pnlToolbar.Controls.Add(this.btnOpenDb);
            this.pnlToolbar.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlToolbar.Location = new System.Drawing.Point(155, 3);
            this.pnlToolbar.Name = "pnlToolbar";
            this.pnlToolbar.Size = new System.Drawing.Size(642, 52);
            this.pnlToolbar.TabIndex = 3;
            // 
            // btnHistory
            // 
            this.btnHistory.Location = new System.Drawing.Point(173, 28);
            this.btnHistory.Name = "btnHistory";
            this.btnHistory.Size = new System.Drawing.Size(92, 23);
            this.btnHistory.TabIndex = 3;
            this.btnHistory.Text = "📖 История действий";
            this.btnHistory.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
            this.btnHistory.Click += new System.EventHandler(this.btnHistory_Click);
            // 
            // lblSemesterSelect
            // 
            this.lblSemesterSelect.AutoSize = true;
            this.lblSemesterSelect.Font = new System.Drawing.Font("Times New Roman", 10.2F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lblSemesterSelect.Location = new System.Drawing.Point(3, 28);
            this.lblSemesterSelect.Name = "lblSemesterSelect";
            this.lblSemesterSelect.Size = new System.Drawing.Size(80, 19);
            this.lblSemesterSelect.TabIndex = 2;
            this.lblSemesterSelect.Text = "Семестр:";
            this.lblSemesterSelect.Click += new System.EventHandler(this.lblSemesterSelect_Click);
            // 
            // lblGroupSelect
            // 
            this.lblGroupSelect.AutoSize = true;
            this.lblGroupSelect.Font = new System.Drawing.Font("Times New Roman", 10.2F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lblGroupSelect.Location = new System.Drawing.Point(3, 6);
            this.lblGroupSelect.Name = "lblGroupSelect";
            this.lblGroupSelect.Size = new System.Drawing.Size(74, 19);
            this.lblGroupSelect.TabIndex = 1;
            this.lblGroupSelect.Text = "Группа: ";
            this.lblGroupSelect.Click += new System.EventHandler(this.lblGroupSelect_Click);
            // 
            // btnOpenDb
            // 
            this.btnOpenDb.Location = new System.Drawing.Point(190, 3);
            this.btnOpenDb.Name = "btnOpenDb";
            this.btnOpenDb.Size = new System.Drawing.Size(75, 23);
            this.btnOpenDb.TabIndex = 0;
            this.btnOpenDb.Text = "button1";
            this.btnOpenDb.UseVisualStyleBackColor = true;
            this.btnOpenDb.Click += new System.EventHandler(this.btnOpenDb_Click);
            // 
            // btnDicts
            // 
            this.btnDicts.Location = new System.Drawing.Point(271, 2);
            this.btnDicts.Name = "btnDicts";
            this.btnDicts.Size = new System.Drawing.Size(105, 23);
            this.btnDicts.TabIndex = 4;
            this.btnDicts.Text = "Справочники";
            this.btnDicts.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageAboveText;
            this.btnDicts.Click += new System.EventHandler(this.btnDicts_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.tlpMainLayout);
            this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.Name = "Form1";
            this.Text = "Form1";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.Load += new System.EventHandler(this.Form1_Load);
            this.tlpMainLayout.ResumeLayout(false);
            this.pnlToolbar.ResumeLayout(false);
            this.pnlToolbar.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tlpMainLayout;
        private System.Windows.Forms.FlowLayoutPanel flpAssignments;
        private System.Windows.Forms.Panel pnlScheduleContainer;
        private System.Windows.Forms.FlowLayoutPanel flpRoomIndicators;
        private System.Windows.Forms.Panel pnlToolbar;
        private System.Windows.Forms.Button btnOpenDb;
        private System.Windows.Forms.Label lblGroupSelect;
        private System.Windows.Forms.Label lblSemesterSelect;
        private System.Windows.Forms.Button btnHistory;
        private System.Windows.Forms.Button btnDicts;
    }
}

