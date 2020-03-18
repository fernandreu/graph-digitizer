namespace GraphDigitizer.Interfaces
{
    public interface ICanPassData<in T>
    {
        void OnDataPassed(T data);
    }
}
