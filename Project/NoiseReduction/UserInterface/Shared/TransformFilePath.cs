using System.IO;

namespace UserInterface.Shared
{
    /// <summary>
    /// Helper class to work with file pathes
    /// </summary>
    public static class TransformFilePath
    {
        /// <summary>
        /// Method to get last part of full file path
        /// </summary>
        /// <param name="fullFilePath">Full file path</param>
        /// <param name="format">format of the file (.mp3 for example)</param>
        /// <returns></returns>
        public static string GetCanonicalName(string fullFilePath, string format)
        {
            return fullFilePath.Substring(fullFilePath.LastIndexOf('\\') + 1).Replace(format, "");
        }

        /// <summary>
        /// Method to get parent diractory of the project
        /// </summary>
        /// <returns>Full path of the parent directory of the project</returns>
        public static string GetParentDirectoryPath()
        {
            return Directory.GetParent(Directory.GetCurrentDirectory()).Parent.FullName;
        }

        /// <summary>
        /// Upading file name to match full file name
        /// </summary>
        /// <param name="fileName">not full file path (with 1 blank line)</param>
        /// <returns>full file path</returns>
        public static string UpdateFileName(string fileName)
        {
            return GetParentDirectoryPath() + "\\" + fileName + ".wav";
        }
    }
}
