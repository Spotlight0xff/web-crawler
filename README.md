# web-crawler
Web crawler using C#

Build Status: [![Build Status](https://travis-ci.org/Spotlight0xff/web-crawler.svg?branch=master)](https://travis-ci.org/Spotlight0xff/web-crawler)



# **IMPORTANT INFORMATION**
This program is *Work in Progress* and will most likely not work as expected.



# Building
Use `nuget restore` and  `xbuild` to build the project/solution.
Only Mono is supported, no Windows Support!

# Dependencies
This project uses the [HTML Agility Pack](https://htmlagilitypack.codeplex.com/) and [NDesk.Options](http://ndesk.org/Options).
As described in **Building**, you can install the required packages using `nuget restore`.

# Running
If no errors occur, the binary file will be placed in web-crawler/bin/Debug/web-crawler or web-crawler/bin/Release/web-crawler.exe.
Running this program without arguments will show you the help and usage screen.


# Options
* -c, --crossdomain *whether to crawl through other domains aswell*
* -l, --depth=VALUE *the max depthlevel of folders to crawl (0 means no limit and is default)*
* -d, --debug *enables debug mode*
* -h, --help *show the help screen*
