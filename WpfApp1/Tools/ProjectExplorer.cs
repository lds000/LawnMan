using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace BackyardBoss.Tools
{

    public static class ProjectStructureToClipboard
    {
        private static readonly string[] AllowedExtensions = { ".cs", ".xaml", ".json" };
        private static readonly string[] ExcludedFolders = { "obj", "Properties" };

        public static void ExportStructureWithContents(string rootDirectory)
        {
            var sb = new StringBuilder();
            var rootDir = new DirectoryInfo(rootDirectory);

            sb.AppendLine($"📁 {rootDir.Name}");

            // Top-level files
            foreach (var file in rootDir.GetFiles())
            {
                if (IsAllowed(file))
                {
                    AppendFileInfo(sb, file, "  ");
                }
            }

            // First-level folders (excluding obj and Properties)
            foreach (var folder in rootDir.GetDirectories())
            {
                if (IsExcluded(folder.Name))
                    continue;

                sb.AppendLine($"  📁 {folder.Name}");

                foreach (var file in folder.GetFiles())
                {
                    if (IsAllowed(file))
                    {
                        AppendFileInfo(sb, file, "    ");
                    }
                }
            }

            Clipboard.SetText(sb.ToString());
            Console.WriteLine("Project structure copied to clipboard.");
        }

        private static bool IsAllowed(FileInfo file) =>
            Array.Exists(AllowedExtensions, ext => file.Extension.Equals(ext, StringComparison.OrdinalIgnoreCase));

        private static bool IsExcluded(string folderName) =>
            Array.Exists(ExcludedFolders, name => name.Equals(folderName, StringComparison.OrdinalIgnoreCase));

        private static void AppendFileInfo(StringBuilder sb, FileInfo file, string indent)
        {
            sb.AppendLine($"{indent}📄 {file.Name}");
            try
            {
                string content = File.ReadAllText(file.FullName);
                sb.AppendLine($"{indent}── FILE CONTENT ──");
                sb.AppendLine(IndentMultiline(content, indent + "  "));
                sb.AppendLine($"{indent}── END ──");
            }
            catch (Exception e)
            {
                sb.AppendLine($"{indent}[ERROR] Could not read file: {e.Message}");
            }
        }

        private static string IndentMultiline(string text, string indent)
        {
            var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            var indented = new StringBuilder();
            foreach (var line in lines)
            {
                indented.AppendLine($"{indent}{line}");
            }
            return indented.ToString();
        }
    }
}
