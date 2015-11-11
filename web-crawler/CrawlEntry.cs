using System;

namespace webcrawler
{
	public class CrawlEntry
	{
		string entry;
		string referrer;
		Uri uri;

		public CrawlEntry (string url)
		{
			referrer = "";
			entry = url;
		}

		public CrawlEntry(Uri uri) {
			this.uri = uri;
			entry = uri.AbsoluteUri;
		}

		public override string ToString ()
		{
			if (entry != "") {
				return entry;
			} else {
				return uri.ToString ();
			}
		}
	}
}

