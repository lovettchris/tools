﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace HttpGet
{
    class Program
    {
        bool all;
        bool deep;
        bool headers;
        string filename;
        Uri baseUrl;
        string rootDir;
        bool stats;
        int depth;
        Dictionary<Uri, string> fetched = new Dictionary<Uri, string>();

        static void Main(string[] args)
        {
            Program p = new Program();
            if (!p.ParseCommandLine(args))
            {
                PrintUsage();
                return;
            }

            try
            {
                p.Run();
            }
            catch (WebException e)
            {
                Console.WriteLine("### Error: {0}", e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine("### Error: {0} {1}", e.GetType().FullName, e.Message);
            }
        }

        private bool ParseCommandLine(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (arg[0] == '-' || arg[0] == '/')
                {
                    string option = arg.Substring(1);
                    switch (option)
                    {
                        case "a":
                        case "all":
                            all = true;
                            break;
                        case "d":
                        case "deep":
                            deep = true;
                            break;
                        case "s":
                            stats = true;
                            break;
                        case "?":
                        case "h":
                        case "help":
                            return false;
                        case "filename":
                            if (i + 1 < args.Length)
                            {
                                this.filename = Path.GetFullPath(args[++i]);
                            }
                            else
                            {
                                Console.WriteLine("### missing file name argument");
                                return false;
                            }
                            break;
                        case "headers":
                            this.headers = true;
                            break;
                    }
                }
                else if (baseUrl == null)
                {
                    baseUrl = new Uri(arg);
                }
                else
                {
                    Console.WriteLine("### Too many arguments");
                    return false;
                }
            }
            if (baseUrl == null)
            {
                Console.WriteLine("### Missing url argument");
                return false;
            }

            return true;
        }

        private void Run()
        {
            Process(this.baseUrl);
        }

        private void Process(Uri uri)
        {
            string path = Download(uri);
            if (path == null)
            {
                return;
            }
            if (all || deep)
            {
                string fullPath = Path.GetFullPath(path);
                string ext = Path.GetExtension(uri.ToString()).ToLowerInvariant();
                if (ext == "htm" || ext == "html" || ext == "")
                {
                    XDocument doc = null;
                    using (var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read))
                    {
                        using (var reader = new StreamReader(stream))
                        {
                            // setup SgmlReader
                            Sgml.SgmlReader sgmlReader = new Sgml.SgmlReader();
                            sgmlReader.DocType = "HTML";
                            sgmlReader.WhitespaceHandling = WhitespaceHandling.All;
                            sgmlReader.CaseFolding = Sgml.CaseFolding.ToLower;
                            sgmlReader.InputStream = reader;

                            doc = XDocument.Load(sgmlReader);
                            FetchCss(uri, doc);
                            FetchAudio(uri, doc);
                            FetchImages(uri, doc);
                            FetchSvgImages(uri, doc);
                            FetchScripts(uri, doc);
                        }
                    }

                    // save valid XML version
                    try
                    {
                        doc.Save(fullPath);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("### Error saving XML file: " + ex.Message);
                    }

                    if (deep)
                    {
                        TraverseLinks(uri, doc);
                    }
                }
            }
        }

        private void CheckValidLink(Uri uri)
        {
            Console.WriteLine("Fetching: " + uri.AbsoluteUri);
            WebRequest req = WebRequest.Create(uri);

            req.Credentials = CredentialCache.DefaultNetworkCredentials;
            req.Method = "GET";
            HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
            if (resp.StatusCode != HttpStatusCode.OK)
            {
                Console.WriteLine("### Error returned from : " + uri.ToString());
                Console.WriteLine("### " + resp.StatusDescription);
            }

        }

        private void TraverseLinks(Uri baseUri, XDocument doc)
        {
            // ok, now check all <a> links in this page and if they are local download them, if they are remote
            // check that they are valid, but don't traverse them.
            depth++;
            XNamespace ns = doc.Root.Name.Namespace;
            foreach (XElement link in doc.Descendants(ns + "a"))
            {
                string href = (string)link.Attribute("href");
                if (!string.IsNullOrWhiteSpace(href))
                {
                    Uri resolved = new Uri(baseUri, href);
                    Process(resolved);
                }
            }
            depth--;
        }

        private void FetchCss(Uri baseUri, XDocument doc)
        {
            XNamespace ns = doc.Root.Name.Namespace;
            foreach (XElement link in doc.Descendants(ns + "link"))
            {
                string href = (string)link.Attribute("href");
                if (!string.IsNullOrWhiteSpace(href))
                {
                    try
                    {
                        Uri resolved = new Uri(baseUri, href);
                        string local = Download(resolved);
                        if (local != null && local != href)
                        {
                            link.SetAttributeValue("href", local);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("### Error: " + ex.Message);
                        Console.WriteLine("### Error: broken link: " + href);
                        Console.WriteLine("### Error: referenced : " + baseUri.ToString());
                    }
                }
            }
        }

        private void FetchAudio(Uri baseUri, XDocument doc)
        {
            XNamespace ns = doc.Root.Name.Namespace;
            foreach (XElement link in doc.Descendants(ns + "audio"))
            {
                string href = (string)link.Attribute("src");
                if (!string.IsNullOrWhiteSpace(href))
                {
                    Uri resolved = new Uri(baseUri, href);
                    string local = Download(resolved);
                    if (local != null && local != href)
                    {
                        link.SetAttributeValue("src", local);
                    }
                }
            }
        }

        private void FetchImages(Uri baseUri, XDocument doc)
        {
            XNamespace ns = doc.Root.Name.Namespace;
            foreach (XElement link in doc.Descendants(ns + "img"))
            {
                string href = (string)link.Attribute("src");
                if (!string.IsNullOrWhiteSpace(href))
                {
                    Uri resolved = new Uri(baseUri, href);
                    string local = Download(resolved);
                    if (local != null && local != href)
                    {
                        link.SetAttributeValue("src", local);
                    }
                }
            }
        }

        private void FetchSvgImages(Uri baseUri, XDocument doc)
        {
            XNamespace svgns = XNamespace.Get("http://www.w3.org/2000/svg");
            XNamespace xlinkns = XNamespace.Get("http://www.w3.org/1999/xlink");
            foreach (XElement link in doc.Descendants(svgns + "image"))
            {
                string href = (string)link.Attribute(xlinkns + "href");
                if (!string.IsNullOrWhiteSpace(href))
                {
                    Uri resolved = new Uri(baseUri, href);
                    string local = Download(resolved);
                    if (local != null && local != href)
                    {
                        link.SetAttributeValue(xlinkns + "href", local);
                    }
                }
            }
        }

        private void FetchScripts(Uri baseUri, XDocument doc)
        {
            XNamespace ns = doc.Root.Name.Namespace;
            foreach (XElement link in doc.Descendants(ns + "script"))
            {
                string href = (string)link.Attribute("src");
                if (!string.IsNullOrWhiteSpace(href))
                {
                    Uri resolved = new Uri(baseUri, href);
                    string local = Download(resolved);
                    if (local != null && local != href)
                    {
                        link.SetAttributeValue("src", local);
                    }
                }
            }
        }

        private string Download(Uri uri)
        {
            string result = null;

            if (fetched.ContainsKey(uri))
            {
                // already taken care of
                return null;
            }

            bool external = (uri.Host != this.baseUrl.Host);

            try
            {

                Uri rel = this.baseUrl.MakeRelativeUri(uri);
                string relative = rel.ToString();                
                if (relative != "" && !external)
                {
                    string subs = "";
                    string[] dirs = relative.Split('/', '\\');
                    for (int i = 0; i < dirs.Length; i++)
                    {
                        string dir = dirs[i];
                        if (dir == ".." || string.IsNullOrWhiteSpace(dir))
                        {
                            // ignore this
                        }
                        else if (i == dirs.Length - 1)
                        {
                            // the file
                            string fname = dir;
                            result = Path.Combine(subs, fname);
                            subs = "";
                        }
                        else
                        {
                            // a directory
                            subs = Path.Combine(subs, dir);
                            string newPath = Path.Combine(this.rootDir, subs);
                            if (!Directory.Exists(newPath))
                            {
                                Directory.CreateDirectory(newPath);
                            }
                        }
                    }
                }

                fetched[uri] = null;

                if (uri.Scheme != "http" && uri.Scheme != "https")
                {
                    return null;
                }

                Console.WriteLine(depth + ") Fetching: " + uri.AbsoluteUri);
                WebRequest req = WebRequest.Create(uri);
                req.Credentials = CredentialCache.DefaultNetworkCredentials;
                req.Method = external ? "HEAD" : "GET";

                using (WebResponse resp = req.GetResponse())
                {

                    if (headers)
                    {
                        foreach (string key in resp.Headers.AllKeys)
                        {
                            Console.WriteLine(key + "=" + resp.Headers[key]);
                        }
                        return null;
                    }

                    if (external)
                    {
                        // stop here, don't traverse into external links, just check they are ok.
                        return null;
                    }
                    using (var stream = resp.GetResponseStream())
                    {
                        if (string.IsNullOrEmpty(result))
                        {
                            string fname = uri.Segments[uri.Segments.Length - 1];
                            if (fname == "/")
                            {
                                fname = "default.htm";
                            }
                            result = fname;
                        }
                        if (result.EndsWith("/"))
                        {
                            result = result.Trim('/');
                        }
                        if (string.IsNullOrEmpty(System.IO.Path.GetExtension(result).ToLowerInvariant()))
                        {
                            result += ".htm";
                        }

                        System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
                        watch.Start();
                        long length = CopyToFile(stream, result);
                        watch.Stop();
                        if (stats)
                        {
                            double bps = (double)length / watch.Elapsed.TotalSeconds;
                            Console.WriteLine("Download speed: {0} bytes per second", Math.Round(bps, 3));
                        }
                        if (stats)
                        {
                            Console.WriteLine("Saved local file: " + Path.GetFullPath(result));
                        }
                    }
                }
                if (rootDir == null)
                {
                    rootDir = Path.GetDirectoryName(Path.GetFullPath(result));
                }
                fetched[uri] = result;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error downloading URL: " + ex.Message);
                return null;
            }
            return result;
        }

        private static long CopyToFile(Stream stream, string path)
        {
            long length = 0;
            byte[] buffer = new byte[64000];
            using (var file = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                int len = stream.Read(buffer, 0, buffer.Length);
                while (len > 0)
                {
                    file.Write(buffer, 0, len);
                    length += len;
                    len = stream.Read(buffer, 0, buffer.Length);
                }
            }
            return length;
        }

        private static void PrintUsage()
        {
            Console.WriteLine("HttpGet [options] <url>");
            Console.WriteLine("Fetches the resource at the given URL and saves it locally");
            Console.WriteLine("Options:");
            Console.WriteLine("   -all      if url is xhtml it brings down all locally referenced resources with the file");
            Console.WriteLine("   -filename the name of the file to save http content into (default writes to stdout)");
            Console.WriteLine("   -headers  just print http headers to stdout");
        }
    }
}
