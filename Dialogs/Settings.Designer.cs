namespace DreamView
{
    partial class Settings
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Settings));
            this.label1 = new System.Windows.Forms.Label();
            this.textPath = new System.Windows.Forms.TextBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.chkRefLighting = new System.Windows.Forms.CheckBox();
            this.chkshading = new System.Windows.Forms.CheckBox();
            this.listDevice = new System.Windows.Forms.ListBox();
            this.chkHW = new System.Windows.Forms.CheckBox();
            this.label4 = new System.Windows.Forms.Label();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.label6 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.numVerbose = new System.Windows.Forms.NumericUpDown();
            this.panel1 = new System.Windows.Forms.Panel();
            this.label3 = new System.Windows.Forms.Label();
            this.labelVersion = new System.Windows.Forms.Label();
            this.btOK = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.groupBox3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numVerbose)).BeginInit();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(2, 98);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(67, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "PakFile Path";
            // 
            // textPath
            // 
            this.textPath.Location = new System.Drawing.Point(75, 95);
            this.textPath.Name = "textPath";
            this.textPath.Size = new System.Drawing.Size(297, 20);
            this.textPath.TabIndex = 1;
            this.textPath.TextChanged += new System.EventHandler(this.textPath_TextChanged);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.chkRefLighting);
            this.groupBox1.Controls.Add(this.chkshading);
            this.groupBox1.Controls.Add(this.listDevice);
            this.groupBox1.Controls.Add(this.chkHW);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Location = new System.Drawing.Point(12, 131);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(359, 99);
            this.groupBox1.TabIndex = 11;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "DirectX settings";
            // 
            // chkRefLighting
            // 
            this.chkRefLighting.AutoSize = true;
            this.chkRefLighting.Checked = true;
            this.chkRefLighting.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkRefLighting.Location = new System.Drawing.Point(216, 59);
            this.chkRefLighting.Name = "chkRefLighting";
            this.chkRefLighting.Size = new System.Drawing.Size(118, 17);
            this.chkRefLighting.TabIndex = 9;
            this.chkRefLighting.Text = "Reference light box";
            this.chkRefLighting.UseVisualStyleBackColor = true;
            this.chkRefLighting.CheckedChanged += new System.EventHandler(this.chkRefLighting_CheckedChanged);
            // 
            // chkshading
            // 
            this.chkshading.AutoSize = true;
            this.chkshading.Checked = true;
            this.chkshading.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkshading.Location = new System.Drawing.Point(13, 76);
            this.chkshading.Name = "chkshading";
            this.chkshading.Size = new System.Drawing.Size(83, 17);
            this.chkshading.TabIndex = 8;
            this.chkshading.Text = "use shading";
            this.chkshading.UseVisualStyleBackColor = true;
            this.chkshading.CheckedChanged += new System.EventHandler(this.chkshading_CheckedChanged);
            // 
            // listDevice
            // 
            this.listDevice.FormattingEnabled = true;
            this.listDevice.Location = new System.Drawing.Point(63, 23);
            this.listDevice.Name = "listDevice";
            this.listDevice.ScrollAlwaysVisible = true;
            this.listDevice.Size = new System.Drawing.Size(271, 30);
            this.listDevice.TabIndex = 6;
            this.listDevice.SelectedIndexChanged += new System.EventHandler(this.listDevice_SelectedIndexChanged);
            // 
            // chkHW
            // 
            this.chkHW.AutoSize = true;
            this.chkHW.Checked = true;
            this.chkHW.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkHW.Location = new System.Drawing.Point(13, 59);
            this.chkHW.Name = "chkHW";
            this.chkHW.Size = new System.Drawing.Size(160, 17);
            this.chkHW.TabIndex = 5;
            this.chkHW.Text = "use full hardware processing";
            this.chkHW.UseVisualStyleBackColor = true;
            this.chkHW.CheckedChanged += new System.EventHandler(this.chkHW_CheckedChanged);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(10, 23);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(41, 13);
            this.label4.TabIndex = 3;
            this.label4.Text = "Device";
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
            this.pictureBox1.InitialImage = null;
            this.pictureBox1.Location = new System.Drawing.Point(-1, -1);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(67, 81);
            this.pictureBox1.TabIndex = 13;
            this.pictureBox1.TabStop = false;
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.label6);
            this.groupBox3.Controls.Add(this.label5);
            this.groupBox3.Controls.Add(this.numVerbose);
            this.groupBox3.Location = new System.Drawing.Point(12, 236);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(249, 77);
            this.groupBox3.TabIndex = 13;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "General settings";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(14, 54);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(182, 13);
            this.label6.TabIndex = 3;
            this.label6.Text = "Set to 0 unless you need to log errors";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(17, 28);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(104, 13);
            this.label5.TabIndex = 1;
            this.label5.Text = "Logfile verbose level";
            // 
            // numVerbose
            // 
            this.numVerbose.Location = new System.Drawing.Point(131, 26);
            this.numVerbose.Maximum = new decimal(new int[] {
            3,
            0,
            0,
            0});
            this.numVerbose.Name = "numVerbose";
            this.numVerbose.Size = new System.Drawing.Size(38, 20);
            this.numVerbose.TabIndex = 0;
            this.numVerbose.ValueChanged += new System.EventHandler(this.numVerbose_ValueChanged);
            // 
            // panel1
            // 
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.panel1.Controls.Add(this.label3);
            this.panel1.Controls.Add(this.labelVersion);
            this.panel1.Location = new System.Drawing.Point(75, 4);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(296, 76);
            this.panel1.TabIndex = 14;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 32);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(247, 13);
            this.label3.TabIndex = 1;
            this.label3.Text = "by Pinku no zou (Tobias Pfaff, vertigo80@gmx.net)";
            // 
            // labelVersion
            // 
            this.labelVersion.AutoSize = true;
            this.labelVersion.Location = new System.Drawing.Point(11, 10);
            this.labelVersion.Name = "labelVersion";
            this.labelVersion.Size = new System.Drawing.Size(101, 13);
            this.labelVersion.TabIndex = 0;
            this.labelVersion.Text = "DreamView version ";
            // 
            // btOK
            // 
            this.btOK.Location = new System.Drawing.Point(283, 288);
            this.btOK.Name = "btOK";
            this.btOK.Size = new System.Drawing.Size(88, 25);
            this.btOK.TabIndex = 15;
            this.btOK.Text = "Ok";
            this.btOK.UseVisualStyleBackColor = true;
            this.btOK.Click += new System.EventHandler(this.btOK_Click);
            // 
            // Settings
            // 
            this.AcceptButton = this.btOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ClientSize = new System.Drawing.Size(681, 334);
            this.ControlBox = false;
            this.Controls.Add(this.btOK);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.textPath);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Settings";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.Text = "Dreamview Settings";
            this.Load += new System.EventHandler(this.Settings_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numVerbose)).EndInit();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textPath;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.CheckBox chkHW;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ListBox listDevice;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.CheckBox chkshading;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.NumericUpDown numVerbose;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button btOK;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label labelVersion;
        private System.Windows.Forms.CheckBox chkRefLighting;
    }
}