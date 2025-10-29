namespace KUnpack.Helpers
{
    /// <summary>
    /// Lớp hỗ trợ cho các thao tác giao diện (UI)
    /// </summary>
    public static class UIHelper
    {
        public static void SetTableLayoutColumnWidth(TableLayoutPanel tableLayout, int columnIndex, float percentage, int maxWidth = 600)
        {
            if (tableLayout == null || columnIndex < 0 || columnIndex >= tableLayout.ColumnCount)
                return;

            // Tính toán độ rộng thực theo phần trăm
            int totalWidth = tableLayout.Width;
            int calculatedWidth = (int)(totalWidth * percentage / 100.0f);

            // Áp dụng giới hạn độ rộng tối đa
            int finalWidth = Math.Min(calculatedWidth, maxWidth);

            // Tính lại phần trăm thực cho độ rộng đã bị giới hạn
            float actualPercentage = (float)finalWidth / totalWidth * 100.0f;

            // Cập nhật kiểu cột
            tableLayout.ColumnStyles[columnIndex] = new ColumnStyle(SizeType.Percent, actualPercentage);

            // Điều chỉnh các cột còn lại theo tỷ lệ
            float remainingPercentage = 100.0f - actualPercentage;
            for (int i = 0; i < tableLayout.ColumnCount; i++)
            {
                if (i != columnIndex)
                {
                    // Phân bổ phần trăm còn lại cho các cột khác
                    float otherPercentage = remainingPercentage / (tableLayout.ColumnCount - 1);
                    tableLayout.ColumnStyles[i] = new ColumnStyle(SizeType.Percent, otherPercentage);
                }
            }
        }

        public static void AdjustListViewColumns(ListView listView)
        {
            if (listView.View != View.Details || listView.Columns.Count == 0)
                return;

            // Lấy độ rộng khả dụng (bao gồm cả thanh cuộn)
            int totalWidth = listView.ClientSize.Width; // Dùng toàn bộ vùng client, bao gồm thanh cuộn

            using (Graphics g = listView.CreateGraphics())
            {
                // Lưu độ rộng tối thiểu cho header và nội dung
                int[] headerWidths = new int[listView.Columns.Count];
                int[] contentWidths = new int[listView.Columns.Count];
                int totalContentWidth = 0;

                // Tính độ rộng tối thiểu cần cho tiêu đề (header)
                for (int i = 0; i < listView.Columns.Count; i++)
                {
                    headerWidths[i] = (int)g.MeasureString(listView.Columns[i].Text, listView.Font).Width + 20; // Add padding
                }

                // Đo độ rộng nội dung cho từng cột
                if (listView.VirtualMode)
                {
                    // Ở chế độ virtual, truy cập item theo index
                    int itemsToMeasure = Math.Min(listView.VirtualListSize, 1000); // Giới hạn để đảm bảo hiệu năng
                    for (int itemIndex = 0; itemIndex < itemsToMeasure; itemIndex++)
                    {
                        for (int colIndex = 0; colIndex < listView.Columns.Count && colIndex < listView.Items[itemIndex].SubItems.Count; colIndex++)
                        {
                            string text = listView.Items[itemIndex].SubItems[colIndex].Text;
                            int textWidth = (int)g.MeasureString(text, listView.Font).Width + 20;
                            contentWidths[colIndex] = Math.Max(contentWidths[colIndex], textWidth);
                        }
                    }
                }
                else
                {
                    // Chế độ non-virtual
                    for (int itemIndex = 0; itemIndex < listView.Items.Count; itemIndex++)
                    {
                        for (int colIndex = 0; colIndex < listView.Columns.Count && colIndex < listView.Items[itemIndex].SubItems.Count; colIndex++)
                        {
                            string text = listView.Items[itemIndex].SubItems[colIndex].Text;
                            int textWidth = (int)g.MeasureString(text, listView.Font).Width + 20;
                            contentWidths[colIndex] = Math.Max(contentWidths[colIndex], textWidth);
                        }
                    }
                }

                // Kết hợp độ rộng header và nội dung
                int[] columnWidths = new int[listView.Columns.Count];
                for (int i = 0; i < listView.Columns.Count; i++)
                {
                    columnWidths[i] = Math.Max(headerWidths[i], contentWidths[i]);
                    totalContentWidth += columnWidths[i];
                }

                // Đảm bảo độ rộng tối thiểu cho cột
                const int MIN_COLUMN_WIDTH = 50;
                for (int i = 0; i < listView.Columns.Count; i++)
                {
                    columnWidths[i] = Math.Max(columnWidths[i], MIN_COLUMN_WIDTH);
                    if (columnWidths[i] > MIN_COLUMN_WIDTH)
                        totalContentWidth += columnWidths[i] - contentWidths[i]; // Điều chỉnh tổng nếu áp dụng min width
                }

                // Điều chỉnh độ rộng cột dựa trên không gian khả dụng
                if (totalContentWidth < totalWidth)
                {
                    // Tăng tỷ lệ các cột để lấp đầy ListView
                    float scaleFactor = (float)totalWidth / totalContentWidth;
                    for (int i = 0; i < listView.Columns.Count; i++)
                    {
                        listView.Columns[i].Width = (int)(columnWidths[i] * scaleFactor);
                    }
                }
                else
                {
                    // Dùng độ rộng đã tính và cho phép cuộn
                    for (int i = 0; i < listView.Columns.Count; i++)
                    {
                        listView.Columns[i].Width = columnWidths[i];
                    }
                }

                // Bật cuộn (cả ngang lẫn dọc)
                listView.Scrollable = true;
            }
        }

        public static void SetExtractionUI(bool extracting, bool isSelected,
            ToolStripMenuItem openMenuItem, ToolStripMenuItem setOutputMenuItem,
            ToolStripMenuItem loadListMenuItem, ToolStripMenuItem extractSelectedMenuItem,
            ToolStripMenuItem extractAllMenuItem, ToolStripMenuItem pauseResumeMenuItem,
            ToolStripMenuItem cancelExtractionMenuItem, ToolStripProgressBar progressBar)
        {
            if (extracting)
            {
                // Vô hiệu hoá thao tác với tệp
                openMenuItem.Enabled = false;
                setOutputMenuItem.Enabled = false;
                loadListMenuItem.Enabled = false;

                // Tắt cả hai nút trích xuất khi đang trích xuất
                extractSelectedMenuItem.Enabled = false;
                extractAllMenuItem.Enabled = false;

                // Bật nút Tạm dừng và Huỷ
                pauseResumeMenuItem.Enabled = true;
                pauseResumeMenuItem.Text = "Pause";
                cancelExtractionMenuItem.Enabled = true;

                // Hiển thị thanh tiến trình
                progressBar.Visible = true;
            }
            else
            {
                // Đặt lại thanh tiến trình
                progressBar.Visible = false;
                progressBar.Value = 0;

                // Bật lại các menu
                openMenuItem.Enabled = true;
                setOutputMenuItem.Enabled = true;
                loadListMenuItem.Enabled = true;

                // Tắt nút Tạm dừng và Huỷ
                pauseResumeMenuItem.Enabled = false;
                pauseResumeMenuItem.Text = "Pause";
                cancelExtractionMenuItem.Enabled = false;
            }
        }

        public static void UpdateMenuState(bool hasPack, int selectedCount, int totalFiles,
            bool isExtracting, ToolStripMenuItem extractAllMenuItem, ToolStripMenuItem extractSelectedMenuItem)
        {
            // Không cập nhật khi đang trích xuất
            if (isExtracting)
                return;

            // Menu Extract All
            extractAllMenuItem.Enabled = hasPack && totalFiles > 0;
            extractAllMenuItem.Text = "Giải nén tất cả";

            // Menu Extract Selected
            extractSelectedMenuItem.Enabled = hasPack && selectedCount > 0;

            // Cập nhật text dựa trên số lượng mục đã chọn
            if (selectedCount > 0)
            {
                extractSelectedMenuItem.Text = $"Giải nén {selectedCount} tệp đã chọn";
            }
            else
            {
                extractSelectedMenuItem.Text = "Giải nén tệp đã chọn";
            }
        }
    }
}