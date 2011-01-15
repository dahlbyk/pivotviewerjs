using System;
using System.Linq;
using System.Xml.Linq;
using System.Web.Script.Serialization;
using System.IO;
using System.Collections.Generic;

namespace cxml2json
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
                throw new Exception("u dum");

            var cxml = XDocument.Load(args[0]);

            var ns = (XNamespace)"http://schemas.microsoft.com/collection/metadata/2009";
            var categories = from category in cxml.Root.Element(ns + "FacetCategories").Elements()
                             select new
                             {
                                 Name = (string)category.Attribute("Name"),
                                 Type = (string)category.Attribute("Type"),
                                 Format = (string)category.Attribute("Format"),
                                 // TODO: Extensions
                             };

            var items = from item in cxml.Root.Element(ns + "Items").Elements()
                        select new
                        {
                            Id = (string)item.Attribute("Id"),
                            Name = (string)item.Attribute("Name"),
                            Img = (string)item.Attribute("Img"),
                            Href = (string)item.Attribute("Href"),
                            Description = (string)item.Element(ns + "Description"),
                            Facets = GetFacets(ns, item.Element(ns + "Facets")).ToArray(),
                        };

            var collection = new
            {
                Name = (string)cxml.Root.Attribute("Name"),
                FacetCategories = categories.ToArray(),
                Items = items.ToArray(),
            };

            var serializer = new JavaScriptSerializer();

            using (var fs = File.Create(args[1]))
            using (var sw = new StreamWriter(fs))
                sw.Write(serializer.Serialize(collection));
        }

        private static object GetFacetValue(string name, XElement value)
        {
            switch (value.Name.LocalName)
            {
                case "Number":
                    return new { Name = name, Number = (decimal?)value.Attribute("Value") };
                case "String":
                    return new { Name = name, String = (string)value.Attribute("Value") };
                default:
                    Console.WriteLine("Unexpected facet value for {0}: {1}", name, value);
                    return new { Name = name };
            }
        }

        private static IEnumerable<object> GetFacets(XNamespace ns, XElement facets)
        {
            return from facet in facets.Elements(ns + "Facet")
                   let name = (string)facet.Attribute("Name")
                   from value in facet.Elements()
                   select GetFacetValue(name, value);
        }
    }
}
