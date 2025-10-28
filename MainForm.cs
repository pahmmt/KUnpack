using KUnpack.EngineSharp;
using KUnpack.Helpers;
using System.Security;
using System.Text;
using System.Drawing.Drawing2D;

namespace KUnpack
{
    public partial class MainForm : Form
    {
        #region Constants & Fields
        private const int HexPreviewMaxBytes = 1024 * 1024;

        private XPackFile? currentPack = null;
        private string? currentPackPath = null;
        private List<XPackIndexInfo> fileIndexList = new List<XPackIndexInfo>();
        private Dictionary<uint, string> idToPathMap = new Dictionary<uint, string>();
        private string? outputFolder = null;
        private bool isExtracting = false;
        private bool isPaused = false;
        private bool cancelRequested = false;
		// Sorting state for listViewFiles (virtual mode)
		private int currentSortColumn = -1; // -1 means no sorting
		private SortOrder currentSortOrder = SortOrder.None;
		private readonly string[] listViewFilesHeaderTexts = new[] { "#", "ID", "Packed Size", "Original Size", "Compression", "Path" };
		private Dictionary<uint, int> originalIndexById = new Dictionary<uint, int>();
		private List<int> sortedIndices = new List<int>();

        // Animation helper
        private SpriteAnimationHelper spriteHelper = new SpriteAnimationHelper();
        // Giữ ảnh gốc đang preview (chuẩn/cur/icon). Sprite không dùng trường này
        private Bitmap? originalImageForPreview = null;
        #endregion

        #region Constructors
        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            // Đăng ký sự kiện cho listViewFiles virtual mode
            listViewFiles.RetrieveVirtualItem += ListViewFiles_RetrieveVirtualItem;
            listViewFiles.SelectedIndexChanged += ListViewFiles_SelectedIndexChanged;
			listViewFiles.ColumnClick += ListViewFiles_ColumnClick;
            
            // Khởi tạo timer animation
            animationTimer.Tick += AnimationTimer_Tick;
            
            // Đặt độ rộng cột của bảng với giới hạn tối đa
            UIHelper.SetTableLayoutColumnWidth(tableLayoutPanel1, 0, 30f, 600);
            
            // Tự cân chỉnh độ rộng các cột
            UIHelper.AdjustListViewColumns(listViewFiles);
            UIHelper.AdjustListViewColumns(listViewInfo);
            
            // Cập nhật trạng thái menu
            UpdateMenuState();
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            // Đặt độ rộng cột của bảng với giới hạn tối đa
            UIHelper.SetTableLayoutColumnWidth(tableLayoutPanel1, 0, 30f, 600);
            
            // Tự cân chỉnh độ rộng các cột
            UIHelper.AdjustListViewColumns(listViewFiles);
            UIHelper.AdjustListViewColumns(listViewInfo);

            // Tự làm mới scale ảnh đang hiển thị nếu có
            RefreshCurrentPreviewScale();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            
            // Dừng animation và dọn dẹp sprite
            StopAnimation();
            spriteHelper.Dispose();
            if (originalImageForPreview != null)
            {
                originalImageForPreview.Dispose();
                originalImageForPreview = null;
            }
            
            // Huỷ trích xuất nếu đang chạy
            if (isExtracting)
            {
                cancelRequested = true;
                isPaused = false; // Bỏ tạm dừng để vòng lặp có thể thoát
                
                // Chờ một chút để thoát an toàn
                for (int i = 0; i < 10 && isExtracting; i++)
                {
                    Application.DoEvents();
                    System.Threading.Thread.Sleep(100);
                }
            }
            
            // Đóng và giải phóng pack file khi đóng form
            if (currentPack != null)
            {
                currentPack.Close();
                currentPack.Dispose();
                currentPack = null;
            }
        }
        #endregion

        #region Menu handlers
        private void OpenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() != DialogResult.OK)
                return;

            string filePath = openFileDialog1.FileName;

            try
            {
                // Đóng pack hiện tại nếu có
                if (currentPack != null)
                {
                    currentPack.Close();
                    currentPack.Dispose();
                    currentPack = null;
                }

                // Mở pack file mới
                currentPack = new XPackFile();
                if (!currentPack.Open(filePath, 0))
                {
                    MessageBox.Show("Không mở được pak: " + filePath);
                    currentPack.Dispose();
                    currentPack = null;
                    return;
                }

                currentPackPath = filePath;
                int total = currentPack.GetElemFileCount();

				// Load danh sách index vào list
                fileIndexList.Clear();
                for (int i = 0; i < total; i++)
                {
                    XPackIndexInfo? indexInfo = currentPack.GetIndexInfo(i);
                    if (indexInfo.HasValue)
                    {
                        fileIndexList.Add(indexInfo.Value);
                    }
                }

				// Lưu lại thứ tự gốc theo ID để có thể sort theo cột '#'
				originalIndexById.Clear();
				for (int i = 0; i < fileIndexList.Count; i++)
				{
					uint id = fileIndexList[i].uId;
					if (!originalIndexById.ContainsKey(id))
						originalIndexById[id] = i;
				}

				// Cập nhật listView với virtual mode
				sortedIndices = Enumerable.Range(0, fileIndexList.Count).ToList();
				listViewFiles.VirtualListSize = fileIndexList.Count;
				ApplySortingIfAny();
				UpdateSortHeaderTexts();
				listViewFiles.Invalidate();

                // Cập nhật menu
                pAKnoneToolStripMenuItem.Text = $"PAK: {Path.GetFileName(filePath)}";

                // Ghi log
                LogMessage($"Đã mở pak: {filePath}");
                LogMessage($"Tổng số file: {total}");
                
                // Kiểm tra xem có mapping nào không
                if (idToPathMap.Count > 0)
                {
                    int mappedFiles = 0;
                    foreach (var indexInfo in fileIndexList)
                    {
                        if (idToPathMap.ContainsKey(indexInfo.uId))
                            mappedFiles++;
                    }
                    LogMessage($"Files with path mapping: {mappedFiles}/{total} ({(double)mappedFiles / total * 100:F1}%)");
                }
                else
                {
                    LogMessage("No path mappings loaded. Use 'File → Load List' to load path mappings.");
                }

                toolStripStatusLabel1.Text = $"Loaded {total} files from {Path.GetFileName(filePath)}";

                // Auto-resize columns
                UIHelper.AdjustListViewColumns(listViewFiles);
                UIHelper.AdjustListViewColumns(listViewInfo);
                
                // Update menu state
                UpdateMenuState();
            }
            catch (SecurityException ex)
            {
                MessageBox.Show($"Security error.\n\nError message: {ex.Message}\n\nDetails:\n\n{ex.StackTrace}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening PAK file.\n\nError message: {ex.Message}\n\nDetails:\n\n{ex.StackTrace}");
            }
        }

        private void SetOutputToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    string selectedFolder = folderBrowserDialog1.SelectedPath;
                    
                    // Validate write permissions
                    if (!ValidateFolderWritePermission(selectedFolder))
                    {
                        MessageBox.Show($"Permission denied.\n\nYou do not have write permission to:\n{selectedFolder}", 
                            "Permission Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    outputFolder = selectedFolder;
                    outputnoneToolStripMenuItem.Text = $"Output: {outputFolder}";
                    LogMessage($"Output folder set to: {outputFolder}");
                    UpdateMenuState();
                }
                catch (SecurityException ex)
                {
                    MessageBox.Show($"Security error.\n\nError message: {ex.Message}\n\n" +
                    $"Details:\n\n{ex.StackTrace}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error setting output folder.\n\nError message: {ex.Message}", 
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>
        /// Validates if the user has write permission to the folder
        /// </summary>
        private bool ValidateFolderWritePermission(string folderPath)
        {
            try
            {
                string testFile = Path.Combine(folderPath, ".kunpack_write_test");
                File.WriteAllText(testFile, "test");
                File.Delete(testFile);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void LoadListToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialog2.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    string listPath = openFileDialog2.FileName;
                    LoadFileList(listPath);
                    
                    listnoneToolStripMenuItem.Text = $"List: {Path.GetFileName(listPath)}";
                    
					// Refresh listview để hiển thị path mới
					ApplySortingIfAny(); // resort if sorting by Path
					UpdateSortHeaderTexts();
					UIHelper.AdjustListViewColumns(listViewFiles);
					listViewFiles.Invalidate();
                }
                catch (SecurityException ex)
                {
                    MessageBox.Show($"Security error.\n\nError message: {ex.Message}\n\n" +
                    $"Details:\n\n{ex.StackTrace}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading list file.\n\nError message: {ex.Message}\n\nDetails:\n\n{ex.StackTrace}");
                }
            }
        }

        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

		private void ExtractSelectedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (currentPack == null || listViewFiles.SelectedIndices.Count == 0)
                return;

			var indices = listViewFiles.SelectedIndices
				.Cast<int>()
				.Select(GetPackIndexFromDisplayIndex)
				.ToList();
            PerformExtraction(indices);
        }

        private void ExtractAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (currentPack == null || fileIndexList.Count == 0)
                return;

			// Use current order? Keep original pack order for Extract All
			var indices = Enumerable.Range(0, fileIndexList.Count).ToList();
            PerformExtraction(indices);
        }

        private void PauseResumeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!isExtracting)
                return;

            isPaused = !isPaused;
            
            if (isPaused)
            {
                pauseResumeToolStripMenuItem.Text = "Resume";
                LogMessage("Extraction paused");
            }
            else
            {
                pauseResumeToolStripMenuItem.Text = "Pause";
                LogMessage("Extraction resumed");
            }
        }

        private void CancelExtractionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!isExtracting)
                return;

            cancelRequested = true;
            isPaused = false; // Bỏ tạm dừng nếu đang tạm dừng
            LogMessage("Cancellation requested...");
        }
        #endregion

        #region ListView events
		private void ListViewFiles_RetrieveVirtualItem(object? sender, RetrieveVirtualItemEventArgs e)
        {
			if (e.ItemIndex < 0 || e.ItemIndex >= fileIndexList.Count)
                return;

			int packIndex = GetPackIndexFromDisplayIndex(e.ItemIndex);
			XPackIndexInfo indexInfo = fileIndexList[packIndex];

			// Tạo ListViewItem với dữ liệu
			ListViewItem item = new ListViewItem((packIndex + 1).ToString()); // # hiển thị index gốc (bắt đầu từ 1)
            item.SubItems.Add(indexInfo.uId.ToString("X8")); // ID (dạng hex)
            
            // Tính kích thước đã nén
            int packedSize = unchecked(indexInfo.lCompressSizeFlag & (~(int)XPACK_METHOD.TYPE_FILTER));
            item.SubItems.Add(ExtractionHelper.FormatFileSize(packedSize)); // Kích thước nén (P.Size)
            
            item.SubItems.Add(ExtractionHelper.FormatFileSize(indexInfo.lSize)); // Kích thước gốc (O.Size)
            
            // Lấy path từ mapping hoặc dùng ID
            string path = idToPathMap.ContainsKey(indexInfo.uId) 
                ? idToPathMap[indexInfo.uId] 
                : $"0x{indexInfo.uId:X8}";
			int compressionMethod = unchecked(indexInfo.lCompressSizeFlag & (int)XPACK_METHOD.TYPE_FILTER);
			string methodStr = ExtractionHelper.GetCompressionMethodName(compressionMethod);
            item.SubItems.Add(methodStr); // Compression method
            item.SubItems.Add(path); // Path

            e.Item = item;
        }

		private void ListViewFiles_SelectedIndexChanged(object? sender, EventArgs e)
        {
            listViewInfo.Items.Clear();
            richTextBoxHexPreview.Clear();
            richTextBoxTextPreview.Clear();
            pictureBox1.Image = null;

            if (listViewFiles.SelectedIndices.Count == 0)
            {
                // Không có lựa chọn → tắt cả 3 tab
                tabPage1.Enabled = false; // Image
                tabPage2.Enabled = false; // Text
                tabPage3.Enabled = false; // Hex
                return;
            }


			int selectedDisplayIndex = listViewFiles.SelectedIndices[0];
			if (selectedDisplayIndex < 0 || selectedDisplayIndex >= fileIndexList.Count)
                return;

			int packIndex = GetPackIndexFromDisplayIndex(selectedDisplayIndex);
			XPackIndexInfo indexInfo = fileIndexList[packIndex];

            // Đọc dữ liệu file để detect type và hiển thị hex
            byte[]? fileData = null;
            string detectedType = "Unknown";
            
            if (currentPack != null)
            {
                try
                {
					fileData = currentPack.ReadElemFileByIndex(packIndex);
                    if (fileData != null && fileData.Length > 0)
                    {
                        detectedType = FileTypeDetector.DetectFileType(fileData);
                        DisplayHexPreview(fileData);
                        
                        // Tự chuyển tab phù hợp và đổ dữ liệu xem trước (logic inline từ UpdatePreviewTabs)
                        bool isImage = detectedType.Contains("(Image)");
                        bool isText = detectedType == "Text" || detectedType.Contains("(Text)");
                        bool isSpr = detectedType.Contains("SPR");

                        if (isText)
                        {
                            DisplayTextPreview(fileData);
                            tabControl1.SelectedTab = tabPage2; // Tab Văn bản
                            UpdatePreviewTabsAvailability(isText: true, isImageOrSpr: false);
                        }
                        else if (isImage || isSpr)
                        {
                            DisplayImagePreview(fileData);
                            tabControl1.SelectedTab = tabPage1; // Tab Ảnh
                            UpdatePreviewTabsAvailability(isText: false, isImageOrSpr: true);
                        }
                        else
                        {
                            tabControl1.SelectedTab = tabPage3; // Tab Hex
                            UpdatePreviewTabsAvailability(isText: false, isImageOrSpr: false);
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogMessage($"Error reading file data: {ex.Message}");
                }
            }

            // Hiển thị thông tin chi tiết trong listViewInfo
			listViewInfo.Items.Add(new ListViewItem(new[] { "Index", packIndex.ToString() }));
            listViewInfo.Items.Add(new ListViewItem(new[] { "ID", $"0x{indexInfo.uId:X8}" }));
            
            // Hiển thị path nếu có
            if (idToPathMap.ContainsKey(indexInfo.uId))
            {
                listViewInfo.Items.Add(new ListViewItem(new[] { "Path", idToPathMap[indexInfo.uId] }));
            }
            
            listViewInfo.Items.Add(new ListViewItem(new[] { "Offset", $"0x{indexInfo.uOffset:X8}" }));
            
            int packedSize = unchecked(indexInfo.lCompressSizeFlag & (~(int)XPACK_METHOD.TYPE_FILTER));
            listViewInfo.Items.Add(new ListViewItem(new[] { "Packed Size", $"{ExtractionHelper.FormatFileSize(packedSize)} ({packedSize} bytes)" }));
            listViewInfo.Items.Add(new ListViewItem(new[] { "Original Size", $"{ExtractionHelper.FormatFileSize(indexInfo.lSize)} ({indexInfo.lSize} bytes)" }));

            // Xác định phương thức nén
            int compressionMethod = unchecked(indexInfo.lCompressSizeFlag & (int)XPACK_METHOD.TYPE_FILTER);
            string methodStr = ExtractionHelper.GetCompressionMethodName(compressionMethod);
            listViewInfo.Items.Add(new ListViewItem(new[] { "Compression", methodStr }));

            // Tính tỷ lệ nén
            if (indexInfo.lSize > 0)
            {
                double ratio = (double)packedSize / indexInfo.lSize * 100.0;
                listViewInfo.Items.Add(new ListViewItem(new[] { "Compression Ratio", $"{ratio:F2}%" }));
            }

            // Hiển thị file type đã detect
            listViewInfo.Items.Add(new ListViewItem(new[] { "Detected Type", detectedType }));

            UIHelper.AdjustListViewColumns(listViewInfo);
            
            // Update menu state
            UpdateMenuState();
        }
        
        // Bật/tắt tab dựa trên loại nội dung đang preview
        private void UpdatePreviewTabsAvailability(bool isText, bool isImageOrSpr)
        {
            if (isText)
            {
                // Text: tắt Image, bật Text + Hex
                tabPage1.Enabled = false; // Image
                tabPage2.Enabled = true;  // Text
                tabPage3.Enabled = true;  // Hex
            }


            else if (isImageOrSpr)
            {
                // Image/Sprite: bật Image + Hex, tắt Text
                tabPage1.Enabled = true;  // Image
                tabPage2.Enabled = false; // Text
                tabPage3.Enabled = true;  // Hex
            }
            else
            {
                // Không xác định: tắt Image + Text, bật Hex
                tabPage1.Enabled = false; // Image
                tabPage2.Enabled = false; // Text
                tabPage3.Enabled = true;  // Hex
            }
        }

		// Column click sorting for listViewFiles (virtual mode)
		private void ListViewFiles_ColumnClick(object? sender, ColumnClickEventArgs e)
		{
			if (e.Column == currentSortColumn)
			{
				currentSortOrder = currentSortOrder == SortOrder.Ascending ? SortOrder.Descending : SortOrder.Ascending;
			}
			else
			{
				currentSortColumn = e.Column;
				currentSortOrder = SortOrder.Ascending;
			}

			ApplySortingIfAny();
			UpdateSortHeaderTexts();
			listViewFiles.Invalidate();
		}

		private void ApplySortingIfAny()
		{
			// Initialize identity order if needed
			sortedIndices = Enumerable.Range(0, fileIndexList.Count).ToList();
			if (currentSortColumn < 0 || currentSortOrder == SortOrder.None || fileIndexList.Count <= 1)
				return;

			Comparison<int> comparison = (ia, ib) => 0;
			switch (currentSortColumn)
			{
				case 0: // '#': original index order
					comparison = (ia, ib) => ia.CompareTo(ib);
					break;
				case 1: // ID (hex)
					comparison = (ia, ib) => fileIndexList[ia].uId.CompareTo(fileIndexList[ib].uId);
					break;
				case 2: // Packed Size
					comparison = (ia, ib) =>
					{
						int px = unchecked(fileIndexList[ia].lCompressSizeFlag & (~(int)XPACK_METHOD.TYPE_FILTER));
						int py = unchecked(fileIndexList[ib].lCompressSizeFlag & (~(int)XPACK_METHOD.TYPE_FILTER));
						return px.CompareTo(py);
					};
					break;
				case 3: // Original Size
					comparison = (ia, ib) => fileIndexList[ia].lSize.CompareTo(fileIndexList[ib].lSize);
					break;
				case 4: // Compression method (by name)
					comparison = (ia, ib) => string.Compare(GetCompressionName(fileIndexList[ia]), GetCompressionName(fileIndexList[ib]), StringComparison.CurrentCultureIgnoreCase);
					break;
				case 5: // Path (from mapping or hex ID)
					comparison = (ia, ib) => string.Compare(GetPathFor(fileIndexList[ia]), GetPathFor(fileIndexList[ib]), StringComparison.CurrentCultureIgnoreCase);
					break;
				default:
					comparison = (ia, ib) => 0;
					break;
			}

			sortedIndices.Sort((a, b) =>
			{
				int res = comparison(a, b);
				return currentSortOrder == SortOrder.Descending ? -res : res;
			});
		}

		private string GetPathFor(in XPackIndexInfo info)
		{
			if (idToPathMap.ContainsKey(info.uId))
				return idToPathMap[info.uId];
			return $"0x{info.uId:X8}";
		}

		private string GetCompressionName(in XPackIndexInfo info)
		{
			int compressionMethod = unchecked(info.lCompressSizeFlag & (int)XPACK_METHOD.TYPE_FILTER);
			return ExtractionHelper.GetCompressionMethodName(compressionMethod);
		}

		private void UpdateSortHeaderTexts()
		{
			// Reset to base texts with optional arrow for current sort column
			ColumnHeader[] headers = new[] { listViewFilesHeader1, listViewFilesHeader2, listViewFilesHeader3, listViewFilesHeader4, listViewFilesHeader5, listViewFilesHeader6 };
			for (int i = 0; i < headers.Length && i < listViewFilesHeaderTexts.Length; i++)
			{
				string arrow = (i == currentSortColumn) ? (currentSortOrder == SortOrder.Descending ? " ▼" : " ▲") : string.Empty;
				headers[i].Text = listViewFilesHeaderTexts[i] + arrow;
			}
		}

		private int GetPackIndexFromDisplayIndex(int displayIndex)
		{
			if (displayIndex < 0 || displayIndex >= fileIndexList.Count)
				return displayIndex;
			if (sortedIndices == null || sortedIndices.Count != fileIndexList.Count)
				return displayIndex;
			return sortedIndices[displayIndex];
		}
        #endregion

        #region Extraction
        private async void PerformExtraction(List<int> indices)
        {
            // Hỏi thư mục xuất nếu chưa có (logic inline từ EnsureOutputFolder)
            if (string.IsNullOrEmpty(outputFolder))
            {
                if (folderBrowserDialog1.ShowDialog() != DialogResult.OK)
                    return;
                outputFolder = folderBrowserDialog1.SelectedPath;
                outputnoneToolStripMenuItem.Text = $"Output: {outputFolder}";
            }

            try
            {
                SetExtractionUI(true, false);
                cancelRequested = false;

                int successCount = 0;
                int failCount = 0;
                int totalCount = indices.Count;

                toolStripProgressBar1.Maximum = totalCount;
                toolStripProgressBar1.Value = 0;

                // Run extraction on background thread to avoid UI freeze
                await Task.Run(() => PerformExtractionBackground(indices, ref successCount, ref failCount));

                LogMessage($"Extraction complete: {successCount} succeeded, {failCount} failed");
                if (!cancelRequested)
                {
                    MessageBox.Show($"Extracted {successCount} file(s) to:\n{outputFolder}", "Extract Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error extracting files:\n{ex.Message}", "Extract Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                LogMessage($"Extraction error: {ex.Message}");
            }
            finally
            {
                SetExtractionUI(false, false);
                toolStripStatusLabel1.Text = $"Loaded {fileIndexList.Count} files from {Path.GetFileName(currentPackPath)}";
            }
        }

        /// <summary>
        /// Background extraction logic - runs on thread pool to avoid blocking UI
        /// </summary>
        private void PerformExtractionBackground(List<int> indices, ref int successCount, ref int failCount)
        {
            foreach (int index in indices)
            {
                // Check pause/cancel state - use Task.Delay instead of Thread.Sleep for async
                while (isPaused && !cancelRequested)
                {
                    System.Threading.Thread.Sleep(50);  // Shorter sleep for background thread
                }

                if (cancelRequested)
                {
                    LogMessage("Extraction cancelled by user");
                    break;
                }

                if (index < 0 || index >= fileIndexList.Count)
                    continue;

                if (ExtractionHelper.ExtractSingleFile(currentPack!, fileIndexList, index, outputFolder!, idToPathMap))
                    successCount++;
                else
                {
                    failCount++;
                    LogMessage($"Failed to extract file at index {index}");
                }

                int processedCount = successCount + failCount;
                
                // Update UI on main thread using Invoke
                this.Invoke(() =>
                {
                    toolStripProgressBar1.Value = Math.Min(processedCount, toolStripProgressBar1.Maximum);
                    toolStripStatusLabel1.Text = $"Extracting... {processedCount}/{indices.Count}";
                });
            }
        }

        private void SetExtractionUI(bool extracting, bool isSelected)
        {
            isExtracting = extracting;
            isPaused = false; // Đặt lại trạng thái tạm dừng

            UIHelper.SetExtractionUI(extracting, isSelected,
                openToolStripMenuItem, setOutputToolStripMenuItem, loadListToolStripMenuItem,
                extractSelectedToolStripMenuItem, extractAllToolStripMenuItem,
                pauseResumeToolStripMenuItem, cancelExtractionToolStripMenuItem, toolStripProgressBar1);

            if (!extracting)
            {
                // Cập nhật trạng thái menu (khôi phục các nút trích xuất)
                UpdateMenuState();
            }
        }

        private void UpdateMenuState()
        {
            bool hasPack = currentPack != null;
			int selectedCount = listViewFiles.SelectedIndices.Count;
            int totalFiles = fileIndexList.Count;

            UIHelper.UpdateMenuState(hasPack, selectedCount, totalFiles, isExtracting,
                extractAllToolStripMenuItem, extractSelectedToolStripMenuItem);
        }
        #endregion

        #region Preview & animation
        private void DisplayHexPreview(byte[] data)
        {
            int bytesToShow = Math.Min(data.Length, HexPreviewMaxBytes);
            var sb = new StringBuilder();
            
            for (int i = 0; i < bytesToShow; i += 16)
            {
                sb.AppendFormat("{0:X8}  ", i);

                for (int j = 0; j < 16; j++)
                {
                    if (i + j < bytesToShow)
                        sb.AppendFormat("{0:X2} ", data[i + j]);
                    else
                        sb.Append("   ");

                    if (j == 7)
                        sb.Append(" ");
                }

                sb.Append(" ");

                for (int j = 0; j < 16; j++)
                {
                    if (i + j < bytesToShow)
                    {
                        byte b = data[i + j];
                        char c = (b >= 0x20 && b <= 0x7E) ? (char)b : '.';
                        sb.Append(c);
                    }
                }

                sb.AppendLine();
            }

            if (data.Length > HexPreviewMaxBytes)
            {
                sb.AppendLine();
                sb.AppendLine($"... ({data.Length - HexPreviewMaxBytes} more bytes)");
            }

            richTextBoxHexPreview.Text = sb.ToString();
        }

        private void DisplayTextPreview(byte[] data)
        {
            try
            {
                System.Text.Encoding encoding;

                // Dùng UDE để phát hiện mã hoá ký tự
                var detector = new Ude.CharsetDetector();
                detector.Feed(data, 0, data.Length);
                detector.DataEnd();

                if (detector.Charset == null)
                {
                    encoding = System.Text.Encoding.GetEncoding("GB18030");
                }
                else
                {
                    try
                    {
                        encoding = System.Text.Encoding.GetEncoding(detector.Charset);
                    }
                    catch
                    {
                        // Nếu charset phát hiện không được hỗ trợ, dùng GB18030
                        encoding = System.Text.Encoding.GetEncoding("GB18030");
                    }
                }

                // Hiển thị toàn bộ văn bản không giới hạn
                string text = encoding.GetString(data);
                richTextBoxTextPreview.Text = text;
            }
            catch (Exception ex)
            {
                richTextBoxTextPreview.Text = $"Error decoding text: {ex.Message}";
            }
        }

        private void DisplayImagePreview(byte[] data)
        {
            try
            {
                StopAnimation();
                
                if (FileTypeDetector.IsSpriteFile(data))
                {
                    DisplaySpritePreview(data);
                }
                else if (FileTypeDetector.IsCursorFile(data))
                {
                    DisplayCursorPreview(data);
                    DisableAnimationControls();
                }
                else
                {
                    // Logic inline từ DisplayStandardImagePreview: hiển thị ảnh chuẩn, chỉ scale-down nếu vượt khung
                    using (var ms = new MemoryStream(data))
                    {
                        var img = Image.FromStream(ms);
                        // Lưu ảnh gốc để có thể fit lại khi Resize
                        if (originalImageForPreview != null)
                        {
                            originalImageForPreview.Dispose();
                            originalImageForPreview = null;
                        }
                        originalImageForPreview = img is Bitmap b ? new Bitmap(b) : new Bitmap(img);
                        if (!(img is Bitmap)) img.Dispose();
                    }

                    ImagePreviewHelper.ClearPictureBox(pictureBox1);
                    var fitted = GetFittedBitmapOrOriginal(originalImageForPreview!);
                    pictureBox1.Image = fitted;
                    pictureBox1.SizeMode = PictureBoxSizeMode.CenterImage; // căn giữa, không phóng to
                    DisableAnimationControls();
                }
            }
            catch (Exception ex)
            {
                spriteHelper.CleanupSprite();
                ImagePreviewHelper.ClearPictureBox(pictureBox1);
                LogMessage($"Error loading image: {ex.Message}");
            }
        }

        private void DisplaySpritePreview(byte[] data)
        {
            if (!spriteHelper.LoadSprite(data))
            {
                spriteHelper.CleanupSprite();
                ImagePreviewHelper.ClearPictureBox(pictureBox1);
                LogMessage("Failed to load sprite file");
                return;
            }

            var sprite = spriteHelper.CurrentSprite;
            if (sprite == null)
                return;

            // Sprite dùng ảnh tự render, không dùng ảnh gốc chuẩn
            if (originalImageForPreview != null)
            {
                originalImageForPreview.Dispose();
                originalImageForPreview = null;
            }

            int frames = sprite.GetFrames();
            int interval = sprite.GetInterval();
            
            LogMessage($"SPR: {sprite.GetWidth()}x{sprite.GetHeight()}, " +
                     $"Frames: {frames}, Directions: {sprite.GetDirections()}, " +
                     $"Colors: {sprite.GetColors()}, Interval: {interval}ms");
            
            var (isAnimated, hasMultipleFrames, fps) = spriteHelper.AnalyzeSprite();
            ConfigureSpriteControls(hasMultipleFrames, isAnimated, fps);
        }

        private void ConfigureSpriteControls(bool hasMultipleFrames, bool isAnimated, double fps)
        {
            checkBox1.Enabled = hasMultipleFrames;
            numericUpDown1.Enabled = hasMultipleFrames;
            
            if (hasMultipleFrames)
            {
                numericUpDown1.Value = (decimal)Math.Round(fps);
                
                if (isAnimated)
                {
                    checkBox1.Checked = true;
                    StartAnimation();
                }
                else
                {
                    checkBox1.Checked = false;
                    DisplaySpriteSheet();
                }
            }
            else
            {
                checkBox1.Checked = false;
                checkBox1.Enabled = false;
                numericUpDown1.Enabled = false;
                DisplaySingleFrame(0);
            }
        }

        private void DisableAnimationControls()
        {
            checkBox1.Enabled = false;
            numericUpDown1.Enabled = false;
        }

        private void DisplaySingleFrame(int frameIndex)
        {
            Bitmap? bitmap = spriteHelper.RenderFrame(frameIndex);
            if (bitmap != null)
            {
                ImagePreviewHelper.ClearPictureBox(pictureBox1);
                // Sprite không lưu ảnh gốc trong trường originalImageForPreview
                Bitmap fitted = GetFittedBitmapOrOriginal(bitmap);
                if (!ReferenceEquals(fitted, bitmap))
                    bitmap.Dispose();
                pictureBox1.Image = fitted;
                pictureBox1.SizeMode = PictureBoxSizeMode.CenterImage; // căn giữa, không phóng to
            }
        }
        
        private void DisplayCursorPreview(byte[] data)
        {
            try
            {
                var cursorImage = ImagePreviewHelper.LoadCursorAsBitmap(data, out ushort hotspotX, out ushort hotspotY);
                if (cursorImage == null)
                {
                    LogMessage("Invalid cursor file: too small");
                    return;
                }

                var previewBitmap = ImagePreviewHelper.CreateCursorPreviewBitmap(cursorImage, hotspotX, hotspotY);
                cursorImage.Dispose();
                
                // Lưu ảnh gốc preview con trỏ để fit lại khi Resize
                if (originalImageForPreview != null)
                {
                    originalImageForPreview.Dispose();
                    originalImageForPreview = null;
                }
                originalImageForPreview = new Bitmap(previewBitmap);

                ImagePreviewHelper.ClearPictureBox(pictureBox1);
                var fitted = GetFittedBitmapOrOriginal(originalImageForPreview);
                pictureBox1.Image = fitted;
                pictureBox1.SizeMode = PictureBoxSizeMode.CenterImage; // căn giữa con trỏ
                LogMessage($"Cursor: {previewBitmap.Width}x{previewBitmap.Height}, Hotspot: ({hotspotX}, {hotspotY})");
            }
            catch (Exception ex)
            {
                LogMessage($"Error loading cursor: {ex.Message}");
                // Fallback: thử chuyển con trỏ sang icon và hiển thị (logic inline từ TryLoadCursorAsFallback)
                try
                {
                    byte[] iconData = ImagePreviewHelper.ConvertCursorToIcon(data);
                    using (var ms = new MemoryStream(iconData))
                    using (var icon = new Icon(ms))
                    {
                        if (originalImageForPreview != null)
                        {
                            originalImageForPreview.Dispose();
                            originalImageForPreview = null;
                        }
                        originalImageForPreview = icon.ToBitmap();

                        ImagePreviewHelper.ClearPictureBox(pictureBox1);
                        var fitted = GetFittedBitmapOrOriginal(originalImageForPreview);
                        pictureBox1.Image = fitted;
                        pictureBox1.SizeMode = PictureBoxSizeMode.CenterImage; // căn giữa icon
                        LogMessage("Cursor loaded as icon (fallback)");
                    }
                }
                catch (Exception ex2)
                {
                    LogMessage($"Fallback also failed: {ex2.Message}");
                }
            }
        }
        
        private void DisplaySpriteSheet()
        {
            Bitmap? sheet = spriteHelper.RenderSpriteSheet();
            if (sheet != null)
            {
                ImagePreviewHelper.ClearPictureBox(pictureBox1);
                // Sprite không lưu ảnh gốc trong trường originalImageForPreview
                Bitmap fitted = GetFittedBitmapOrOriginal(sheet);
                if (!ReferenceEquals(fitted, sheet))
                    sheet.Dispose();
                pictureBox1.Image = fitted;
                pictureBox1.SizeMode = PictureBoxSizeMode.CenterImage; // căn giữa, không phóng to
            }
        }

        // Scale-down-only helper: trả về ảnh đã fit nếu lớn hơn Panel2, ngược lại trả về ảnh gốc
        private Bitmap GetFittedBitmapOrOriginal(Bitmap source)
        {
            Size max = splitContainer1.Panel2.ClientSize;
            if (max.Width <= 0 || max.Height <= 0)
                return source;

            if (source.Width <= max.Width && source.Height <= max.Height)
                return source; // giữ nguyên kích thước thật

            double scale = Math.Min((double)max.Width / source.Width, (double)max.Height / source.Height);
            int w = Math.Max(1, (int)Math.Round(source.Width * scale));
            int h = Math.Max(1, (int)Math.Round(source.Height * scale));

			var result = new Bitmap(w, h);
			using (var g = Graphics.FromImage(result))
			{
				g.InterpolationMode = InterpolationMode.HighQualityBicubic;
				g.PixelOffsetMode = PixelOffsetMode.HighQuality;
				g.CompositingQuality = CompositingQuality.HighQuality;
				g.SmoothingMode = SmoothingMode.None;
				g.DrawImage(source, 0, 0, w, h);
			}
			return result;
        }

        // Làm mới scale ảnh đang hiển thị dựa trên trạng thái hiện tại
        private void RefreshCurrentPreviewScale()
        {
            if (tabControl1.SelectedTab != tabPage1)
                return;

            if (spriteHelper.CurrentSprite != null)
            {
                if (checkBox1.Checked)
                {
                    // Đang phát animation → hiển thị lại frame hiện tại
                    DisplaySingleFrame(spriteHelper.CurrentFrameIndex);
                }
                else
                {
                    // Đang hiển thị sprite sheet
                    DisplaySpriteSheet();
                }
                return;
            }

            if (originalImageForPreview != null)
            {
                ImagePreviewHelper.ClearPictureBox(pictureBox1);
                var fitted = GetFittedBitmapOrOriginal(originalImageForPreview);
                pictureBox1.Image = fitted;
                pictureBox1.SizeMode = PictureBoxSizeMode.CenterImage;
            }
        }
        
        private void StartAnimation()
        {
            if (spriteHelper.CurrentSprite == null)
                return;
            
            spriteHelper.ResetFrameIndex();
            
            double fps = (double)numericUpDown1.Value;
            int intervalMs = (int)Math.Round(1000.0 / fps);
            animationTimer.Interval = Math.Max(1, intervalMs);
            
            animationTimer.Start();
            DisplaySingleFrame(spriteHelper.CurrentFrameIndex);
        }
        
        private void StopAnimation()
        {
            animationTimer.Stop();
            spriteHelper.CleanupSprite();
        }
        #endregion

        #region Animation events
        private void AnimationTimer_Tick(object? sender, EventArgs e)
        {
            if (spriteHelper.CurrentSprite == null)
            {
                animationTimer.Stop();
                return;
            }
            
            spriteHelper.AdvanceFrame();
            DisplaySingleFrame(spriteHelper.CurrentFrameIndex);
        }
        
        private void CheckBox1_CheckedChanged(object? sender, EventArgs e)
        {
            if (spriteHelper.CurrentSprite == null)
                return;
            
            if (checkBox1.Checked)
            {
                StartAnimation();
            }
            else
            {
                animationTimer.Stop();
                DisplaySpriteSheet();
            }
        }
        
        private void NumericUpDown1_ValueChanged(object? sender, EventArgs e)
        {
            if (animationTimer.Enabled && spriteHelper.CurrentSprite != null)
            {
                double fps = (double)numericUpDown1.Value;
                int intervalMs = (int)Math.Round(1000.0 / fps);
                animationTimer.Interval = Math.Max(1, intervalMs);
            }
        }
        #endregion

        #region Path loading
        private void LoadFileList(string listPath)
        {
            idToPathMap.Clear();
            
            string[] lines = File.ReadAllLines(listPath);
            if (lines.Length == 0)
            {
                LogMessage("List file is empty");
                return;
            }
            
            // Kiểm tra xem có phải định dạng chi tiết không
            bool isDetailedFormat = false;
            
            // Kiểm tra header chi tiết: có dòng chứa "TotalFile:" hoặc header tab-separated "Index\tID\tTime\tFileName..."
            for (int i = 0; i < Math.Min(10, lines.Length); i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line))
                    continue;
                    
                // Kiểm tra có header metadata không (TotalFile:, PakTime:, etc.)
                if (line.Contains("TotalFile:") || line.Contains("PakTime:") || line.Contains("PakTimeSave:"))
                {
                    isDetailedFormat = true;
                }
                
                // Kiểm tra có header table không (Index\tID\tTime\tFileName...)
                if (line.Contains("\t") && (line.StartsWith("Index\t", StringComparison.OrdinalIgnoreCase) || 
                    (line.StartsWith("Index", StringComparison.OrdinalIgnoreCase) && line.Contains("ID") && line.Contains("FileName"))))
                {
                    isDetailedFormat = true;
                    break;
                }
            }
            
            int loadedCount = 0;
            int totalLines = 0;
            int headerLines = 0;
            
            if (isDetailedFormat)
            {
                LogMessage("Detected detailed list format (tab-separated)");
                
                // Bỏ qua các dòng header metadata (TotalFile:, PakTime:, etc.)
                int lineIndex = 0;
                while (lineIndex < lines.Length)
                {
                    string line = lines[lineIndex].Trim();
                    if (string.IsNullOrEmpty(line))
                    {
                        lineIndex++;
                        continue;
                    }
                    
                    // Nếu gặp header table "Index\tID\tTime\tFileName...", dòng tiếp theo là dữ liệu
                    if (line.Contains("\t") && (line.StartsWith("Index\t", StringComparison.OrdinalIgnoreCase) || 
                        (line.StartsWith("Index", StringComparison.OrdinalIgnoreCase) && line.Contains("ID") && line.Contains("FileName"))))
                    {
                        headerLines = lineIndex + 1;
                        lineIndex++;
                        break;
                    }
                    
                    // Nếu là header metadata, bỏ qua
                    if (line.Contains("TotalFile:") || line.Contains("PakTime:") || line.Contains("PakTimeSave:") || line.Contains("CRC:"))
                    {
                        headerLines++;
                        lineIndex++;
                        continue;
                    }
                    
                    // Nếu không phải header, có thể đã đến dữ liệu
                    if (line.Contains("\t") && !line.StartsWith("Index", StringComparison.OrdinalIgnoreCase))
                    {
                        headerLines = lineIndex;
                        break;
                    }
                    
                    lineIndex++;
                }
                
                // Parse các dòng dữ liệu
                for (int i = headerLines; i < lines.Length; i++)
                {
                    totalLines++;
                    string trimmedLine = lines[i].Trim();
                    if (string.IsNullOrEmpty(trimmedLine))
                        continue;
                    
                    // Split theo tab
                    string[] parts = trimmedLine.Split('\t');
                    if (parts.Length < 4) // Ít nhất cần Index, ID, Time, FileName
                        continue;
                    
                    // parts[0] = Index
                    // parts[1] = ID (hex string)
                    // parts[2] = Time
                    // parts[3] = FileName
                    
                    string idHexStr = parts[1].Trim();
                    string fileName = parts[3].Trim();
                    
                    // Convert hex ID to uint
                    if (!uint.TryParse(idHexStr, System.Globalization.NumberStyles.HexNumber, null, out uint id))
                    {
                        LogMessage($"Warning: Cannot parse ID '{idHexStr}' as hex at line {i + 1}");
                        continue;
                    }
                    
                    // Chuẩn hoá đường dẫn
                    string normalizedPath = fileName.Replace('/', '\\');
                    while (normalizedPath.Contains("\\\\"))
                        normalizedPath = normalizedPath.Replace("\\\\", "\\");
                    
                    // Thử cả 2 format: với và không có leading backslash
                    string pathWithoutSlash = normalizedPath.TrimStart('\\');
                    string pathWithSlash = normalizedPath.StartsWith("\\") 
                        ? normalizedPath 
                        : "\\" + normalizedPath;
                    
                    // Lưu mapping với ID từ file (không cần tính lại)
                    if (!idToPathMap.ContainsKey(id))
                    {
                        idToPathMap[id] = pathWithoutSlash;
                        loadedCount++;
                    }
                    
                    // Cũng thử tính ID từ path và lưu (để hỗ trợ cả 2 cách)
                    uint idFromPathWithout = KFilePath.FileName2Id(pathWithoutSlash);
                    if (idFromPathWithout != id && !idToPathMap.ContainsKey(idFromPathWithout))
                    {
                        idToPathMap[idFromPathWithout] = pathWithoutSlash;
                        loadedCount++;
                    }
                    
                    uint idFromPathWith = KFilePath.FileName2Id(pathWithSlash);
                    if (idFromPathWith != id && idFromPathWith != idFromPathWithout && !idToPathMap.ContainsKey(idFromPathWith))
                    {
                        idToPathMap[idFromPathWith] = pathWithSlash;
                        loadedCount++;
                    }
                }
                
                LogMessage($"Skipped {headerLines} header lines");
            }
            else
            {
                // Định dạng đơn giản: mỗi dòng là một đường dẫn
                LogMessage("Detected simple list format (path list)");
                
                foreach (string line in lines)
                {
                    totalLines++;
                    string trimmedLine = line.Trim();
                    if (string.IsNullOrEmpty(trimmedLine))
                        continue;
                    
                    // Chuẩn hoá đường dẫn: đổi '/' thành '\\', loại bỏ '\\\\' lặp (logic inline từ NormalizePath)
                    string normalizedPath = trimmedLine.Replace('/', '\\');
                    while (normalizedPath.Contains("\\\\"))
                        normalizedPath = normalizedPath.Replace("\\\\", "\\");
                    
                    // Thử cả 2 format: với và không có leading backslash
                    string pathWithoutSlash = normalizedPath.TrimStart('\\');
                    string pathWithSlash = normalizedPath.StartsWith("\\") 
                        ? normalizedPath 
                        : "\\" + normalizedPath;
                    
                    // Tính ID cho cả 2 format
                    uint idWithoutSlash = KFilePath.FileName2Id(pathWithoutSlash);
                    uint idWithSlash = KFilePath.FileName2Id(pathWithSlash);
                    
                    // Lưu cả 2 vào dictionary
                    if (!idToPathMap.ContainsKey(idWithoutSlash))
                    {
                        idToPathMap[idWithoutSlash] = pathWithoutSlash;
                        loadedCount++;
                    }
                    
                    if (idWithSlash != idWithoutSlash && !idToPathMap.ContainsKey(idWithSlash))
                    {
                        idToPathMap[idWithSlash] = pathWithSlash;
                        loadedCount++;
                    }
                }
            }
            
            LogMessage($"Read {totalLines} data lines from list file");
            
            LogMessage($"Created {loadedCount} ID mappings (including with/without leading slash variants)");
            LogMessage($"Unique paths in dictionary: {idToPathMap.Count}");
            
            // Debug: hiển thị 10 path đầu tiên và ID của chúng
            if (idToPathMap.Count > 0)
            {
                LogMessage("Sample loaded paths and their IDs:");
                int sampleCount = 0;
                foreach (var kvp in idToPathMap)
                {
                    LogMessage($"  - 0x{kvp.Key:X8} → {kvp.Value}");
                    sampleCount++;
                    if (sampleCount >= 10) break;
                }
            }
            
            // Đếm số file trong PAK có mapping
            if (fileIndexList.Count > 0)
            {
                int mappedFiles = 0;
                foreach (var indexInfo in fileIndexList)
                {
                    if (idToPathMap.ContainsKey(indexInfo.uId))
                        mappedFiles++;
                }
                
                LogMessage($"Matched {mappedFiles}/{fileIndexList.Count} files in PAK ({(double)mappedFiles / fileIndexList.Count * 100:F1}%)");
                
                if (mappedFiles < fileIndexList.Count)
                {
                    LogMessage($"Warning: {fileIndexList.Count - mappedFiles} files in PAK have no path mapping");
                }
                
                // Debug: hiển thị 5 ID đầu tiên không match
                if (mappedFiles < fileIndexList.Count)
                {
                    LogMessage("Sample unmapped IDs:");
                    int sampleCount = 0;
                    foreach (var indexInfo in fileIndexList)
                    {
                        if (!idToPathMap.ContainsKey(indexInfo.uId))
                        {
                            LogMessage($"  - ID: 0x{indexInfo.uId:X8}");
                            sampleCount++;
                            if (sampleCount >= 5) break;
                        }
                    }
                }
            }
        }
        #endregion

        #region Logging
        private void LogMessage(string message)
        {
            richTextBoxLogs.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
            richTextBoxLogs.ScrollToCaret();
        }
        #endregion
    }
}
