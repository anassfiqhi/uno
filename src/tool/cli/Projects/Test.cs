﻿using Mono.Options;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using Uno.Build;
using Uno.TestRunner;

namespace Uno.CLI.Projects
{
    class Test : Command
    {
        public override string Name => "test";
        public override string Description => "Run test project(s).";

        public override void Help()
        {
            WriteUsage("[target] [options] [paths-to-search]");

            Log.Skip();
            WriteLine("[paths-to-search] is a list of paths to unoprojs to run tests from, and/or");
            WriteLine("directories in which to search for test projects.");
            WriteLine("When a directory is given, uno test searches recursively in that directory");
            WriteLine("for projects named '*Test.unoproj'");
            
            WriteHead("Examples");
            WriteLine(@"  uno test");
            WriteLine(@"  uno test path/projects");
            WriteLine(@"  uno test path/projects/FooTest.unoproj path/projects/BarTest.unoproj");
            WriteLine(@"  uno test path/projects path/other-projects/FooTest.unoproj");
            WriteLine(@"  uno test native -v path/projects");

            WriteHead("Available options", 26);
            WriteRow("-l, --logfile=PATH",          "Write output to this file instead of stdout");
            WriteRow("-t, --target=STRING",         "Build target. Supported: android, dotnet and native");
            WriteRow("-v, --verbose",               "Verbose, always prints output from compiler and debug_log");
            WriteRow("-q, --quiet",                 "Quiet, only prints output from compiler and debug_log in case of errors.");
            WriteRow("-f, --filter=",               "Only run tests matching this string");
            WriteRow("-e, --regex-filter=STRING",   "Only run tests matching this regular expression");
            WriteRow("    --trace",                 "Print trace information from unotest");
            WriteRow("    --build-only",            "Don't run compiled program.");
            WriteRow("    --no-uninstall",          "Don't uninstall tests after running on device");
            WriteRow("-D, --define=STRING",         "Add define, to enable a feature");
            WriteRow("-U, --undefine=STRING",       "Remove define, to disable a feature");
            WriteRow("    --out-dir=PATH",          "Override output directory");

            WriteHead("Available build targets", 19);

            foreach (var c in BuildTargets.Enumerate(false))
                WriteRow("* " + c.Identifier.ToLowerInvariant(), c.Description);
        }

        public override void Execute(IEnumerable<string> args)
        {
            try
            {
                var options = Parse(args);
                if (options != null)
                    UnoTest.DiscoverAndRun(options);
            }
            catch (HttpListenerException e)
            {
                Log.Error("Failed to open network socket for test communication: " + e.Message);
                Log.WriteErrorLine(
                    "If you're trying to run your tests on an external device, remember to run UnoTest as Administrator.");
            }
            catch (ArgumentException e)
            {
                Log.Error("Invalid argument: " + e.Message);
            }
            catch (AggregateException)
            {
                Log.Error("Internal error(s) occured.");
            }
            catch (Exception e)
            {
                Log.Error("Exited with exception: " + e.Message);
            }
        }

        TestOptions Parse(IEnumerable<string> args)
        {
            var verbose = Log.IsVerbose;
            var quiet = false;

            var options = new TestOptions();

            string targetName = null;
            var p = new OptionSet
            {
                { "r|reporter=",            v => Log.Warning("--reporter is deprecated and has no effect.") },
                { "l|logfile=",             v => options.LogFileName = v },
                { "t|target=",              v => targetName = v },
                { "v|verbose",              v => verbose = v != null },
                { "q|quiet",                v => quiet = v != null },
                { "f|filter=",              v => options.Filter = Regex.Escape(v) },
                { "e|regex-filter=",        v => options.Filter = v },
                { "o|timeout=",             v => Log.Warning("--timeout is deprecated and has no effect.") },
                { "startup-timeout=",       v => Log.Warning("--startup-timeout is deprecated and has no effect.") },
                { "trace",                  v => { options.Trace = v != null; } },
                { "build-only|only-build",  v => options.OnlyBuild = v != null },
                { "allow-debugger",         v => Log.Warning("--allow-debugger is deprecated and has no effect.") },
                { "d|debug",                v => Log.Warning("--debug is deprecated and has no effect.") },
                { "run-local",              v => Log.Warning("--run-local is deprecated and has no effect.") },
                { "no-uninstall",           v => options.NoUninstall = v != null },
                { "D=|define=",             options.Defines.Add },
                { "U=|undefine=",           options.Undefines.Add },
                { "out-dir=|output-dir=",   v => options.OutputDirectory = v },
                { "libs",                   v => options.Library = true },
            };

            try
            {
                var extra = p.Parse(args);
                options.Target = BuildTargets.Get(targetName, extra);
                options.Paths = extra;
                options.Verbose = verbose;

                if (verbose && quiet)
                    throw new ArgumentException("Cannot specify both -q and -v");
            }
            catch (OptionException e)
            {
                Log.WriteErrorLine(e.ToString());
                Help();
                return null;
            }

            return options;
        }
    }
}