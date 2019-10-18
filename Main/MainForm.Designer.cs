using System.Windows.Forms;

namespace DreamView
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.upperPanel = new System.Windows.Forms.Panel();
            this.btInject = new DreamView.MButton();
            this.btRevert = new DreamView.MButton();
            this.btImport = new DreamView.MButton();
            this.btExport = new DreamView.MButton();
            this.btFullscreen = new DreamView.MButton();
            this.btShowHide = new DreamView.MButton();
            this.btOptions = new DreamView.MButton();
            this.split = new System.Windows.Forms.SplitContainer();
            this.tree = new DreamView.MTreeView();
            this.renderPanel = new DreamView.MPanel();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.upperPanel.SuspendLayout();
            this.split.Panel1.SuspendLayout();
            this.split.Panel2.SuspendLayout();
            this.split.SuspendLayout();
            this.SuspendLayout();
            // 
            // upperPanel
            // 
            this.upperPanel.Controls.Add(this.btInject);
            this.upperPanel.Controls.Add(this.btRevert);
            this.upperPanel.Controls.Add(this.btImport);
            this.upperPanel.Controls.Add(this.btExport);
            this.upperPanel.Controls.Add(this.btFullscreen);
            this.upperPanel.Controls.Add(this.btShowHide);
            this.upperPanel.Controls.Add(this.btOptions);
            this.upperPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.upperPanel.Location = new System.Drawing.Point(0, 0);
            this.upperPanel.Name = "upperPanel";
            this.upperPanel.Size = new System.Drawing.Size(726, 21);
            this.upperPanel.TabIndex = 3;
            // 
            // btInject
            // 
            this.btInject.Location = new System.Drawing.Point(440, 0);
            this.btInject.Name = "btInject";
            this.btInject.Size = new System.Drawing.Size(101, 21);
            this.btInject.TabIndex = 6;
            this.btInject.Text = "inject (bun->pak)";
            this.btInject.UseVisualStyleBackColor = true;
            this.btInject.Click += new System.EventHandler(this.btInject_Click);
            // 
            // btRevert
            // 
            this.btRevert.Location = new System.Drawing.Point(331, 0);
            this.btRevert.Name = "btRevert";
            this.btRevert.Size = new System.Drawing.Size(103, 21);
            this.btRevert.TabIndex = 5;
            this.btRevert.Text = "revert (pak->bun)";
            this.btRevert.UseVisualStyleBackColor = true;
            this.btRevert.Click += new System.EventHandler(this.mButton1_Click);
            // 
            // btImport
            // 
            this.btImport.Location = new System.Drawing.Point(136, 0);
            this.btImport.Name = "btImport";
            this.btImport.Size = new System.Drawing.Size(55, 21);
            this.btImport.TabIndex = 4;
            this.btImport.Text = "Import";
            this.btImport.UseVisualStyleBackColor = true;
            this.btImport.Click += new System.EventHandler(this.btImport_Click);
            // 
            // btExport
            // 
            this.btExport.Location = new System.Drawing.Point(78, 0);
            this.btExport.Name = "btExport";
            this.btExport.Size = new System.Drawing.Size(52, 21);
            this.btExport.TabIndex = 3;
            this.btExport.Text = "Export";
            this.btExport.UseVisualStyleBackColor = true;
            this.btExport.Click += new System.EventHandler(this.btExport_Click);
            // 
            // btFullscreen
            // 
            this.btFullscreen.Location = new System.Drawing.Point(258, 0);
            this.btFullscreen.Name = "btFullscreen";
            this.btFullscreen.Size = new System.Drawing.Size(67, 21);
            this.btFullscreen.TabIndex = 2;
            this.btFullscreen.Text = "Fullscreen";
            this.btFullscreen.UseVisualStyleBackColor = true;
            this.btFullscreen.Click += new System.EventHandler(this.btFullscreen_Click);
            // 
            // btShowHide
            // 
            this.btShowHide.Location = new System.Drawing.Point(0, 0);
            this.btShowHide.Name = "btShowHide";
            this.btShowHide.Size = new System.Drawing.Size(72, 21);
            this.btShowHide.TabIndex = 1;
            this.btShowHide.Text = "Hide Tab";
            this.btShowHide.UseVisualStyleBackColor = true;
            this.btShowHide.Click += new System.EventHandler(this.btShowHide_Click);
            // 
            // btOptions
            // 
            this.btOptions.Location = new System.Drawing.Point(197, 0);
            this.btOptions.Name = "btOptions";
            this.btOptions.Size = new System.Drawing.Size(55, 21);
            this.btOptions.TabIndex = 0;
            this.btOptions.Text = "Options";
            this.btOptions.UseVisualStyleBackColor = true;
            this.btOptions.Click += new System.EventHandler(this.btOptions_Click);
            // 
            // split
            // 
            this.split.Dock = System.Windows.Forms.DockStyle.Fill;
            this.split.Location = new System.Drawing.Point(0, 21);
            this.split.Name = "split";
            // 
            // split.Panel1
            // 
            this.split.Panel1.Controls.Add(this.tree);
            // 
            // split.Panel2
            // 
            this.split.Panel2.Controls.Add(this.renderPanel);
            this.split.Size = new System.Drawing.Size(726, 468);
            this.split.SplitterDistance = 160;
            this.split.TabIndex = 4;
            // 
            // tree
            // 
            this.tree.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tree.Indent = 15;
            this.tree.Location = new System.Drawing.Point(0, 0);
            this.tree.Name = "tree";
            this.tree.Size = new System.Drawing.Size(160, 468);
            this.tree.TabIndex = 0;
            this.toolTip1.SetToolTip(this.tree, "Doubleclick to display");
            this.tree.DoubleClick += new System.EventHandler(this.tree_DoubleClick_1);
            // 
            // renderPanel
            // 
            this.renderPanel.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.renderPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.renderPanel.Location = new System.Drawing.Point(0, 0);
            this.renderPanel.Name = "renderPanel";
            this.renderPanel.Size = new System.Drawing.Size(562, 468);
            this.renderPanel.TabIndex = 0;
            this.renderPanel.MouseDown += new System.Windows.Forms.MouseEventHandler(this.renderPanel_MouseDown);
            this.renderPanel.MouseMove += new System.Windows.Forms.MouseEventHandler(this.renderPanel_MouseMove);
            this.renderPanel.Resize += new System.EventHandler(this.MainForm_Resize);
            this.renderPanel.Paint += new System.Windows.Forms.PaintEventHandler(this.renderPanel_Paint);
            this.renderPanel.MouseUp += new System.Windows.Forms.MouseEventHandler(this.renderPanel_MouseUp);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(726, 489);
            this.Controls.Add(this.split);
            this.Controls.Add(this.upperPanel);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.KeyPreview = true;
            this.Name = "MainForm";
            this.ResizeBegin += new System.EventHandler(this.MainForm_ResizeBegin);
            this.Resize += new System.EventHandler(this.MainForm_Resize);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.MainForm_KeyDown);
            this.ResizeEnd += new System.EventHandler(this.MainForm_ResizeEnd);
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.upperPanel.ResumeLayout(false);
            this.split.Panel1.ResumeLayout(false);
            this.split.Panel2.ResumeLayout(false);
            this.split.ResumeLayout(false);
            this.ResumeLayout(false);

        }
             

        #endregion
       
        private System.Windows.Forms.SplitContainer split;
        private MTreeView tree;
        private MButton btOptions;
        private MPanel renderPanel;
        private MButton btShowHide;
        private Panel upperPanel;
        private ToolTip toolTip1;
        private MButton btFullscreen;
        private MButton btExport;
        private MButton btImport;
        private MButton btRevert;
        private MButton btInject;
    }
}