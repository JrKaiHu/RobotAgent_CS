namespace RobotAgent_CS
{
    partial class Form1
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.timer_IsArmConnected = new System.Windows.Forms.Timer(this.components);
            this.timer_IsCPConnected = new System.Windows.Forms.Timer(this.components);
            this.laserCombo = new System.Windows.Forms.ComboBox();
            this.laserLabel = new System.Windows.Forms.Label();
            this.barcodeLabel = new System.Windows.Forms.Label();
            this.barcodeCombo = new System.Windows.Forms.ComboBox();
            this.cameraLabel = new System.Windows.Forms.Label();
            this.cameraCombo = new System.Windows.Forms.ComboBox();
            this.brLabel = new System.Windows.Forms.Label();
            this.brTB = new System.Windows.Forms.TextBox();
            this.msgLabel = new System.Windows.Forms.Label();
            this.logLabel = new System.Windows.Forms.Label();
            this.imgLabel = new System.Windows.Forms.Label();
            this.msgRichTB = new System.Windows.Forms.RichTextBox();
            this.logRichTB = new System.Windows.Forms.RichTextBox();
            this.timer_UpdateCameraBuffer = new System.Windows.Forms.Timer(this.components);
            this.m_takeSnapshot = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // timer_IsArmConnected
            // 
            this.timer_IsArmConnected.Interval = 1000;
            this.timer_IsArmConnected.Tick += new System.EventHandler(this.timer_IsArmConnected_Tick);
            // 
            // timer_IsCPConnected
            // 
            this.timer_IsCPConnected.Interval = 1000;
            this.timer_IsCPConnected.Tick += new System.EventHandler(this.timer_IsCPConnected_Tick);
            // 
            // laserCombo
            // 
            this.laserCombo.Font = new System.Drawing.Font("Verdana", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.laserCombo.FormattingEnabled = true;
            this.laserCombo.Location = new System.Drawing.Point(34, 216);
            this.laserCombo.Name = "laserCombo";
            this.laserCombo.Size = new System.Drawing.Size(181, 24);
            this.laserCombo.TabIndex = 10;
            this.laserCombo.Tag = "";
            this.laserCombo.SelectedIndexChanged += new System.EventHandler(this.laserCombo_SelectedIndexChanged);
            // 
            // laserLabel
            // 
            this.laserLabel.AutoSize = true;
            this.laserLabel.Location = new System.Drawing.Point(34, 180);
            this.laserLabel.Name = "laserLabel";
            this.laserLabel.Size = new System.Drawing.Size(115, 18);
            this.laserLabel.TabIndex = 11;
            this.laserLabel.Text = "Select Laser : ";
            this.laserLabel.UseMnemonic = false;
            // 
            // barcodeLabel
            // 
            this.barcodeLabel.AutoSize = true;
            this.barcodeLabel.Location = new System.Drawing.Point(34, 84);
            this.barcodeLabel.Name = "barcodeLabel";
            this.barcodeLabel.Size = new System.Drawing.Size(126, 18);
            this.barcodeLabel.TabIndex = 12;
            this.barcodeLabel.Text = "Select Barcode:";
            // 
            // barcodeCombo
            // 
            this.barcodeCombo.Font = new System.Drawing.Font("Verdana", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.barcodeCombo.FormattingEnabled = true;
            this.barcodeCombo.Location = new System.Drawing.Point(34, 120);
            this.barcodeCombo.Name = "barcodeCombo";
            this.barcodeCombo.Size = new System.Drawing.Size(181, 24);
            this.barcodeCombo.TabIndex = 13;
            this.barcodeCombo.SelectedIndexChanged += new System.EventHandler(this.barcodeCombo_SelectedIndexChanged);
            // 
            // cameraLabel
            // 
            this.cameraLabel.AutoSize = true;
            this.cameraLabel.Location = new System.Drawing.Point(34, 276);
            this.cameraLabel.Name = "cameraLabel";
            this.cameraLabel.Size = new System.Drawing.Size(129, 18);
            this.cameraLabel.TabIndex = 14;
            this.cameraLabel.Text = "Select Camera :";
            // 
            // cameraCombo
            // 
            this.cameraCombo.Font = new System.Drawing.Font("Verdana", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cameraCombo.FormattingEnabled = true;
            this.cameraCombo.Location = new System.Drawing.Point(34, 312);
            this.cameraCombo.Name = "cameraCombo";
            this.cameraCombo.Size = new System.Drawing.Size(181, 24);
            this.cameraCombo.TabIndex = 15;
            this.cameraCombo.SelectedIndexChanged += new System.EventHandler(this.cameraCombo_SelectedIndexChanged);
            // 
            // brLabel
            // 
            this.brLabel.AutoSize = true;
            this.brLabel.Location = new System.Drawing.Point(244, 511);
            this.brLabel.Name = "brLabel";
            this.brLabel.Size = new System.Drawing.Size(127, 18);
            this.brLabel.TabIndex = 18;
            this.brLabel.Text = "Barcode result :";
            // 
            // brTB
            // 
            this.brTB.Location = new System.Drawing.Point(280, 551);
            this.brTB.Name = "brTB";
            this.brTB.Size = new System.Drawing.Size(538, 26);
            this.brTB.TabIndex = 19;
            // 
            // msgLabel
            // 
            this.msgLabel.AutoSize = true;
            this.msgLabel.Font = new System.Drawing.Font("Cambria", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.msgLabel.Location = new System.Drawing.Point(872, 51);
            this.msgLabel.Name = "msgLabel";
            this.msgLabel.Size = new System.Drawing.Size(133, 17);
            this.msgLabel.TabIndex = 20;
            this.msgLabel.Text = "Command and ACK :";
            // 
            // logLabel
            // 
            this.logLabel.AutoSize = true;
            this.logLabel.Location = new System.Drawing.Point(875, 332);
            this.logLabel.Name = "logLabel";
            this.logLabel.Size = new System.Drawing.Size(47, 18);
            this.logLabel.TabIndex = 22;
            this.logLabel.Text = "Log :";
            // 
            // imgLabel
            // 
            this.imgLabel.AutoSize = true;
            this.imgLabel.Location = new System.Drawing.Point(249, 53);
            this.imgLabel.Name = "imgLabel";
            this.imgLabel.Size = new System.Drawing.Size(67, 18);
            this.imgLabel.TabIndex = 24;
            this.imgLabel.Text = "Image :";
            // 
            // msgRichTB
            // 
            this.msgRichTB.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.msgRichTB.Location = new System.Drawing.Point(875, 84);
            this.msgRichTB.Name = "msgRichTB";
            this.msgRichTB.Size = new System.Drawing.Size(203, 233);
            this.msgRichTB.TabIndex = 25;
            this.msgRichTB.Text = "";
            // 
            // logRichTB
            // 
            this.logRichTB.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.logRichTB.Location = new System.Drawing.Point(878, 377);
            this.logRichTB.Name = "logRichTB";
            this.logRichTB.Size = new System.Drawing.Size(200, 240);
            this.logRichTB.TabIndex = 26;
            this.logRichTB.Text = "";
            // 
            // timer_UpdateCameraBuffer
            // 
            this.timer_UpdateCameraBuffer.Interval = 50;
            this.timer_UpdateCameraBuffer.Tick += new System.EventHandler(this.timer_UpdateCameraBuffer_Tick);
            // 
            // m_takeSnapshot
            // 
            this.m_takeSnapshot.Location = new System.Drawing.Point(37, 13);
            this.m_takeSnapshot.Name = "m_takeSnapshot";
            this.m_takeSnapshot.Size = new System.Drawing.Size(123, 32);
            this.m_takeSnapshot.TabIndex = 27;
            this.m_takeSnapshot.Text = "Save Image";
            this.m_takeSnapshot.UseVisualStyleBackColor = true;
            this.m_takeSnapshot.Click += new System.EventHandler(this.m_takeSnapshot_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1127, 638);
            this.Controls.Add(this.m_takeSnapshot);
            this.Controls.Add(this.logRichTB);
            this.Controls.Add(this.msgRichTB);
            this.Controls.Add(this.imgLabel);
            this.Controls.Add(this.logLabel);
            this.Controls.Add(this.msgLabel);
            this.Controls.Add(this.brTB);
            this.Controls.Add(this.brLabel);
            this.Controls.Add(this.cameraCombo);
            this.Controls.Add(this.cameraLabel);
            this.Controls.Add(this.barcodeCombo);
            this.Controls.Add(this.barcodeLabel);
            this.Controls.Add(this.laserLabel);
            this.Controls.Add(this.laserCombo);
            this.Font = new System.Drawing.Font("Verdana", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.MaximizeBox = false;
            this.Name = "Form1";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
            this.Text = "RobotAgent_CS 1.0.0.4";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_Closing);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Timer timer_IsArmConnected;
        private System.Windows.Forms.Timer timer_IsCPConnected;
        private System.Windows.Forms.ComboBox laserCombo;
        private System.Windows.Forms.Label laserLabel;
        private System.Windows.Forms.Label barcodeLabel;
        private System.Windows.Forms.ComboBox barcodeCombo;
        private System.Windows.Forms.Label cameraLabel;
        private System.Windows.Forms.ComboBox cameraCombo;
        private System.Windows.Forms.Label brLabel;
        private System.Windows.Forms.TextBox brTB;
        private System.Windows.Forms.Label msgLabel;
        private System.Windows.Forms.Label logLabel;
        private System.Windows.Forms.Label imgLabel;
        private System.Windows.Forms.RichTextBox msgRichTB;
        private System.Windows.Forms.RichTextBox logRichTB;
        private System.Windows.Forms.Timer timer_UpdateCameraBuffer;
        private System.Windows.Forms.Button m_takeSnapshot;

    }
}

