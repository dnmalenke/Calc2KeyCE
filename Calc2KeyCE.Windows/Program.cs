using System;
using System.Windows.Forms;
using Python.Runtime;

namespace Calc2KeyCE
{
    static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Runtime.PythonDLL = @".\python37.dll";
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Calc2KeyCE());
        }
    }
}
