namespace MediaBrowser.ServerApplication
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
            this.notifyIcon1 = new System.Windows.Forms.NotifyIcon(this.components);
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.cmdExit = new System.Windows.Forms.ToolStripMenuItem();
            this.cmdCommunity = new System.Windows.Forms.ToolStripMenuItem();
            this.cmdLogWindow = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.cmdRestart = new System.Windows.Forms.ToolStripMenuItem();
            this.cmdLibraryExplorer = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.cmdConfigure = new System.Windows.Forms.ToolStripMenuItem();
            this.cmdBrowse = new System.Windows.Forms.ToolStripMenuItem();
            this.cmdApiDocs = new System.Windows.Forms.ToolStripMenuItem();
            this.cmdStandardDocs = new System.Windows.Forms.ToolStripMenuItem();
            this.cmdSwagger = new System.Windows.Forms.ToolStripMenuItem();
            this.cmdGtihub = new System.Windows.Forms.ToolStripMenuItem();
            this.contextMenuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // notifyIcon1
            // 
            this.notifyIcon1.ContextMenuStrip = this.contextMenuStrip1;
            this.notifyIcon1.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyIcon1.Icon")));
            this.notifyIcon1.Text = "Media Browser";
            this.notifyIcon1.Visible = true;
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.cmdBrowse,
            this.cmdConfigure,
            this.toolStripSeparator2,
            this.cmdLibraryExplorer,
            this.cmdRestart,
            this.toolStripSeparator1,
            this.cmdApiDocs,
            this.cmdLogWindow,
            this.cmdCommunity,
            this.cmdExit});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.ShowCheckMargin = true;
            this.contextMenuStrip1.ShowImageMargin = false;
            this.contextMenuStrip1.Size = new System.Drawing.Size(209, 214);
            // 
            // cmdExit
            // 
            this.cmdExit.Name = "cmdExit";
            this.cmdExit.Size = new System.Drawing.Size(208, 22);
            this.cmdExit.Text = "Exit";
            // 
            // cmdCommunity
            // 
            this.cmdCommunity.Name = "cmdCommunity";
            this.cmdCommunity.Size = new System.Drawing.Size(208, 22);
            this.cmdCommunity.Text = "Visit Community";
            // 
            // cmdLogWindow
            // 
            this.cmdLogWindow.CheckOnClick = true;
            this.cmdLogWindow.Name = "cmdLogWindow";
            this.cmdLogWindow.Size = new System.Drawing.Size(208, 22);
            this.cmdLogWindow.Text = "Show Log Window";
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(205, 6);
            // 
            // cmdRestart
            // 
            this.cmdRestart.Name = "cmdRestart";
            this.cmdRestart.Size = new System.Drawing.Size(208, 22);
            this.cmdRestart.Text = "Restart Server";
            // 
            // cmdLibraryExplorer
            // 
            this.cmdLibraryExplorer.Name = "cmdLibraryExplorer";
            this.cmdLibraryExplorer.Size = new System.Drawing.Size(208, 22);
            this.cmdLibraryExplorer.Text = "Open Library Explorer";
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(205, 6);
            // 
            // cmdConfigure
            // 
            this.cmdConfigure.Name = "cmdConfigure";
            this.cmdConfigure.Size = new System.Drawing.Size(208, 22);
            this.cmdConfigure.Text = "Configure Media Browser";
            // 
            // cmdBrowse
            // 
            this.cmdBrowse.Name = "cmdBrowse";
            this.cmdBrowse.Size = new System.Drawing.Size(208, 22);
            this.cmdBrowse.Text = "Browse Library";
            // 
            // cmdApiDocs
            // 
            this.cmdApiDocs.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.cmdStandardDocs,
            this.cmdSwagger,
            this.cmdGtihub});
            this.cmdApiDocs.Name = "cmdApiDocs";
            this.cmdApiDocs.Size = new System.Drawing.Size(208, 22);
            this.cmdApiDocs.Text = "View Api Documentation";
            // 
            // cmdStandardDocs
            // 
            this.cmdStandardDocs.Name = "cmdStandardDocs";
            this.cmdStandardDocs.Size = new System.Drawing.Size(136, 22);
            this.cmdStandardDocs.Text = "Standard";
            // 
            // cmdSwagger
            // 
            this.cmdSwagger.Name = "cmdSwagger";
            this.cmdSwagger.Size = new System.Drawing.Size(136, 22);
            this.cmdSwagger.Text = "Swagger";
            // 
            // cmdGtihub
            // 
            this.cmdGtihub.Name = "cmdGtihub";
            this.cmdGtihub.Size = new System.Drawing.Size(136, 22);
            this.cmdGtihub.Text = "Github Wiki";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 261);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "MainForm";
            this.ShowInTaskbar = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "MainForm";
            this.contextMenuStrip1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.NotifyIcon notifyIcon1;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem cmdExit;
        private System.Windows.Forms.ToolStripMenuItem cmdBrowse;
        private System.Windows.Forms.ToolStripMenuItem cmdConfigure;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripMenuItem cmdLibraryExplorer;
        private System.Windows.Forms.ToolStripMenuItem cmdRestart;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem cmdLogWindow;
        private System.Windows.Forms.ToolStripMenuItem cmdCommunity;
        private System.Windows.Forms.ToolStripMenuItem cmdApiDocs;
        private System.Windows.Forms.ToolStripMenuItem cmdStandardDocs;
        private System.Windows.Forms.ToolStripMenuItem cmdSwagger;
        private System.Windows.Forms.ToolStripMenuItem cmdGtihub;
    }
}