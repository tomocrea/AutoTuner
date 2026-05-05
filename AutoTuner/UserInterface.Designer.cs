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
            SuspendLayout();
            // 
            // button1
            // 
            button1.BackColor = Color.LightGreen;
            button1.Font = new Font("Trebuchet MS", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            button1.Location = new Point(361, 42);
            button1.Name = "button1";
            button1.Size = new Size(163, 85);
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
            // UserInterface
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(562, 168);
            Controls.Add(comboBox1);
            Controls.Add(button1);
            Name = "UserInterface";
            Text = "Auto Tuning Tool";
            ResumeLayout(false);
        }

        #endregion

        private Button button1;
        private ComboBox comboBox1;
    }
}