using System;
using System.Collections.Generic;
using NDesk.Options;

namespace webcrawler
{
	class MainClass
	{
		private static bool debug = true;
		private bool show_help = false;
		List<string> urls = new List<string> ();
		int depth = 0;
		bool cross_domain;
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

			// run the Crawler on all provided URLs
			foreach (string url in urls) {
				WebCrawler crawl = new WebCrawler (url, depth, cross_domain, debug);
				crawl.RunAsync (200); // run the crawler in async mode (with 20 threads)
			}
			Environment.Exit(0);
		}

		private void parseArguments(string[] args) {
			p = new OptionSet (){ 
				{ "c|crossdomain", "whether to crawl through other domains aswell",
					v => cross_domain = (v != null) ? true : false},
				{ "l|depth=", "the max depthlevel of folders to crawl\n" +
					"0 means no limit and is default.\n" +
					"this has to be an integer.",
					(int v) => depth = v },
				{ "d|debug", "enables debug mode",
					v => debug = true},
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
			Console.WriteLine ("Crawls the specified URLs according to the given options (see below).");
			Console.WriteLine ();
			Console.WriteLine ("Options:");
			p.WriteOptionDescriptions (Console.Out);
		}

	}
}
