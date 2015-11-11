using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Linq;
using HtmlAgilityPack;
using System.Net;
using System.IO;

namespace webcrawler
{
	public class WebCrawler
	{
		Uri uri;
		int depth;
		bool cross_domain;
		bool debug;
		bool quiet;

		/// <summary>
		/// Public constructor
		/// <param name="url">the URL to start crawling on</param>
		/// <param name="depth">depth to crawl, 0 means no limit (optional, defaults to 0)</param>
		/// <param name="cross_domain">allow to follow links to domains not matching the initial host? (optional, defaults to false)</param>
		/// <param name="debug">debug output (optional, defaults to false)</param>
		/// <param name="quiet">disables all console standard output (optional, defaults to false)</param>
		/// </summary>
		public WebCrawler (string url, int depth = 0, bool cross_domain = false, bool debug = false, bool quiet = false)
		{
			try {
				this.uri = new Uri(url);
			} catch (Exception e) {
				if (!url.StartsWith ("http://") && !url.StartsWith ("https://")) {
					try {
						this.uri = new Uri("http://" + url);
					} catch (Exception e2) {
						Console.Error.WriteLine("Doesn't even work with http://"+url);
						Console.Error.WriteLine(debug ? e2.ToString() : e2.Message);
					}
				}
			}
			this.depth = depth;
			this.cross_domain = cross_domain;
			this.debug = debug;
			this.quiet = quiet;
		}

		/// <summary>
		/// Runs a crawler on uri
		/// </summary>
		///	<returns>List of all crawled files</returns>
		public List<string> Run() {

			Browser browser = new Browser ("WebCrawler");
			List<string> list_files = new List<string> ();
			Stack<string> marked_files = new Stack<string>();
			string current = uri.ToString();
			string content;
			int counter = 0;

			// initialize the lists
			marked_files.Push(current);

			do {
				current = marked_files.Pop(); // get the first item
				list_files.Add(current);
				if (!quiet) {
					Console.WriteLine(current);
				}
					
				int status = browser.get (current);
				if (status == 0) {
					if (!quiet) {
						Console.WriteLine("Error GETing: {0}", current);
						continue;
					}
				}
				if (debug) {
					Console.WriteLine ("Status: {0}", status);
					Console.WriteLine("Length: {0}", browser.getContent().Count());
				}
				if (status >= 200 && status < 300) { // OK
					content = browser.getContent();
					List<string> urls = parseForUrls(content);
					foreach(string url in urls) {
						// check whether the url is already listed
						if (!list_files.Contains(url) && !marked_files.Contains(url)) {
							marked_files.Push(url);
							if(debug) {
								Console.WriteLine("pushed {0} to marked_files", url);
							}
						}
					}
				}else if (status == 302 || status == 301) {
					string redirect = browser.getRedirect();
					if (debug) {
						Console.WriteLine("redirection to {0}", redirect);
					}
					marked_files.Push(redirect);
					
				}
				if (debug) {
					Console.WriteLine("Counter:  {0} ({1} remaining)", counter, marked_files.Count);
				}
				counter ++;

			} while(marked_files.Count > 0);

			if (!quiet) {
				Console.WriteLine ("crawled through {0} items.", list_files.Count);
			}
			return list_files;
		}


		/// <summary>
		/// Runs a crawler on uri asynchronously
		/// </summary>
		/// <param name="max_count">Maximum number of used threads (optional, defaults to 20)</param>
		///	<returns>List of all crawled files</returns> 
		public List<string> RunAsync(int max_count = 20) { 
			if (debug) {
				Console.WriteLine ("Starting RunAsync() on '{0}' with {1} threads", uri, max_count);
			}
			List<string> list_files = new List<string> ();
			List<State> list_states = new List<State> ();
			List<State> list_remove = new List<State> (); // tracks which states should be removed
			ConcurrentStack<CrawlEntry> marked_files = new ConcurrentStack<CrawlEntry> ();
			CrawlEntry current;
			int counter = 0;

			marked_files.Push (new CrawlEntry(uri));
			//bool success = ThreadPool.SetMaxThreads (max_count, max_count);
			bool success;
			//if (!success && !quiet) {
				int workerThreads, ioThreads;
				ThreadPool.GetMaxThreads (out workerThreads, out ioThreads);
				Console.WriteLine ("Could not set max threads to {0}.", max_count);
				Console.WriteLine ("Maximum worker threads: {0}", workerThreads);
				Console.WriteLine ("Maximum completion port threads: {0}", ioThreads);
			//}

			while (marked_files.Count() > 0 || list_states.Count > 0) {
				if (marked_files.Count () > 0) {
					// there are still items to be processed
					// we can put them into the thread pool
					success = marked_files.TryPop (out current);
					list_files.Add (current.ToString());
					if (debug) {
						//Console.WriteLine ("{0} / {1} items!", list_files.Count, marked_files.Count);
					}
					if (success) {
						if (debug) {
							//Console.WriteLine ("New item: " + current);
						}
						State current_state = new State (current);
						list_states.Add (current_state);
						bool success_pool = ThreadPool.QueueUserWorkItem(new WaitCallback(CrawlUrl), current_state);
						if (success_pool) {
							counter ++;
							if (debug) {
								//Console.WriteLine ("Started new Threadpool Task #{0}", counter);
								//Console.WriteLine ("Currently using {0} Threads.", list_states.Count);
							}
						}
					}
				}

				// lets check if some job is done
				foreach (State state in list_states) {
					if (state.eventWaitHandle.WaitOne(100)) { // check if one job is done, non-blocking
						if (debug) {
							//Console.WriteLine ("CrawlUrl() is done with {0} new items", state.result.Count);
							foreach (CrawlEntry entry in state.result) {
								//Console.WriteLine ("Entry: "+entry);
								if (!marked_files.Contains (entry) && !list_files.Contains (entry.ToString ())) {
									//Console.WriteLine ("Pushed "+entry.ToString());
									marked_files.Push (entry);
								}
							}
						}
						list_remove.Add(state);
					}
				}

				if (debug && list_remove.Count > 0) {
					//Console.WriteLine ("Removing  {0} states", list_remove.Count);
				}
				// remove the states which are done
				foreach (State state in list_remove) {
					list_states.Remove (state);
					counter--;
				}
				list_remove.Clear ();
				
			}



			return list_files;
		}

		private class State
		{

			public EventWaitHandle eventWaitHandle = new ManualResetEvent(false);
			public List<CrawlEntry> result;
			public CrawlEntry entry;

			public State(CrawlEntry entry) {
				this.entry = entry;
				result = new List<CrawlEntry>();
			}

		}

		// PRODUCER! 
		private void CrawlUrl(object obj) {
			State state = (State) obj;
			CrawlEntry current = state.entry;
			Browser browser = new Browser ();
			List<string> files = new List<string> ();
			string content;
			if (debug) {
				//Console.WriteLine("Crawl URI: {0}", current);
			}
			int status = browser.get (current.ToString());
			if (status == 0) { // error occured
				state.eventWaitHandle.Set ();
				return; // we're done, as we get an error!
			}
			content = browser.getContent ();
			if (content == null) {
				if (debug) {
					Console.Error.WriteLine ("No content returned! aborting on '{0}'", current);
				}
				state.eventWaitHandle.Set ();
				return;
			}
			if (debug) {
				//Console.WriteLine ("Status: {0}", status);
				//Console.WriteLine("Length: {0}", content.Count());
			}
			if (status >= 200 && status < 300) { // OK
				List<string> urls = parseForUrls(content);
				files.AddRange(urls);
			}else if (status == 302 || status == 301) {
				string redirect = browser.getRedirect();
				if (debug) {
					//Console.WriteLine("redirection to {0}", redirect);
				}
				files.Add (redirect); // TODO validate!
			}
			if (debug) {
				//Console.WriteLine("CrawlUrl(\"{0}\") returns {1} items", current, files.Count);
			}

			// save result
			foreach (string entry_str in files) {
				try {
					CrawlEntry entry = new CrawlEntry(entry_str);
					state.result.Add(entry);
				} catch (Exception e) {
					Console.Error.WriteLine(e.ToString());
				}
			}

			state.eventWaitHandle.Set (); // We're done!
		}

		/// <summary>
		/// Searchs for href-links on a html site
		/// </summary>
		/// <param name="content">HTML Content of a website</param>
		///	<returns>List of all HTML links (normalized)</returns>
		private List<string> parseForUrls(string content) {
			List<string> list_href = new List<string> ();
			HtmlAgilityPack.HtmlDocument htmlDoc = new HtmlDocument ();
			htmlDoc.OptionFixNestedTags = true;
			htmlDoc.LoadHtml (content);


			foreach(HtmlNode node in htmlDoc.DocumentNode.SelectNodes("//a[@href]"))  {
				string href = node.Attributes ["href"].Value;
				string link = normalizeHref (href, uri.Authority);
				bool allow_add = validateLink (link);
				if (allow_add) {
					list_href.Add (link);
				}
			}
			return list_href;
		}


		/// <summary>
		/// Validates the given link.
		/// </summary>
		/// <description>
		/// Tries to cast the link into an Uri object, if this fails, the method returns false immediatly
		/// Otherwise will the crossdomain attribute be checked
		/// </description>
		/// <returns><c>true</c>, if link was validated successfully, <c>false</c> otherwise.</returns>
		/// <param name="link">link to check</param>
		private bool validateLink(string link) {
			Uri uri_check;
			try {
				uri_check = new Uri(link);
			} catch (Exception) {
				// Return false.
				// We don't allow links which can't be casted into URIs
				// but expand `normalizeHref` functionality
				return false;
			}
			if (!cross_domain) {
				if (!uri_check.Host.Equals (uri.Host)) { // Crossdomain 
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Tries to normalize a href-link
		/// </summary>
		/// <description>
		/// Strips all query-parameters and prepends the href with a host and scheme, if needed.
		/// May not work on all types of links, please file an issue if you experience problems (with an url/html file).
		/// </description>
		/// <param name="href">pre-processed href-link</param>
		/// <param name="host">host of the website</param>
		/// <param name="scheme">scheme string (optional, defaults to http)</param>
		///	<returns>normalized link</returns>
		private string normalizeHref(string href, string host, string scheme="http") {
			string link;
			Uri href_uri;

			if (href.StartsWith ("/")) {
				link = scheme + "://" + host + href;
				return link;
			}

			try {
				href_uri = new Uri(href); // lets just try it

				// lets build it, but omit the segment
				link = href_uri.AbsoluteUri;
				return link;
			} catch( Exception) {
				// damn, didn't work, we need to work ourselves a little bit!
			}

			return "";
		}


	}
}

