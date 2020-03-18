using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using GalaSoft.MvvmLight.Ioc;
using GraphDigitizer.Attributes;
using GraphDigitizer.Interfaces;

namespace GraphDigitizer.Services
{
    public class DialogService : IDialogService
    {
        public TViewModel ShowDialog<TViewModel>()
        {
            var (window, viewModel) = FindDialog<TViewModel>();
            window.ShowDialog();
            return viewModel;
        }

        public TViewModel ShowDialog<TViewModel, TData>(TData data) where TViewModel : ICanPassData<TData>
        {
            var (window, viewModel) = FindDialog<TViewModel>();
            viewModel.OnDataPassed(data);
            window.ShowDialog();
            return viewModel;
        }

        private (Window window, TViewModel viewModel) FindDialog<TViewModel>()
        {
            foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
            {
                var attribute = type.GetCustomAttribute<RegisterAttribute>();
                if (attribute == null || attribute.ViewModelType != typeof(TViewModel))
                {
                    continue;
                }
                
                var window = (Window) Activator.CreateInstance(type);

                var viewModel = SimpleIoc.Default.GetInstance<TViewModel>();
                window.DataContext = viewModel;

                return (window, viewModel);
            }

            throw new KeyNotFoundException($"There is no view registered for ViewModel {typeof(TViewModel).Name}");
        }
    }
}
