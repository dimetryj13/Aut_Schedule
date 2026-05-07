namespace AutoSchedule
{
    partial class FormDbList
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.btnSelect = new System.Windows.Forms.Button();
            this.listViewDbs = new System.Windows.Forms.ListView();
            this.btnDelete = new System.Windows.Forms.Button();
            this.btnDuplicate = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // btnSelect
            // 
            this.btnSelect.Location = new System.Drawing.Point(447, 442);
            this.btnSelect.Name = "btnSelect";
            this.btnSelect.Size = new System.Drawing.Size(166, 40);
            this.btnSelect.TabIndex = 1;
            this.btnSelect.Text = "Выбрать базу данных";
            this.btnSelect.UseVisualStyleBackColor = true;
            this.btnSelect.Click += new System.EventHandler(this.btnSelect_Click);
            // 
            // listViewDbs
            // 
            this.listViewDbs.HideSelection = false;
            this.listViewDbs.Location = new System.Drawing.Point(12, 12);
            this.listViewDbs.Name = "listViewDbs";
            this.listViewDbs.Size = new System.Drawing.Size(750, 424);
            this.listViewDbs.TabIndex = 2;
            this.listViewDbs.UseCompatibleStateImageBehavior = false;
            this.listViewDbs.SelectedIndexChanged += new System.EventHandler(this.listViewDbs_SelectedIndexChanged);
            // 
            // btnDelete
            // 
            this.btnDelete.Location = new System.Drawing.Point(293, 442);
            this.btnDelete.Name = "btnDelete";
            this.btnDelete.Size = new System.Drawing.Size(148, 40);
            this.btnDelete.TabIndex = 3;
            this.btnDelete.Text = "Удалить";
            this.btnDelete.UseVisualStyleBackColor = true;
            this.btnDelete.Click += new System.EventHandler(this.btnDelete_Click);
            // 
            // btnDuplicate
            // 
            this.btnDuplicate.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            this.btnDuplicate.Location = new System.Drawing.Point(147, 442);
            this.btnDuplicate.Name = "btnDuplicate";
            this.btnDuplicate.Size = new System.Drawing.Size(140, 40);
            this.btnDuplicate.TabIndex = 4;
            this.btnDuplicate.Text = "Дублировать";
            this.btnDuplicate.UseVisualStyleBackColor = true;
            this.btnDuplicate.Click += new System.EventHandler(this.btnDuplicate_Click);
            // 
            // FormDbList
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(774, 495);
            this.Controls.Add(this.btnDuplicate);
            this.Controls.Add(this.btnDelete);
            this.Controls.Add(this.listViewDbs);
            this.Controls.Add(this.btnSelect);
            this.Name = "FormDbList";
            this.Text = "FormDbList";
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Button btnSelect;
        private System.Windows.Forms.ListView listViewDbs;
        private System.Windows.Forms.Button btnDelete;
        private System.Windows.Forms.Button btnDuplicate;
    }
}