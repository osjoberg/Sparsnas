using System.Linq;
using System.Text.RegularExpressions;
using static System.Console;

namespace Sparsnas
{
    internal static class Program
    {
        private static readonly Regex ParseTune = new Regex("^(--|-|/)tune$");
        private static readonly Regex ParseF1 = new Regex("(--|-|/)f1 ([-]{0,1}[0-9]{1,6})");
        private static readonly Regex ParseSensor = new Regex("(--|-|/)s ([0-9]{6})/([0-9]{1,7})");

        public static void Main(string[] commandLine)
        {
            if (commandLine.Length == 0)
            {
                PrintUsage(null);
                return;
            }

            var parseCommandLine = string.Join(" ", commandLine);
            if (ParseTune.IsMatch(parseCommandLine))
            {
                new Tuner().Start();
                return;
            }

            var f1Matches = ParseF1.Matches(parseCommandLine);
            if (f1Matches.Count != 1)
            {
                PrintUsage("Unable to parse -f1 command line switch.");
                return;
            }

            var f1 = int.Parse(f1Matches.First().Groups[2].Value);
            parseCommandLine = ParseF1.Replace(parseCommandLine, "");

            var sensorMatches = ParseSensor.Matches(parseCommandLine).ToArray();
            if (sensorMatches.Length == 0)
            {
                PrintUsage("Unable to parse -s command line switch.");
                return;
            }

            parseCommandLine = ParseSensor.Replace(parseCommandLine, "");
            if (string.IsNullOrWhiteSpace(parseCommandLine) == false)
            {
                PrintUsage($"Unable to parse \"{parseCommandLine}\"");
                return;
            }

            var output = new Monitor(f1);

            foreach (var sensorMatch in sensorMatches)
            {
                var sensorId = int.Parse(sensorMatch.Groups[2].Value);
                var pulsesPerKwh = int.Parse(sensorMatch.Groups[3].Value);

                if (pulsesPerKwh == 0)
                {
                    PrintUsage("Unable to parse -s command line switch.");
                    return;
                }

                output.AddSensor(sensorId, pulsesPerKwh);
            }

            output.Start();
        }

        private static void PrintUsage(string errorMessage)
        {
            WriteLine("Get current and total power usage from Sparsnas sensors.");
            WriteLine();
            WriteLine("SPARSNAS /tune");
            WriteLine("SPARSNAS /f1 <frequency> /s <sensor id>/<pulses per kWh>");
            WriteLine();
            WriteLine("/tune    Tune to find best f1 frequency.");
            WriteLine("/f1      Use f1 frequency <frequency>.");
            WriteLine("/s       Use senor <sensor id> with <pulses per kwh> number of pulses per kWh.");
            WriteLine("         Six digit sensor is located in the battery compartment (400xxxxxx).");
            WriteLine("         Multiple sensors can be specified by using multiple -s switches.");

            if (errorMessage != null)
            {
                WriteLine();
                WriteLine("ERROR, " + errorMessage);
            }
        }
    }
}