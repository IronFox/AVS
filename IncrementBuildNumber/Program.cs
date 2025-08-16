namespace IncrementBuildNumber
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var path = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            while (!string.IsNullOrEmpty(path) && !Directory.Exists(Path.Combine(path, "AVS")))
            {
                path = Path.GetDirectoryName(path);
            }
            if (string.IsNullOrEmpty(path) || !Directory.Exists(Path.Combine(path, "AVS")))
            {
                Console.Error.WriteLine("AVS directory not found from " + Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location));
                return;
            }
            var filePath = Path.Combine(path, "AVS", "Properties", "AssemblyInfo.cs");
            if (!File.Exists(filePath))
            {
                Console.Error.WriteLine("AssemblyInfo.cs not found.");
                return;
            }
            var lines = File.ReadAllLines(filePath);
            foreach (var line in lines)
            {
                Console.WriteLine(line);
                if (line.StartsWith("[assembly: AssemblyVersion(") || line.StartsWith("[assembly: AssemblyFileVersion("))
                {
                    var versionLine = line.Split('"')[1];
                    var version = Version.Parse(versionLine); // Validate version format
                    version = new Version(version.Major, version.Minor, version.Build + 1, 0); // Reset build number to 0
                    var newLine = line.Replace(versionLine, version.ToString());
                    Console.WriteLine($"Updating version line: {newLine}");
                    lines[Array.IndexOf(lines, line)] = newLine; // Update the line in the array
                }
            }
            File.WriteAllLines(filePath, lines); // Write the updated lines back to the file
            Console.WriteLine("All Done");
        }
    }
}
