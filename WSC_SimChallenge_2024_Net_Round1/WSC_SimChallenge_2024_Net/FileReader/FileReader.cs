using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace WSC_SimChallenge_2024_Net
{
    internal class FileReader
    {
        public static Dictionary<string, Dictionary<string, int>> ReadContainersInfo(string fileName)
        {
            // Get the directory where the current executable is running
            string directory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string filePath = Path.Combine(directory, fileName);

            // Read all lines from the specified file path
            string[] lines = File.ReadAllLines(filePath);
            Dictionary<string, Dictionary<string, int>> matrix = new Dictionary<string, Dictionary<string, int>>();
            string[] headers = lines[0].Split(',');

            for (int i = 1; i < lines.Length; i++)
            {
                string[] parts = lines[i].Split(',');
                string rowLabel = parts[0];
                Dictionary<string, int> rowDict = new Dictionary<string, int>();

                for (int j = 1; j < parts.Length; j++)
                {
                    rowDict[headers[j]] = int.Parse(parts[j]);
                }

                matrix[rowLabel] = rowDict;
            }
            return matrix;
        }

        public static Dictionary<string, DateTime> ReadVesselArrivalTimes(string filePath)
        {
            var arrivalTimes = new Dictionary<string, DateTime>();
            using (var reader = new StreamReader(filePath))
            {
                //reader.ReadLine(); // Skip header
                while (!reader.EndOfStream)
                {
                    var line = reader.ReadLine();
                    var values = line.Split(',');
                    //Console.WriteLine($"{values[0]}, {values[1]}");
                    var vessel = values[0];
                    var time = DateTime.ParseExact(values[1], "yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture);
                    arrivalTimes[vessel] = time;
                }
            }
            return arrivalTimes;
        }

        public static Dictionary<string, Tuple<double, double>> ReadControlpointsInfo(string fileName)
        {
            // Get the directory of the current executable
            string directory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string filePath = Path.Combine(directory, fileName);

            // Read all lines from the specified file path
            string[] lines = File.ReadAllLines(filePath);
            var matrix = new Dictionary<string, Tuple<double, double>>();

            for (int i = 0; i < lines.Length; i++)
            {
                string[] parts = lines[i].Split(',');
                if (parts.Length == 2) // Ensure there are exactly two parts to parse
                {
                    string rowLabel = i.ToString();
                    double item1 = double.Parse(parts[0]);
                    double item2 = double.Parse(parts[1]);
                    matrix[rowLabel] = Tuple.Create(item1, item2);
                }
            }
            return matrix;
        }
    }
}
