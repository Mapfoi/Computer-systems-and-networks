namespace Chat_Client
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        private TextBox txtHistory;
        private TextBox txtMessage;
        private Button btnSend;
        private TextBox txtName;
        private TextBox txtTcpPort;
        private Button btnConnect;
        private Label lblStatus;
        private Label lblName;
        private Label lblPort;

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
            txtHistory = new TextBox();
            txtMessage = new TextBox();
            btnSend = new Button();
            txtName = new TextBox();
            txtTcpPort = new TextBox();
            btnConnect = new Button();
            lblStatus = new Label();
            lblName = new Label();
            lblPort = new Label();
            SuspendLayout();
            // 
            // txtHistory
            // 
            txtHistory.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            txtHistory.Location = new Point(12, 64);
            txtHistory.Multiline = true;
            txtHistory.Name = "txtHistory";
            txtHistory.ReadOnly = true;
            txtHistory.ScrollBars = ScrollBars.Vertical;
            txtHistory.Size = new Size(776, 342);
            txtHistory.TabIndex = 0;
            // 
            // txtMessage
            // 
            txtMessage.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            txtMessage.Location = new Point(12, 412);
            txtMessage.Name = "txtMessage";
            txtMessage.Size = new Size(664, 23);
            txtMessage.TabIndex = 5;
            txtMessage.KeyDown += txtMessage_KeyDown;
            // 
            // btnSend
            // 
            btnSend.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnSend.Location = new Point(682, 412);
            btnSend.Name = "btnSend";
            btnSend.Size = new Size(106, 23);
            btnSend.TabIndex = 6;
            btnSend.Text = "Отправить";
            btnSend.UseVisualStyleBackColor = true;
            btnSend.Click += btnSend_Click;
            // 
            // txtName
            // 
            txtName.Location = new Point(66, 9);
            txtName.Name = "txtName";
            txtName.Size = new Size(150, 23);
            txtName.TabIndex = 1;
            // 
            // txtTcpPort
            // 
            txtTcpPort.Location = new Point(280, 9);
            txtTcpPort.Name = "txtTcpPort";
            txtTcpPort.Size = new Size(80, 23);
            txtTcpPort.TabIndex = 2;
            txtTcpPort.Text = "60000";
            // 
            // btnConnect
            // 
            btnConnect.Location = new Point(380, 8);
            btnConnect.Name = "btnConnect";
            btnConnect.Size = new Size(120, 23);
            btnConnect.TabIndex = 3;
            btnConnect.Text = "Подключиться";
            btnConnect.UseVisualStyleBackColor = true;
            btnConnect.Click += btnConnect_Click;
            // 
            // lblStatus
            // 
            lblStatus.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            lblStatus.AutoSize = true;
            lblStatus.Location = new Point(12, 40);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(118, 15);
            lblStatus.TabIndex = 7;
            lblStatus.Text = "Не подключено к чату";
            // 
            // lblName
            // 
            lblName.AutoSize = true;
            lblName.Location = new Point(12, 12);
            lblName.Name = "lblName";
            lblName.Size = new Size(34, 15);
            lblName.TabIndex = 8;
            lblName.Text = "Имя:";
            // 
            // lblPort
            // 
            lblPort.AutoSize = true;
            lblPort.Location = new Point(222, 12);
            lblPort.Name = "lblPort";
            lblPort.Size = new Size(52, 15);
            lblPort.TabIndex = 9;
            lblPort.Text = "TCP порт";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(lblPort);
            Controls.Add(lblName);
            Controls.Add(lblStatus);
            Controls.Add(btnConnect);
            Controls.Add(txtTcpPort);
            Controls.Add(txtName);
            Controls.Add(btnSend);
            Controls.Add(txtMessage);
            Controls.Add(txtHistory);
            Name = "Form1";
            Text = "Chat Client";
            FormClosing += Form1_FormClosing;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
    }
}
