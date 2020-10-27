using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Terminator
{
    internal class Program
    {
        private static List<string> PrettyPleaseKill(List<Process> processes)
        {
            var errors = new List<string>();
            processes.ForEach(_ =>
            {
                try
                {
                    _.Kill();
                }
                catch (Exception ex)
                {
                    errors.Add($"{_.Id}\t{_.ProcessName}\t{ex.Message}");
                }
            });
            return errors;
        }

        private static List<string> JustKillAlready(List<Process> processes)
        {
            var job = new Job();
            job.AddProcess(processes.Select(_ => _.Handle));
            job.Dispose();
            return new List<string>();
        }


        private static void Main(string[] args)
        {
            var patternFileName = $"{Path.GetFileNameWithoutExtension(AppDomain.CurrentDomain.FriendlyName)}.txt";
            var pattern = string.Join("|", File.ReadAllLines(patternFileName));

            var stopWatchToDiscover = new Stopwatch();
            var selectedProcesses = Process.GetProcesses()
                .Where(_ => Regex.IsMatch(_.ProcessName, pattern, RegexOptions.IgnoreCase))
                .OrderBy(_ => _.ProcessName)
                .ToList();
            stopWatchToDiscover.Stop();

            var colorBefore = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(string.Join(Environment.NewLine, selectedProcesses.Select(_ => $"{_.Id}\t{_.ProcessName}\t{(DateTime.Now - _.StartTime).TotalSeconds}s")));
            Console.ForegroundColor = colorBefore;

            List<string> errors;
            var stopWatchToKill = new Stopwatch();
            if (args.Any() && args[0] == "PrettyPlease")
                errors = PrettyPleaseKill(selectedProcesses);
            else
                errors = JustKillAlready(selectedProcesses);
            stopWatchToKill.Stop();

            if(stopWatchToDiscover.Elapsed.Milliseconds != 0)
                Console.WriteLine($"Time to discover {stopWatchToDiscover.Elapsed.TotalMilliseconds}");

            if(stopWatchToKill.Elapsed.Milliseconds != 0)
                Console.WriteLine($"Time to kill {stopWatchToKill.Elapsed.TotalMilliseconds}");

            if (selectedProcesses.Any())
            {
                Console.WriteLine($"{selectedProcesses.Count} process detected");
                if (errors.Any())
                {
                    colorBefore = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(string.Join(Environment.NewLine, errors));
                    Console.ForegroundColor = colorBefore;
                }
            }

            var secondsToClose = args.Length >= 2 ? Convert.ToInt32(args[1]) : 1;
            if (secondsToClose <= 0) return;

            Console.WriteLine($"Press to quit or wait {secondsToClose} seconds");
            Task.Factory.StartNew(Console.ReadKey).Wait(TimeSpan.FromSeconds(secondsToClose));
        }
    }
}
