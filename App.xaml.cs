using Microsoft.EntityFrameworkCore;
using NassefStore.Data;
using System.Windows;
using System.Windows.Threading;

namespace NassefStore;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // ── Global exception handlers — بدل ما يقفل بصمت ─────
        DispatcherUnhandledException += (s, ex) =>
        {
            MessageBox.Show(
                $"خطأ غير متوقع:\n{ex.Exception.Message}\n\n{ex.Exception.InnerException?.Message}",
                "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            ex.Handled = true; // امنع الإغلاق
        };

        AppDomain.CurrentDomain.UnhandledException += (s, ex) =>
        {
            var msg = (ex.ExceptionObject as Exception)?.Message ?? ex.ExceptionObject.ToString();
            MessageBox.Show($"خطأ حرج:\n{msg}", "خطأ حرج", MessageBoxButton.OK, MessageBoxImage.Error);
        };

        TaskScheduler.UnobservedTaskException += (s, ex) =>
        {
            ex.SetObserved();
        };

        // ── تهيئة قاعدة البيانات ──────────────────────────────
        try
        {
            using var db = new AppDbContext();
            await db.Database.EnsureCreatedAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في تهيئة قاعدة البيانات:\n{ex.Message}", "خطأ",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
