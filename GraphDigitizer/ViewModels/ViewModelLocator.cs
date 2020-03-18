using GalaSoft.MvvmLight.Ioc;

namespace GraphDigitizer.ViewModels
{
    public class ViewModelLocator
    {
        public ViewModelLocator()
        {
            SimpleIoc.Default.Register<MainWindowViewModel>();
            SimpleIoc.Default.Register<AboutDialogViewModel>();
        }

        public MainWindowViewModel Main => SimpleIoc.Default.GetInstance<MainWindowViewModel>();

        public AboutDialogViewModel About => SimpleIoc.Default.GetInstance<AboutDialogViewModel>();
    }
}
