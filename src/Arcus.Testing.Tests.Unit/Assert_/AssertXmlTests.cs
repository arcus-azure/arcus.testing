using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using Bogus;
using FsCheck.Xunit;
using Xunit;

namespace Arcus.Testing.Tests.Unit.Assert_
{
    public class AssertXmlTests
    {
        private static readonly Faker Bogus = new();

        [Property]
        public void Compare_SameXml_Succeeds()
        {
            string expected = RandomXml();
            string actual = expected;

            AssertXml.Equal(expected, actual);
        }

        [Property]
        public void Compare_WithIgnoreDiff_StillSucceeds()
        {
            // Arrange
            string expected = RandomXml();

            string[] diffNodeNames = CreateNodeNames("diff-");
            string actual = diffNodeNames.Aggregate(expected, AppendNode);

            // Act / Assert
            AssertXml.Equal(expected, actual, 
                options => Assert.All(diffNodeNames, name => options.IgnoreNode(name)));
        }

        private static string[] CreateNodeNames(string prefix)
        {
            return Bogus.Make(Bogus.Random.Int(5, 10), () => CreateNodeName(prefix)).ToArray();
        }

        private static string AppendNode(string xml, string nodeName)
        {
            var doc = new XmlDocument();
            doc.LoadXml(xml);

            XmlNode node = doc.ChildNodes.Item(Bogus.Random.Int(0, doc.ChildNodes.Count - 1));
            Assert.NotNull(node);
            node.AppendChild(doc.CreateElement(nodeName));

            return doc.OuterXml;
        }

        [Fact]
        public void Compare_DiffXml_Fails()
        {
            string expected = RandomXml();
            string actual = RandomXml();

            Assert.ThrowsAny<AssertionException>(() => AssertXml.Equal(expected, actual));
        }

        private static string RandomXml()
        {
            int maxDepth = Bogus.Random.Int(1, 3);
            StringBuilder Recurse(StringBuilder acc, int depth)
            {
                if (depth >= maxDepth)
                {
                    return acc;
                }

                string[] nodeNames = Bogus.Make(Bogus.Random.Int(1, 10), () => CreateNodeName()).ToArray();
                foreach (string nodeName in nodeNames)
                {
                    if (Bogus.Random.Bool())
                    {
                        if (Bogus.Random.Bool())
                        {
                            AddEmptyNode(acc, nodeName);
                        }

                        AddOpenNode(acc, nodeName);
                        acc.AppendLine(Bogus.PickRandom(
                            Bogus.Random.Int().ToString(), 
                            Bogus.Lorem.Word()));
                        AddCloseNode(acc, nodeName);
                    }
                    else
                    {
                        AddOpenNode(acc, nodeName);
                        Recurse(acc, depth + 1);
                        AddCloseNode(acc, nodeName);
                    }
                }

                return acc;
            }

            var builder = new StringBuilder();
            string root = Bogus.Lorem.Word();
            AddOpenNode(builder, root);
            builder = Recurse(builder, depth: 1);
            AddCloseNode(builder, root);

            return builder.ToString();
        }

        private static string CreateNodeName(string prefix = null)
        {
            return prefix + Bogus.Lorem.Word();
        }

        private static void AddOpenNode(StringBuilder builder, string name)
        {
            string attributesTxt = RandomAttributesTxt();
            builder.AppendLine($"<{name} {attributesTxt}>");
        }

        private static void AddCloseNode(StringBuilder builder, string name)
        {
            builder.AppendLine($"</{name}>");
        }

        private static void AddEmptyNode(StringBuilder builder, string name)
        {
            string attributesTxt = RandomAttributesTxt();
            builder.AppendLine($"<{name} {attributesTxt}/>");
        }

        private static string RandomAttributesTxt()
        {
            IList<(string name, string value)> attributes = Bogus.Make(
                Bogus.Random.Int(1, 5),
                () => (Bogus.Lorem.Word() + Bogus.Random.Guid().ToString()[..5], Bogus.Lorem.Word()));

            return string.Join(" ", attributes.Select(a => $"{a.name}=\"{a.value}\""));
        }

        [Fact]
        public void Load_WithInvalidXml_FailsWithDescription()
        {
            var exception = Assert.Throws<XmlException>(() => AssertXml.Load(Bogus.Random.String()));
            Assert.Contains(nameof(AssertXml), exception.Message);
            Assert.Contains("XML contents", exception.Message);
        }
    }
}
