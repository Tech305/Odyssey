using System.Windows;
using WindowsApplication = System.Windows.Application;

namespace Odyssey.Daedalus.View.Controls.Interop
{
    internal static class Helpers
    {
        // TODO: Remove Helpers class, refactor
        internal static Window GetDefaultOwnerWindow()
        {
            Window defaultWindow = null;

            // TODO: Detect active window and change to that instead
            if (WindowsApplication.Current != null && WindowsApplication.Current.MainWindow != null)
            {
                defaultWindow = WindowsApplication.Current.MainWindow;
            }
            return defaultWindow;
        }

    }
}
