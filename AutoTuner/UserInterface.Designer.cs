namespace AutoTuner
{
    partial class UserInterface
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
            button1 = new Button();
            comboBox1 = new ComboBox();
            button2 = new Button();
            formsPlot1 = new ScottPlot.WinForms.FormsPlot();
            formsPlot2 = new ScottPlot.WinForms.FormsPlot();
            formsPlot3 = new ScottPlot.WinForms.FormsPlot();
            tableLayoutPanel1 = new TableLayoutPanel();
            label1 = new Label();
            label2 = new Label();
            label3 = new Label();
            tableLayoutPanel1.SuspendLayout();
            SuspendLayout();
            // 
            // button1
            // 
            button1.BackColor = Color.ForestGreen;
            button1.FlatStyle = FlatStyle.Flat;
            button1.Font = new Font("Trebuchet MS", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            button1.Location = new Point(39, 131);
            button1.Name = "button1";
            button1.Size = new Size(254, 121);
            button1.TabIndex = 0;
            button1.Text = "Start Tuning";
            button1.UseVisualStyleBackColor = false;
            button1.Click += button1_Click;
            // 
            // comboBox1
            // 
            comboBox1.FormattingEnabled = true;
            comboBox1.Items.AddRange(new object[] { "Overclock (Increases power limit)", "Undervolt (Decreases power limit)" });
            comboBox1.Location = new Point(39, 72);
            comboBox1.Name = "comboBox1";
            comboBox1.Size = new Size(254, 28);
            comboBox1.TabIndex = 1;
            // 
            // button2
            // 
            button2.BackColor = Color.Crimson;
            button2.FlatStyle = FlatStyle.Flat;
            button2.Font = new Font("Trebuchet MS", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            button2.Location = new Point(39, 283);
            button2.Name = "button2";
            button2.Size = new Size(254, 121);
            button2.TabIndex = 2;
            button2.Text = "Stop Tuning";
            button2.UseVisualStyleBackColor = false;
            button2.Click += button2_Click;
            // 
            // formsPlot1
            // 
            formsPlot1.Dock = DockStyle.Fill;
            formsPlot1.ForeColor = Color.WhiteSmoke;
            formsPlot1.Location = new Point(3, 3);
            formsPlot1.Name = "formsPlot1";
            formsPlot1.Size = new Size(476, 225);
            formsPlot1.TabIndex = 4;
            // 
            // formsPlot2
            // 
            formsPlot2.Dock = DockStyle.Fill;
            formsPlot2.Location = new Point(3, 234);
            formsPlot2.Name = "formsPlot2";
            formsPlot2.Size = new Size(476, 225);
            formsPlot2.TabIndex = 5;
            // 
            // formsPlot3
            // 
            formsPlot3.Dock = DockStyle.Fill;
            formsPlot3.Location = new Point(3, 465);
            formsPlot3.Name = "formsPlot3";
            formsPlot3.Size = new Size(476, 226);
            formsPlot3.TabIndex = 6;
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 1;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.Controls.Add(formsPlot1, 0, 0);
            tableLayoutPanel1.Controls.Add(formsPlot3, 0, 2);
            tableLayoutPanel1.Controls.Add(formsPlot2, 0, 1);
            tableLayoutPanel1.Location = new Point(455, 28);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 3;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3333321F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3333321F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3333321F));
            tableLayoutPanel1.Size = new Size(482, 694);
            tableLayoutPanel1.TabIndex = 7;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(39, 432);
            label1.Name = "label1";
            label1.Size = new Size(216, 20);
            label1.TabIndex = 8;
            label1.Text = "Current stage: Waiting for input";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(39, 477);
            label2.Name = "label2";
            label2.Size = new Size(85, 20);
            label2.TabIndex = 9;
            label2.Text = "GPU Usage:";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(39, 522);
            label3.Name = "label3";
            label3.Size = new Size(121, 20);
            label3.TabIndex = 10;
            label3.Text = "Waiting for input";
            // 
            // UserInterface
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(30, 30, 30);
            ClientSize = new Size(957, 742);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(tableLayoutPanel1);
            Controls.Add(button2);
            Controls.Add(comboBox1);
            Controls.Add(button1);
            ForeColor = Color.WhiteSmoke;
            Name = "UserInterface";
            Text = "Auto Tuning Tool";
            tableLayoutPanel1.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button button1;
        private ComboBox comboBox1;
        private Button button2;
        private ScottPlot.WinForms.FormsPlot formsPlot1;
        private ScottPlot.WinForms.FormsPlot formsPlot2;
        private ScottPlot.WinForms.FormsPlot formsPlot3;
        private TableLayoutPanel tableLayoutPanel1;
        private Label label1;
        private Label label2;
        private Label label3;
    }
}