using System.Reflection;

namespace GraphDigitizer.ViewModels
{
    public class AboutDialogViewModel
    {
        public string Version => Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? string.Empty;
    }
}
