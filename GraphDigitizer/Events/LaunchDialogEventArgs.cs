using System;

namespace GraphDigitizer.Events
{
    public class LaunchDialogEventArgs
    {
        public Type DialogType { get; }

        public LaunchDialogEventArgs(Type dialogType)
        {
            DialogType = dialogType;
        }
    }
}
