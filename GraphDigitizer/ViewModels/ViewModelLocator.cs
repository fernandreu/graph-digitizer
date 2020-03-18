using GalaSoft.MvvmLight.Ioc;
using GraphDigitizer.Interfaces;
using GraphDigitizer.Services;

namespace GraphDigitizer.ViewModels
{
    public class ViewModelLocator
    {
        public ViewModelLocator()
        {
            SimpleIoc.Default.Register<MainWindowViewModel>();
            SimpleIoc.Default.Register<AboutDialogViewModel>();
            SimpleIoc.Default.Register<AxesPropViewModel>();
            SimpleIoc.Default.Register<IDialogService, DialogService>();
        }

        public MainWindowViewModel Main => SimpleIoc.Default.GetInstance<MainWindowViewModel>();
    }
}
