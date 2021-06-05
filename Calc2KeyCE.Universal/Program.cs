using System;
using System.Diagnostics;

namespace Calc2KeyCE.Universal
{
    class Program
    {
        // Linux
        // wget https://packages.microsoft.com/config/ubuntu/21.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
        // sudo dpkg -i packages-microsoft-prod.deb
        // sudo apt-get update
        // sudo apt-get dotnet-runtime-5.0
        static void Main(string[] args)
        {
            using (Process screenCap = new Process())
            {
                screenCap.StartInfo.RedirectStandardOutput = true;
                var process = Process.Start("./PyScreenCaptureLinux");
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.Start();

                Console.WriteLine(process.StandardOutput.ReadToEnd());
            }
        }
    }
}
