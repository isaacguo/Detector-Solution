namespace DetectorServerHost
{
    partial class MainForm
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
            this.btnStart = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.tbWebServerPort = new System.Windows.Forms.TextBox();
            this.tbIPAddress = new System.Windows.Forms.TextBox();
            this.tbTCPListeningPort = new System.Windows.Forms.TextBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.lbWebServerStatus = new System.Windows.Forms.Label();
            this.lbDetectorStatus = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnStart
            // 
            this.btnStart.Location = new System.Drawing.Point(160, 159);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(75, 23);
            this.btnStart.TabIndex = 1;
            this.btnStart.Text = "启动";
            this.btnStart.UseVisualStyleBackColor = true;
            this.btnStart.Click += new System.EventHandler(this.button1_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 28);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(61, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "侦听端口：";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 53);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(97, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Web Server 端口：";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(6, 81);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(97, 13);
            this.label3.TabIndex = 2;
            this.label3.Text = "Web Server 地址：";
            // 
            // tbWebServerPort
            // 
            this.tbWebServerPort.Location = new System.Drawing.Point(109, 50);
            this.tbWebServerPort.Name = "tbWebServerPort";
            this.tbWebServerPort.Size = new System.Drawing.Size(89, 20);
            this.tbWebServerPort.TabIndex = 3;
            // 
            // tbIPAddress
            // 
            this.tbIPAddress.Location = new System.Drawing.Point(109, 78);
            this.tbIPAddress.Name = "tbIPAddress";
            this.tbIPAddress.Size = new System.Drawing.Size(89, 20);
            this.tbIPAddress.TabIndex = 5;
            // 
            // tbTCPListeningPort
            // 
            this.tbTCPListeningPort.Location = new System.Drawing.Point(109, 24);
            this.tbTCPListeningPort.Name = "tbTCPListeningPort";
            this.tbTCPListeningPort.Size = new System.Drawing.Size(89, 20);
            this.tbTCPListeningPort.TabIndex = 3;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.lbWebServerStatus);
            this.groupBox1.Controls.Add(this.lbDetectorStatus);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.label5);
            this.groupBox1.Location = new System.Drawing.Point(222, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(167, 124);
            this.groupBox1.TabIndex = 6;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "状态信息";
            // 
            // lbWebServerStatus
            // 
            this.lbWebServerStatus.AutoSize = true;
            this.lbWebServerStatus.Location = new System.Drawing.Point(117, 59);
            this.lbWebServerStatus.Name = "lbWebServerStatus";
            this.lbWebServerStatus.Size = new System.Drawing.Size(0, 13);
            this.lbWebServerStatus.TabIndex = 2;
            // 
            // lbDetectorStatus
            // 
            this.lbDetectorStatus.AutoSize = true;
            this.lbDetectorStatus.Location = new System.Drawing.Point(117, 31);
            this.lbDetectorStatus.Name = "lbDetectorStatus";
            this.lbDetectorStatus.Size = new System.Drawing.Size(0, 13);
            this.lbDetectorStatus.TabIndex = 2;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(16, 59);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(97, 13);
            this.label4.TabIndex = 2;
            this.label4.Text = "网络服务器状态：";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(16, 31);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(85, 13);
            this.label5.TabIndex = 2;
            this.label5.Text = "探头连接状态：";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.tbTCPListeningPort);
            this.groupBox2.Controls.Add(this.label1);
            this.groupBox2.Controls.Add(this.tbIPAddress);
            this.groupBox2.Controls.Add(this.label2);
            this.groupBox2.Controls.Add(this.label3);
            this.groupBox2.Controls.Add(this.tbWebServerPort);
            this.groupBox2.Location = new System.Drawing.Point(12, 12);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(204, 124);
            this.groupBox2.TabIndex = 6;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "状态信息";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(401, 194);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.btnStart);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MainForm";
            this.Text = "探头服务器";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox tbWebServerPort;
        private System.Windows.Forms.TextBox tbIPAddress;
        private System.Windows.Forms.TextBox tbTCPListeningPort;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label lbWebServerStatus;
        private System.Windows.Forms.Label lbDetectorStatus;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.GroupBox groupBox2;
    }
}

