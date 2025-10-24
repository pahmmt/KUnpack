namespace KUnpack
{
    internal static class Program
    {
        /// <summary>
        ///  Điểm vào chính của ứng dụng.
        /// </summary>
        [STAThread]
        static void Main()
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            // Tùy chỉnh cấu hình ứng dụng như thiết lập DPI cao hoặc font mặc định,
            // xem tại https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
    }
}