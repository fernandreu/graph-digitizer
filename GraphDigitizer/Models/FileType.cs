namespace GraphDigitizer.Models
{
    public enum FileType
    {
        Unknown,

        /// <summary>
        /// A standard image in binary format
        /// </summary>
        Image,

        /// <summary>
        /// A file previously saved with the tool
        /// </summary>
        Saved,
    }
}
