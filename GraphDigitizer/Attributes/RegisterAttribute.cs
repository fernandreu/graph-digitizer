using System;

namespace GraphDigitizer.Attributes
{
    public class RegisterAttribute : Attribute
    {
        public RegisterAttribute(Type viewModelType)
        {
            ViewModelType = viewModelType;
        }

        public Type ViewModelType { get; }
    }
}
