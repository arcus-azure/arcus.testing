using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Bogus;
using Xunit;

namespace Arcus.Testing.Tests.Unit.Assert_.Fixture
{
    /// <summary>
    /// Represents a test fixture that generates random XML contents.
    /// </summary>
    public class TestXml
    {
        private XmlDocument _doc;
        private static readonly Faker Bogus = new();

        private TestXml(XmlDocument doc)
        {
            _doc = doc;
        }

        /// <summary>
        /// Generates random XML contents with randomly number of elements, attributes, namespaces and values.
        /// </summary>
        public static TestXml Generate()
        {
            var doc = new XmlDocument();

            XmlElement root = doc.CreateElement(GenPrefix(), GenNodeName(), GenNamespace());
            Assert.All(GenAttributes(doc), a => root.Attributes.Append(a));
            doc.AppendChild(root);

            int maxDepth = Bogus.Random.Int(3, 5);
            void Recurse(XmlElement current, int depth)
            {
                if (depth >= maxDepth)
                {
                    return;
                }

                XmlElement[] children =
                    Bogus.Make(Bogus.Random.Int(1, 5), GenNodeName)
                         .Select(name =>
                         {
                             string ns = Bogus.Random.Bool() ? current.NamespaceURI : GenNamespace();
                             return doc.CreateElement(GenPrefix(), name, ns);
                         })
                         .ToArray();

                foreach (XmlElement child in children)
                {
                    Assert.All(GenAttributes(doc), a => child.Attributes.Append(a));

                    if (Bogus.Random.Bool())
                    {
                        Recurse(child, depth + 1);
                    }
                    else
                    {
                        child.AppendChild(doc.CreateTextNode(Bogus.Random.AlphaNumeric(Bogus.Random.Int(1, 10))));
                    }

                    current.AppendChild(child);
                }
            }

            Recurse(root, 0);
            return new TestXml(doc);
        }

        private static XmlAttribute[] GenAttributes(XmlDocument doc)
        {
            return Bogus.Make(Bogus.Random.Int(5, 10), () => CreateAttribute(doc)).ToArray();
        }

        private static string GenPrefix()
        {
            string Gen()
            {
                return string.Concat(Bogus.PickRandom(Alphabet(), Bogus.Random.Int(4, 6)));
            }

            string prefix = Gen();
            while (prefix.StartsWith("xml"))
            {
                prefix = Gen();
            }

            return prefix;
        }

        private static string GenNamespace()
        {
            return Bogus.Internet.Url();
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            InsertRandomlyCDataSections();
            InsertRandomlyComments();

            return _doc.OuterXml;
        }

        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>A new object that is a copy of this instance.</returns>
        public TestXml Copy()
        {
            var doc = new XmlDocument();
            doc.LoadXml(_doc.OuterXml);

            return new TestXml(doc);
        }

        /// <summary>
        /// Changes the element name of a randomly picked element.
        /// </summary>
        public void ChangeElementName(string newName)
        {
            XmlElement oldElement = SelectRandomlyElement();
            XmlElement newElement = _doc.CreateElement(oldElement.Prefix, newName, oldElement.NamespaceURI);
            newElement.InnerXml = oldElement.InnerXml;

            Assert.NotNull(oldElement.ParentNode);
            oldElement.ParentNode.ReplaceChild(newElement, oldElement);
        }

        /// <summary>
        /// Changes the value of a randomly picked element.
        /// </summary>
        public void ChangeRandomlyElementValue(string newValue)
        {
            XmlElement element = SelectRandomlyElement();
            element.InnerText = newValue;
        }

        /// <summary>
        /// Changes the name of a randomly picked attribute.
        /// </summary>
        public void ChangeAttributeName(string newName)
        {
            XmlElement node = SelectRandomlyElement();

            string namespaceDefinition = "http://www.w3.org/2000/xmlns/";
            XmlAttribute oldAttribute = Bogus.PickRandom(node.Attributes.OfType<XmlAttribute>().Where(a => a.NamespaceURI != namespaceDefinition));
            XmlAttribute newAttribute = string.IsNullOrWhiteSpace(oldAttribute.NamespaceURI)
                ? _doc.CreateAttribute(oldAttribute.Prefix, newName, null)
                : _doc.CreateAttribute(oldAttribute.Prefix, newName, oldAttribute.NamespaceURI);

            newAttribute.Value = oldAttribute.Value;

            node.Attributes.InsertAfter(newAttribute, oldAttribute);
            node.Attributes.Remove(oldAttribute);
        }

        /// <summary>
        /// Changes the namespace of a randomly picked attribute
        /// </summary>
        public void ChangeAttributeNamespace(string newNamespace)
        {
            XmlElement node = SelectRandomlyElement();

            string namespaceDefinition = "http://www.w3.org/2000/xmlns/";
            XmlAttribute oldAttribute = Bogus.PickRandom(node.Attributes.OfType<XmlAttribute>().Where(a => a.NamespaceURI != namespaceDefinition));
            XmlAttribute newAttribute = _doc.CreateAttribute(GenPrefix(), oldAttribute.LocalName, newNamespace);

            node.Attributes.InsertAfter(newAttribute, oldAttribute);
            node.Attributes.Remove(oldAttribute);
        }

        /// <summary>
        /// Changes the value of a randomly picked attribute.
        /// </summary>
        public void ChangeAttributeValue(string newValue)
        {
            XmlElement node = SelectRandomlyElement();

            string namespaceDefinition = "http://www.w3.org/2000/xmlns/";
            XmlAttribute attribute = Bogus.PickRandom(node.Attributes.OfType<XmlAttribute>().Where(a => a.NamespaceURI != namespaceDefinition));
            attribute.Value = newValue;
        }

        /// <summary>
        /// Inserts a new element with a <paramref name="name"/> at a random place in the document.
        /// </summary>
        public void InsertElement(string name)
        {
            SelectRandomlyElement().AppendChild(_doc.CreateElement(name));
        }

        /// <summary>
        /// Inserts a new attribute with a <paramref name="name"/> at a random place in the document.
        /// </summary>
        public void InsertAttribute(string name)
        {
            SelectRandomlyElement().Attributes.Append(CreateAttribute(_doc, name));
        }

        private static XmlAttribute CreateAttribute(XmlDocument doc, string name = null)
        {
            if (Bogus.Random.Bool())
            {

                return doc.CreateAttribute(name ?? GenAttributeName());
            }

            return doc.CreateAttribute(GenPrefix(), name ?? GenAttributeName(), GenNamespace());
        }

        private static string GenAttributeName()
        {
            string name = GenNodeName();
            while (name.StartsWith("xml"))
            {
                name = GenNodeName();
            }

            return name;
        }

        /// <summary>
        /// Generates a valid XML node name.
        /// </summary>
        public static string GenNodeName()
        {
            char[] alphabet = Alphabet();
            char firstChar = Bogus.PickRandom(alphabet.Concat(alphabet).Append('_'));

            return firstChar + string.Concat(Bogus.Make(Bogus.Random.Int(1, 10), () => Bogus.PickRandom(alphabet)));
        }

        private static char[] Alphabet()
        {
            char[] alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();
            return alphabet.Select(char.ToLower).ToArray();
        }

        private void InsertRandomlyComments()
        {
            var comments = Bogus.Make(
                Bogus.Random.Int(1, 10),
                () => _doc.CreateComment(Bogus.Lorem.Sentence()));

            Assert.All(comments,
                comment => SelectRandomlyElement().AppendChild(comment));
        }

        private void InsertRandomlyCDataSections()
        {
            var cdatas = Bogus.Make(
                Bogus.Random.Int(1, 10),
                () => _doc.CreateCDataSection(Bogus.Random.AlphaNumeric(Bogus.Random.Int(10, 20))));

            Assert.All(cdatas,
                cdata => SelectRandomlyElement().AppendChild(cdata));
        }

        /// <summary>
        /// Selects a random element within the document.
        /// </summary>
        public XmlElement SelectRandomlyElement(Func<XmlElement, bool> additionalFilter = null)
        {
            return SelectRandomly(_doc.DocumentElement, additionalFilter);
        }

        private static XmlElement SelectRandomly(XmlElement node, Func<XmlElement, bool> filter)
        {
            IEnumerable<XmlElement> elements =
                node.ChildNodes.OfType<XmlElement>()
                    .Select(n => SelectRandomly(n, filter))
                    .Where(n => filter?.Invoke(n) ?? true)
                    .Concat(filter?.Invoke(node) ?? true ? new[] { node } : Array.Empty<XmlElement>());

            return Bogus.PickRandom(elements);
        }

        public void Shuffle()
        {
            XmlNode shuffled = Shuffle(_doc.DocumentElement);

            var xml = new XmlDocument();
            xml.LoadXml(shuffled.OuterXml);
            _doc = xml;
        }

        private static XmlNode Shuffle(XmlNode node)
        {
            if (node is XmlElement element)
            {
                XmlAttribute[] attributes = Bogus.Random.Shuffle(
                    element.Attributes.OfType<XmlAttribute>()
                           .Where(a => a.NamespaceURI != "http://www.w3.org/2000/xmlns/")).ToArray();

                Assert.All(attributes, attr => element.RemoveAttribute(attr.Name));

                foreach (XmlAttribute attr in attributes)
                {
                    element.SetAttributeNode(attr);
                }

                foreach (XmlElement child in element.ChildNodes.OfType<XmlElement>())
                {
                    Shuffle(child);
                }

                return element;
            }

            return node;
        }
    }
}
