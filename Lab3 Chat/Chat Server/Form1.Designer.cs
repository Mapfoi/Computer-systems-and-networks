namespace Chat_Server
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        private TextBox txtLog;
        private Button btnStartStop;
        private Label lblStatus;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            txtLog = new TextBox();
            btnStartStop = new Button();
            lblStatus = new Label();
            SuspendLayout();
            // 
            // txtLog
            // 
            txtLog.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            txtLog.Location = new Point(12, 41);
            txtLog.Multiline = true;
            txtLog.Name = "txtLog";
            txtLog.ReadOnly = true;
            txtLog.ScrollBars = ScrollBars.Vertical;
            txtLog.Size = new Size(776, 397);
            txtLog.TabIndex = 0;
            // 
            // btnStartStop
            // 
            btnStartStop.Location = new Point(12, 12);
            btnStartStop.Name = "btnStartStop";
            btnStartStop.Size = new Size(120, 23);
            btnStartStop.TabIndex = 1;
            btnStartStop.Text = "Запустить сервер";
            btnStartStop.UseVisualStyleBackColor = true;
            btnStartStop.Click += btnStartStop_Click;
            // 
            // lblStatus
            // 
            lblStatus.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            lblStatus.AutoSize = true;
            lblStatus.Location = new Point(150, 16);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(98, 15);
            lblStatus.TabIndex = 2;
            lblStatus.Text = "Сервер остановлен";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(lblStatus);
            Controls.Add(btnStartStop);
            Controls.Add(txtLog);
            Name = "Form1";
            Text = "Chat Server";
            FormClosing += Form1_FormClosing;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
    }
}
