using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestUtilities
{
    public class TestInfo
    {
        public TestInfo(string name, TestStatus status, IReadOnlyList<string> categories, string comment="")
        {
            Comment = comment;
            Categories = categories;
            Status = status;
            Name = name;
        }

        public string Name { get; private set; }
        public TestStatus Status { get; private set; }
        public IReadOnlyList<string> Categories { get; private set; }
        public string Comment { get; private set; }

        public override string ToString()
        {
            return string.Format(
                "{0},{1},{2},{3}",
                Name,
                Status,
                string.Join(";", Categories),
                Comment);
        }

        public static bool TryParse(string line, out TestInfo info)
        {
            var cells = line.Split(',').Select(text => text.Trim()).ToArray();
            if (!cells.Any() || !cells[0].Any())
            {
                info = null;
                return false;
            }

            var name = cells[0];
            TestStatus status;
            if (cells.Length < 2 || !TestStatus.TryParse(cells[1], out status))
                status = TestStatus.Pass;
            List<string> categories = cells.Length >= 3
                ? cells[2].Split(';').Select(text => text.Trim()).ToList()
                : new List<string>();
            string comment = cells.Length >= 4 ? cells[3].Trim() : string.Empty;

            info = new TestInfo(name, status, categories, comment);
            return true;
        }

        public static IEnumerable<TestInfo> ParseTestsFromFile(string path)
        {
            var lines = File.ReadAllLines(path);
            foreach (var line in lines)
            {
                TestInfo info;
                if (TryParse(line, out info))
                    yield return info;
            }
        }

        public static void WriteTestsToFile(IEnumerable<TestInfo> tests, string path)
        {
            File.WriteAllLines(path, tests.Select(test => test.ToString()));
        }
    }

    public enum TestStatus
    {
        Pass, Fail, Inconclusive, Ignore
    }
}
