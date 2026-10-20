using System;
using System.Threading;
using System.Windows.Forms;

namespace NaturalClicker
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            bool createdNew;
            using (Mutex mutex = new Mutex(true, "NaturalClicker_SingleInstance_9E6A", out createdNew))
            {
                if (!createdNew)
                {
                    MessageBox.Show("自然连点器已经在运行了。\r\n请从任务栏或系统托盘打开已有窗口。",
                        "自然连点器", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                Native.SetProcessDPIAware();
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                Application.ThreadException += delegate(object sender, ThreadExceptionEventArgs e)
                {
                    MessageBox.Show("程序遇到未处理的错误：\r\n" + e.Exception.Message,
                        "自然连点器", MessageBoxButtons.OK, MessageBoxIcon.Error);
                };

                Application.Run(new MainForm());
                GC.KeepAlive(mutex);
            }
        }
    }
}
