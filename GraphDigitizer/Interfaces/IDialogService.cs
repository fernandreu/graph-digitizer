namespace GraphDigitizer.Interfaces
{
    public interface IDialogService
    {
        TViewModel ShowDialog<TViewModel>();

        TViewModel ShowDialog<TViewModel, TData>(TData data) where TViewModel : ICanPassData<TData>;
    }
}
