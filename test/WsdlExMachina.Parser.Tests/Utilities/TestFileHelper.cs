using System;
using System.IO;

namespace WsdlExMachina.Parser.Tests.Utilities
{
    public static class TestFileHelper
    {
        /// <summary>
        /// Gets the path to a sample file by dynamically finding the project root.
        /// </summary>
        /// <param name="sampleFileName">The name of the sample file in the samples directory.</param>
        /// <returns>The full path to the sample file.</returns>
        /// <exception cref="DirectoryNotFoundException">Thrown when the samples directory cannot be found.</exception>
        public static string GetSamplePath(string sampleFileName)
        {
            var directory = new DirectoryInfo(Directory.GetCurrentDirectory());

            // Navigate up until we find the project root (where samples directory exists)
            while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "samples")))
            {
                directory = directory.Parent;
            }

            if (directory == null)
                throw new DirectoryNotFoundException("Could not find samples directory in any parent directory. Make sure the samples directory exists.");

            return Path.Combine(directory.FullName, "samples", sampleFileName);
        }
    }
}
