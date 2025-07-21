using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AndroidDeviceVerificationTool;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();
        
        // 🎭 스플래시 스크린 먼저 표시
        ShowSplashScreenAndMainForm();
    }

    private static void ShowSplashScreenAndMainForm()
    {
        //// 스플래시 스크린 생성 및 표시
        //using (var splashScreen = new SplashScreen())
        //{
        //    // 스플래시 스크린을 모달로 표시 (자동으로 닫힐 때까지 대기)
        //    splashScreen.ShowDialog();
        //}

        // 스플래시 스크린이 닫힌 후 메인 폼 실행
        Application.Run(new Form1());
    }
}