using CommonServiceLocator;
using GalaSoft.MvvmLight.Ioc;

namespace GraphDigitizer.ViewModels
{
    public class ViewModelLocator
    {
        public ViewModelLocator()
        {
            ServiceLocator.SetLocatorProvider(() => SimpleIoc.Default);


            SimpleIoc.Default.Register<MainWindowViewModel>();
        }

        public MainWindowViewModel Main => SimpleIoc.Default.GetInstance<MainWindowViewModel>();
    }
}
