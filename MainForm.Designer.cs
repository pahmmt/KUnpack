namespace KUnpack
{
    partial class MainForm
    {
        /// <summary>
        ///  Biến do Designer tạo ra.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Giải phóng mọi tài nguyên đang sử dụng.
        /// </summary>
        /// <param name="disposing">true nếu cần giải phóng tài nguyên được quản lý; ngược lại false.</param>
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
        ///  Phương thức bắt buộc để hỗ trợ Designer - không chỉnh sửa
        ///  nội dung của phương thức này bằng code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            menuStrip1 = new MenuStrip();
            fileToolStripMenuItem = new ToolStripMenuItem();
            openToolStripMenuItem = new ToolStripMenuItem();
            setOutputToolStripMenuItem = new ToolStripMenuItem();
            loadListToolStripMenuItem = new ToolStripMenuItem();
            loadPathToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator1 = new ToolStripSeparator();
            exitToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator2 = new ToolStripSeparator();
            pAKnoneToolStripMenuItem = new ToolStripMenuItem();
            outputnoneToolStripMenuItem = new ToolStripMenuItem();
            listnoneToolStripMenuItem = new ToolStripMenuItem();
            extractToolStripMenuItem = new ToolStripMenuItem();
            extractSelectedToolStripMenuItem = new ToolStripMenuItem();
            extractAllToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator3 = new ToolStripSeparator();
            pauseResumeToolStripMenuItem = new ToolStripMenuItem();
            cancelExtractionToolStripMenuItem = new ToolStripMenuItem();
            statusStrip1 = new StatusStrip();
            toolStripStatusLabel1 = new ToolStripStatusLabel();
            toolStripProgressBar1 = new ToolStripProgressBar();
            tableLayoutPanel1 = new TableLayoutPanel();
            groupBox1 = new GroupBox();
            listViewFiles = new ListView();
            listViewFilesHeader1 = new ColumnHeader();
            listViewFilesHeader2 = new ColumnHeader();
            listViewFilesHeader3 = new ColumnHeader();
            listViewFilesHeader4 = new ColumnHeader();
            listViewFilesHeader5 = new ColumnHeader();
            listViewFilesHeader6 = new ColumnHeader();
            groupBox2 = new GroupBox();
            tabControl1 = new TabControl();
            tabPage1 = new TabPage();
            splitContainer1 = new SplitContainer();
            numericUpDown1 = new NumericUpDown();
            checkBox1 = new CheckBox();
            pictureBox1 = new PictureBox();
            tabPage2 = new TabPage();
            richTextBoxTextPreview = new RichTextBox();
            tabPage3 = new TabPage();
            richTextBoxHexPreview = new RichTextBox();
            groupBox3 = new GroupBox();
            listViewInfo = new ListView();
            listViewInfoHeader1 = new ColumnHeader();
            listViewInfoHeader2 = new ColumnHeader();
            groupBox4 = new GroupBox();
            richTextBoxLogs = new RichTextBox();
            openFileDialog1 = new OpenFileDialog();
            openFileDialog2 = new OpenFileDialog();
            folderBrowserDialog1 = new FolderBrowserDialog();
            animationTimer = new System.Windows.Forms.Timer();
            menuStrip1.SuspendLayout();
            statusStrip1.SuspendLayout();
            tableLayoutPanel1.SuspendLayout();
            groupBox1.SuspendLayout();
            groupBox2.SuspendLayout();
            tabControl1.SuspendLayout();
            tabPage1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numericUpDown1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            tabPage2.SuspendLayout();
            tabPage3.SuspendLayout();
            groupBox3.SuspendLayout();
            groupBox4.SuspendLayout();
            SuspendLayout();
            // 
            // menuStrip1
            // 
            menuStrip1.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem, extractToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(1008, 24);
            menuStrip1.TabIndex = 0;
            menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { openToolStripMenuItem, setOutputToolStripMenuItem, loadListToolStripMenuItem, loadPathToolStripMenuItem, toolStripSeparator1, exitToolStripMenuItem, toolStripSeparator2, pAKnoneToolStripMenuItem, outputnoneToolStripMenuItem, listnoneToolStripMenuItem });
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new Size(37, 20);
            fileToolStripMenuItem.Text = "File";
            // 
            // openToolStripMenuItem
            // 
            openToolStripMenuItem.Name = "openToolStripMenuItem";
            openToolStripMenuItem.Size = new Size(153, 22);
            openToolStripMenuItem.Text = "Mở PAK";
            openToolStripMenuItem.Click += OpenToolStripMenuItem_Click;
            // 
            // setOutputToolStripMenuItem
            // 
            setOutputToolStripMenuItem.Name = "setOutputToolStripMenuItem";
            setOutputToolStripMenuItem.Size = new Size(153, 22);
            setOutputToolStripMenuItem.Text = "Đặt mục giải nén";
            setOutputToolStripMenuItem.Click += SetOutputToolStripMenuItem_Click;
            // 
            // loadListToolStripMenuItem
            // 
            loadListToolStripMenuItem.Name = "loadListToolStripMenuItem";
            loadListToolStripMenuItem.Size = new Size(153, 22);
            loadListToolStripMenuItem.Text = "Tải danh sách";
            loadListToolStripMenuItem.Click += LoadListToolStripMenuItem_Click;
            // 
            // loadPathToolStripMenuItem
            // 
            loadPathToolStripMenuItem.Name = "loadPathToolStripMenuItem";
            loadPathToolStripMenuItem.Size = new Size(153, 22);
            loadPathToolStripMenuItem.Text = "Tải đường dẫn";
            loadPathToolStripMenuItem.Click += LoadPathToolStripMenuItem_Click;
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new Size(150, 6);
            // 
            // exitToolStripMenuItem
            // 
            exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            exitToolStripMenuItem.Size = new Size(153, 22);
            exitToolStripMenuItem.Text = "Thoát";
            exitToolStripMenuItem.Click += ExitToolStripMenuItem_Click;
            // 
            // toolStripSeparator2
            // 
            toolStripSeparator2.Name = "toolStripSeparator2";
            toolStripSeparator2.Size = new Size(150, 6);
            // 
            // pAKnoneToolStripMenuItem
            // 
            pAKnoneToolStripMenuItem.Enabled = false;
            pAKnoneToolStripMenuItem.Name = "pAKnoneToolStripMenuItem";
            pAKnoneToolStripMenuItem.Size = new Size(153, 22);
            pAKnoneToolStripMenuItem.Text = "Tệp PAK: (chưa chọn)";
            // 
            // outputnoneToolStripMenuItem
            // 
            outputnoneToolStripMenuItem.Enabled = false;
            outputnoneToolStripMenuItem.Name = "outputnoneToolStripMenuItem";
            outputnoneToolStripMenuItem.Size = new Size(153, 22);
            outputnoneToolStripMenuItem.Text = "Thư mục giải nén: (chưa chọn)";
            // 
            // listnoneToolStripMenuItem
            // 
            listnoneToolStripMenuItem.Enabled = false;
            listnoneToolStripMenuItem.Name = "listnoneToolStripMenuItem";
            listnoneToolStripMenuItem.Size = new Size(153, 22);
            listnoneToolStripMenuItem.Text = "Danh sách: (chưa chọn)";
            // 
            // extractToolStripMenuItem
            // 
            extractToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { extractSelectedToolStripMenuItem, extractAllToolStripMenuItem, toolStripSeparator3, pauseResumeToolStripMenuItem, cancelExtractionToolStripMenuItem });
            extractToolStripMenuItem.Name = "extractToolStripMenuItem";
            extractToolStripMenuItem.Size = new Size(54, 20);
            extractToolStripMenuItem.Text = "Giải nén";
            // 
            // extractSelectedToolStripMenuItem
            // 
            extractSelectedToolStripMenuItem.Name = "extractSelectedToolStripMenuItem";
            extractSelectedToolStripMenuItem.Size = new Size(156, 22);
            extractSelectedToolStripMenuItem.Text = "Giải nén chọn";
            extractSelectedToolStripMenuItem.Click += ExtractSelectedToolStripMenuItem_Click;
            // 
            // extractAllToolStripMenuItem
            // 
            extractAllToolStripMenuItem.Name = "extractAllToolStripMenuItem";
            extractAllToolStripMenuItem.Size = new Size(156, 22);
            extractAllToolStripMenuItem.Text = "Giải nén tất cả";
            extractAllToolStripMenuItem.Click += ExtractAllToolStripMenuItem_Click;
            // 
            // toolStripSeparator3
            // 
            toolStripSeparator3.Name = "toolStripSeparator3";
            toolStripSeparator3.Size = new Size(153, 6);
            // 
            // pauseResumeToolStripMenuItem
            // 
            pauseResumeToolStripMenuItem.Enabled = false;
            pauseResumeToolStripMenuItem.Name = "pauseResumeToolStripMenuItem";
            pauseResumeToolStripMenuItem.Size = new Size(156, 22);
            pauseResumeToolStripMenuItem.Text = "Tạm dừng";
            pauseResumeToolStripMenuItem.Click += PauseResumeToolStripMenuItem_Click;
            // 
            // cancelExtractionToolStripMenuItem
            // 
            cancelExtractionToolStripMenuItem.Enabled = false;
            cancelExtractionToolStripMenuItem.Name = "cancelExtractionToolStripMenuItem";
            cancelExtractionToolStripMenuItem.Size = new Size(156, 22);
            cancelExtractionToolStripMenuItem.Text = "Hủy";
            cancelExtractionToolStripMenuItem.Click += CancelExtractionToolStripMenuItem_Click;
            // 
            // statusStrip1
            // 
            statusStrip1.Items.AddRange(new ToolStripItem[] { toolStripStatusLabel1, toolStripProgressBar1 });
            statusStrip1.Location = new Point(0, 658);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new Size(1008, 22);
            statusStrip1.TabIndex = 1;
            statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabel1
            // 
            toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            toolStripStatusLabel1.Size = new Size(891, 17);
            toolStripStatusLabel1.Spring = true;
            toolStripStatusLabel1.Text = "Sẵn sàng";
            toolStripStatusLabel1.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // toolStripProgressBar1
            // 
            toolStripProgressBar1.Name = "toolStripProgressBar1";
            toolStripProgressBar1.Size = new Size(100, 16);
            toolStripProgressBar1.Visible = false;
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 2;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70F));
            tableLayoutPanel1.Controls.Add(groupBox1, 0, 0);
            tableLayoutPanel1.Controls.Add(groupBox2, 1, 0);
            tableLayoutPanel1.Controls.Add(groupBox3, 0, 1);
            tableLayoutPanel1.Controls.Add(groupBox4, 1, 1);
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Location = new Point(0, 24);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 2;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 70F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 30F));
            tableLayoutPanel1.Size = new Size(1008, 634);
            tableLayoutPanel1.TabIndex = 2;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(listViewFiles);
            groupBox1.Dock = DockStyle.Fill;
            groupBox1.Location = new Point(3, 3);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(296, 437);
            groupBox1.TabIndex = 0;
            groupBox1.TabStop = false;
            groupBox1.Text = "Files";
            // 
            // listViewFiles
            // 
            listViewFiles.Columns.AddRange(new ColumnHeader[] { listViewFilesHeader1, listViewFilesHeader2, listViewFilesHeader3, listViewFilesHeader4, listViewFilesHeader5, listViewFilesHeader6 });
            listViewFiles.Dock = DockStyle.Fill;
            listViewFiles.FullRowSelect = true;
            listViewFiles.Location = new Point(3, 18);
            listViewFiles.MultiSelect = true;
            listViewFiles.Name = "listViewFiles";
            listViewFiles.Size = new Size(290, 416);
            listViewFiles.TabIndex = 0;
            listViewFiles.UseCompatibleStateImageBehavior = false;
            listViewFiles.View = View.Details;
            listViewFiles.VirtualMode = true;
            // 
            // listViewFilesHeader1
            // 
            listViewFilesHeader1.Text = "#";
            // 
            // listViewFilesHeader2
            // 
            listViewFilesHeader2.Text = "ID";
            // 
            // listViewFilesHeader3
            // 
            listViewFilesHeader3.Text = "Kích thước nén";
            // 
            // listViewFilesHeader4
            // 
            listViewFilesHeader4.Text = "Kích thước gốc";
            // 
            // listViewFilesHeader5
            // 
            listViewFilesHeader5.Text = "Loại nén";
            // 
            // listViewFilesHeader6
            // 
            listViewFilesHeader6.Text = "Đường dẫn";
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(tabControl1);
            groupBox2.Dock = DockStyle.Fill;
            groupBox2.Location = new Point(305, 3);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new Size(700, 437);
            groupBox2.TabIndex = 1;
            groupBox2.TabStop = false;
            groupBox2.Text = "Xem trước";
            // 
            // tabControl1
            // 
            tabControl1.Controls.Add(tabPage1);
            tabControl1.Controls.Add(tabPage2);
            tabControl1.Controls.Add(tabPage3);
            tabControl1.Dock = DockStyle.Fill;
            tabControl1.Location = new Point(3, 18);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new Size(694, 416);
            tabControl1.TabIndex = 0;
            // 
            // tabPage1
            // 
            tabPage1.Controls.Add(splitContainer1);
            tabPage1.Location = new Point(4, 23);
            tabPage1.Name = "tabPage1";
            tabPage1.Padding = new Padding(3);
            tabPage1.Size = new Size(686, 389);
            tabPage1.TabIndex = 0;
            tabPage1.Text = "Ảnh";
            tabPage1.UseVisualStyleBackColor = true;
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = DockStyle.Fill;
            splitContainer1.Location = new Point(3, 3);
            splitContainer1.Name = "splitContainer1";
            splitContainer1.Orientation = Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(numericUpDown1);
            splitContainer1.Panel1.Controls.Add(checkBox1);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.BackColor = Color.DimGray;
            splitContainer1.Panel2.Controls.Add(pictureBox1);
            splitContainer1.Size = new Size(680, 383);
            splitContainer1.SplitterDistance = 26;
            splitContainer1.TabIndex = 0;
            // 
            // numericUpDown1
            // 
            numericUpDown1.Enabled = false;
            numericUpDown1.Location = new Point(91, 2);
            numericUpDown1.Maximum = new decimal(new int[] { 120, 0, 0, 0 });
            numericUpDown1.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numericUpDown1.Name = "numericUpDown1";
            numericUpDown1.Size = new Size(120, 22);
            numericUpDown1.TabIndex = 1;
            numericUpDown1.Value = new decimal(new int[] { 24, 0, 0, 0 });
            numericUpDown1.ValueChanged += NumericUpDown1_ValueChanged;
            // 
            // checkBox1
            // 
            checkBox1.AutoSize = true;
            checkBox1.Enabled = false;
            checkBox1.Location = new Point(3, 3);
            checkBox1.Name = "checkBox1";
            checkBox1.Size = new Size(87, 18);
            checkBox1.TabIndex = 0;
            checkBox1.Text = "Phát FPS:";
            checkBox1.UseVisualStyleBackColor = true;
            checkBox1.CheckedChanged += CheckBox1_CheckedChanged;
            // 
            // pictureBox1
            // 
            pictureBox1.Dock = DockStyle.Fill;
            pictureBox1.Location = new Point(0, 0);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(680, 353);
            pictureBox1.TabIndex = 0;
            pictureBox1.TabStop = false;
            // 
            // tabPage2
            // 
            tabPage2.Controls.Add(richTextBoxTextPreview);
            tabPage2.Location = new Point(4, 23);
            tabPage2.Name = "tabPage2";
            tabPage2.Padding = new Padding(3);
            tabPage2.Size = new Size(686, 389);
            tabPage2.TabIndex = 1;
            tabPage2.Text = "Văn bản";
            tabPage2.UseVisualStyleBackColor = true;
            // 
            // richTextBoxTextPreview
            // 
            richTextBoxTextPreview.Dock = DockStyle.Fill;
            richTextBoxTextPreview.Font = new Font("Consolas", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            richTextBoxTextPreview.Location = new Point(3, 3);
            richTextBoxTextPreview.Name = "richTextBoxTextPreview";
            richTextBoxTextPreview.Size = new Size(680, 383);
            richTextBoxTextPreview.TabIndex = 0;
            richTextBoxTextPreview.Text = "";
            richTextBoxTextPreview.WordWrap = false;
            // 
            // tabPage3
            // 
            tabPage3.Controls.Add(richTextBoxHexPreview);
            tabPage3.Location = new Point(4, 23);
            tabPage3.Name = "tabPage3";
            tabPage3.Padding = new Padding(3);
            tabPage3.Size = new Size(686, 389);
            tabPage3.TabIndex = 2;
            tabPage3.Text = "Hex";
            tabPage3.UseVisualStyleBackColor = true;
            // 
            // richTextBoxHexPreview
            // 
            richTextBoxHexPreview.Dock = DockStyle.Fill;
            richTextBoxHexPreview.Font = new Font("Consolas", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            richTextBoxHexPreview.Location = new Point(3, 3);
            richTextBoxHexPreview.Name = "richTextBoxHexPreview";
            richTextBoxHexPreview.Size = new Size(680, 383);
            richTextBoxHexPreview.TabIndex = 0;
            richTextBoxHexPreview.Text = "";
            richTextBoxHexPreview.WordWrap = false;
            // 
            // groupBox3
            // 
            groupBox3.Controls.Add(listViewInfo);
            groupBox3.Dock = DockStyle.Fill;
            groupBox3.Location = new Point(3, 446);
            groupBox3.Name = "groupBox3";
            groupBox3.Size = new Size(296, 185);
            groupBox3.TabIndex = 2;
            groupBox3.TabStop = false;
            groupBox3.Text = "Thông tin";
            // 
            // listViewInfo
            // 
            listViewInfo.Columns.AddRange(new ColumnHeader[] { listViewInfoHeader1, listViewInfoHeader2 });
            listViewInfo.Dock = DockStyle.Fill;
            listViewInfo.FullRowSelect = true;
            listViewInfo.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            listViewInfo.Location = new Point(3, 18);
            listViewInfo.Name = "listViewInfo";
            listViewInfo.Size = new Size(290, 164);
            listViewInfo.TabIndex = 0;
            listViewInfo.UseCompatibleStateImageBehavior = false;
            listViewInfo.View = View.Details;
            // 
            // listViewInfoHeader1
            // 
            listViewInfoHeader1.Text = "Tên";
            // 
            // listViewInfoHeader2
            // 
            listViewInfoHeader2.Text = "Giá trị";
            // 
            // groupBox4
            // 
            groupBox4.Controls.Add(richTextBoxLogs);
            groupBox4.Dock = DockStyle.Fill;
            groupBox4.Location = new Point(305, 446);
            groupBox4.Name = "groupBox4";
            groupBox4.Size = new Size(700, 185);
            groupBox4.TabIndex = 3;
            groupBox4.TabStop = false;
            groupBox4.Text = "Logs";
            // 
            // richTextBoxLogs
            // 
            richTextBoxLogs.Dock = DockStyle.Fill;
            richTextBoxLogs.Location = new Point(3, 18);
            richTextBoxLogs.Name = "richTextBoxLogs";
            richTextBoxLogs.Size = new Size(694, 164);
            richTextBoxLogs.TabIndex = 0;
            richTextBoxLogs.Text = "";
            // 
            // openFileDialog1
            // 
            openFileDialog1.FileName = "Chọn tệp PAK";
            openFileDialog1.Filter = "Tệp PAK (*.pak)|*.pak|Tất cả tệp (*.*)|*.*";
            openFileDialog1.RestoreDirectory = true;
            // 
            // openFileDialog2
            // 
            openFileDialog2.FileName = "Chọn tệp văn bản";
            openFileDialog2.Filter = "Tệp văn bản (*.txt)|*.txt|Tất cả tệp (*.*)|*.*";
            openFileDialog2.RestoreDirectory = true;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 14F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1008, 680);
            Controls.Add(tableLayoutPanel1);
            Controls.Add(statusStrip1);
            Controls.Add(menuStrip1);
            Font = new Font("Tahoma", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MainMenuStrip = menuStrip1;
            MinimumSize = new Size(1024, 719);
            Name = "MainForm";
            Text = "Kingsoft JXSJ Unpack Tool By PMT :: github.com/pahmmt";
            Load += MainForm_Load;
            Resize += MainForm_Resize;
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            tableLayoutPanel1.ResumeLayout(false);
            groupBox1.ResumeLayout(false);
            groupBox2.ResumeLayout(false);
            tabControl1.ResumeLayout(false);
            tabPage1.ResumeLayout(false);
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel1.PerformLayout();
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)numericUpDown1).EndInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            tabPage2.ResumeLayout(false);
            tabPage3.ResumeLayout(false);
            groupBox3.ResumeLayout(false);
            groupBox4.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private MenuStrip menuStrip1;
        private ToolStripMenuItem fileToolStripMenuItem;
        private ToolStripMenuItem openToolStripMenuItem;
        private ToolStripMenuItem setOutputToolStripMenuItem;
        private ToolStripMenuItem loadListToolStripMenuItem;
        private ToolStripMenuItem loadPathToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripMenuItem exitToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator2;
        private ToolStripMenuItem pAKnoneToolStripMenuItem;
        private ToolStripMenuItem outputnoneToolStripMenuItem;
        private ToolStripMenuItem listnoneToolStripMenuItem;
        private ToolStripMenuItem extractToolStripMenuItem;
        private ToolStripMenuItem extractSelectedToolStripMenuItem;
        private ToolStripMenuItem extractAllToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator3;
        private ToolStripMenuItem pauseResumeToolStripMenuItem;
        private ToolStripMenuItem cancelExtractionToolStripMenuItem;
        private StatusStrip statusStrip1;
        private ToolStripStatusLabel toolStripStatusLabel1;
        private ToolStripProgressBar toolStripProgressBar1;
        private TableLayoutPanel tableLayoutPanel1;
        private GroupBox groupBox1;
        private GroupBox groupBox2;
        private GroupBox groupBox3;
        private GroupBox groupBox4;
        private ListView listViewFiles;
        private TabControl tabControl1;
        private TabPage tabPage1;
        private TabPage tabPage2;
        private ListView listViewInfo;
        private RichTextBox richTextBoxLogs;
        private TabPage tabPage3;
        private SplitContainer splitContainer1;
        private CheckBox checkBox1;
        private NumericUpDown numericUpDown1;
        private RichTextBox richTextBoxTextPreview;
        private RichTextBox richTextBoxHexPreview;
        private ColumnHeader listViewInfoHeader1;
        private ColumnHeader listViewInfoHeader2;
        private ColumnHeader listViewFilesHeader1;
        private ColumnHeader listViewFilesHeader2;
        private ColumnHeader listViewFilesHeader3;
        private ColumnHeader listViewFilesHeader4;
        private ColumnHeader listViewFilesHeader5;
        private ColumnHeader listViewFilesHeader6;
        private OpenFileDialog openFileDialog1;
        private OpenFileDialog openFileDialog2;
        private FolderBrowserDialog folderBrowserDialog1;
        private PictureBox pictureBox1;
        private System.Windows.Forms.Timer animationTimer;
    }
}
