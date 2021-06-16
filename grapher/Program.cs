using System;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Xml;
using Grapher.Model;
using System.Linq;
using System.Collections.Generic;

namespace Grapher
{
    class Program
    {
        static void Main(string[] args)
        {
            string graph = @"
digraph {
node[
  shape=rect
  width=0 height=0 margin=0.07
  style=filled
  fontsize=11]
edge[
  penwidth=1
  arrowsize=0.6
  arrowhead=vee
  pencolor=""#444444""]
rankdir = LR
nodesep = 0.07
ranksep = 0.5
";
            string json = File.ReadAllText("D:\\repos\\source-build-tools\\grapher\\manifest.json");
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            };
            Manifest manifest = JsonSerializer.Deserialize<Manifest>(json, options);
            foreach (Repo repo in manifest.Repos)
            {

                Console.WriteLine($"Processing:  {repo.Name}");
                Console.WriteLine("-------------------------------------------------------------");

                graph += $"{Environment.NewLine}\"{repo.Name}\"";

                if (repo.NonArcade || repo.IsExternal)
                {
                    continue;
                }

                WebClient wc = new WebClient();
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                string versionDetailsUrl = $"{repo.Url}/main/eng/Version.Details.xml"
                    .Replace("https://github.com/", "https://raw.githubusercontent.com/");
                Console.WriteLine($"Version.Details.xml content from {versionDetailsUrl}:");
                string versionDetailsXml = wc.DownloadString(versionDetailsUrl);
                Console.WriteLine(versionDetailsXml);

                XmlDocument xml = new XmlDocument();
                xml.LoadXml(versionDetailsXml);

                Dictionary<string, bool> dependencies = new Dictionary<string, bool>();
                XmlNodeList xnList = xml.SelectNodes("/Dependencies/*/Dependency");
                foreach (XmlNode dependency in xnList)
                {
                    XmlNode uri = dependency.SelectSingleNode("Uri");
                    bool isSourceBuilt = dependency.SelectSingleNode("SourceBuild") != null;

                    if (!manifest.Repos.Select(repo => repo.Url).Contains(uri.InnerText))
                    {
                        continue;
                    }

                    if (!dependencies.ContainsKey(uri.InnerText))
                    {
                        dependencies.Add(uri.InnerText, isSourceBuilt);
                    }
                    else if (isSourceBuilt)
                    {
                        dependencies[uri.InnerText] = isSourceBuilt;
                    }
                }

                foreach (KeyValuePair<string, bool> kvp in dependencies)
                {
                    graph += $"{Environment.NewLine}\"{repo.Name}\" -> \"{kvp.Key.Split("/").Last()}\"";
                    if (kvp.Value)
                    {
                        graph += " [color = green]";
                    }
                }
            }

            graph += $"{Environment.NewLine}}}";
            File.WriteAllText("D:\\repos\\source-build-tools\\grapher\\graph.dot", graph);
        }
    }
}
