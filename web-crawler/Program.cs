using System;
using System.Collections.Generic;
using System.Net;
using NDesk.Options;

namespace webcrawler
{
	class MainClass
	{
		private static bool debug = false;
		private bool show_help = false;
		private int verbosity = 2;
		List<string> urls = new List<string> ();
		int depth = 0;
		string mirror_dir = "";
		bool mirror;
		OptionSet p;



		public static void Main (string[] args) {
			MainClass mainClass = new MainClass ();
			try {
				mainClass.Run(args);
			} catch(Exception e) {
				if (debug) {
					Console.Error.WriteLine ("web-crawler: {0}", e.ToString ());
				} else {
					Console.Error.WriteLine ("web-crawler: {0}", e.Message);
				}
				Console.WriteLine ("See `web-crawler help' for more information.");
				Environment.ExitCode = 1;
			}
		}

		private void Run(string[] args) {
			parseArguments (args);

			if (show_help || urls.Count <= 0) {
				showHelp();
				Environment.Exit(1);
			}

			foreach (string url in urls) {
				WebCrawler crawl = new WebCrawler (url, depth, debug, verbosity);
				crawl.Run ();
			}

		}

		private void parseArguments(string[] args) {
			p = new OptionSet (){ 
				{ "m|mirror=", "mirror the specified websites to {NAME}",
					v => mirror_dir = v },
				{ "l|depth=", "the max depthlevel of folders to crawl\n" +
					"0 means no limit and is default.\n" +
					"this has to be an integer.",
					(int v) => depth = v },
				{ "d|debug", "enables debug mode",
					v => debug = true},
				{ "v", "increase debug message verbosity",
					v => { if (v != null) ++verbosity; } },
				{ "h|help",  "show this message and exit", 
					v => show_help = v != null },
			};
			try {
				urls = p.Parse(args);
			} catch (OptionException e) {
				Console.Error.Write("web-crawler: ");
				if (debug) {
					Console.WriteLine(e.ToString());
				} else {
					Console.WriteLine(e.Message);
				}
				Console.WriteLine("Try web-crawler --help for more information");
			}
		}

		private void showHelp() {
			Console.WriteLine ("Usage: web-crawler [OPTIONS]... URLs...");
			Console.WriteLine ("Crawls the specified URLs according to the given options ( see below).");
			Console.WriteLine ("If --mirror is not specified, no files will be stored permanently.");
			Console.WriteLine ();
			Console.WriteLine ("Options:");
			p.WriteOptionDescriptions (Console.Out);
		}

	}
}
