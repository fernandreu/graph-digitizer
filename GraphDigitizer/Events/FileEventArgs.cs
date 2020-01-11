using System;
using GraphDigitizer.Models;

namespace GraphDigitizer.Events
{
    public class FileEventArgs : EventArgs
    {
        public string File { get; set; }

        public FileType Type { get; set; }
    }
}
