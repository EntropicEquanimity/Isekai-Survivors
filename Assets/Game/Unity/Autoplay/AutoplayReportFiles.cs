using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace Isekai
{
    /// <summary>
    /// Where autoplay reports go and how they are written: one JSON file per Run, one CSV row per Run appended to
    /// <c>autoplay.csv</c>, and a summary JSON per batch. In the Editor they are under <c>Logs/Autoplay</c> in the project;
    /// in a build under <see cref="Application.persistentDataPath"/>.
    /// </summary>
    public static class AutoplayReportFiles
    {
        public const string CsvName = "autoplay.csv";

        /// <summary>The folder reports go to unless the batch names another.</summary>
        public static string DefaultDirectory
        {
            get
            {
                if (Application.isEditor)
                {
                    string project = Directory.GetParent(Application.dataPath)?.FullName ?? Application.persistentDataPath;
                    return Path.Combine(project, "Logs", "Autoplay");
                }
                return Path.Combine(Application.persistentDataPath, "Logs", "Autoplay");
            }
        }

        /// <summary>Writes the Run's JSON and appends its CSV row; returns the JSON file's path.</summary>
        public static string WriteRun(string directory, string batchId, RunReport report)
        {
            Directory.CreateDirectory(directory);
            string json = Path.Combine(directory, $"run-{batchId}-{report.Index:000}-seed{report.Seed}.json");
            File.WriteAllText(json, report.ToJson(), new UTF8Encoding(false));
            string csv = Path.Combine(directory, CsvName);
            var text = new StringBuilder();
            if (!File.Exists(csv)) text.AppendLine(RunReport.CsvHeader);
            text.AppendLine(report.ToCsvRow());
            File.AppendAllText(csv, text.ToString(), new UTF8Encoding(false));
            return json;
        }

        /// <summary>Writes the batch summary; returns its path.</summary>
        public static string WriteSummary(string directory, string batchId, BatchSummary summary)
        {
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, $"summary-{batchId}.json");
            File.WriteAllText(path, summary.ToJson(), new UTF8Encoding(false));
            return path;
        }

        /// <summary>A name for a batch that sorts by time.</summary>
        public static string NewBatchId() => DateTime.Now.ToString("yyyyMMdd-HHmmss");
    }
}
