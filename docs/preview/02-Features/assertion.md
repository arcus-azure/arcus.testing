# Test assertion
The `Arcus.Testing.Assert` library is a highly reusable and independent library that contains dev-friendly test assertions on different types of output (XML, JSON...). The purpose of the library is to write readable and test-friendly assertions that show quickly what went wrong with the (XML, JSON...) output of a certain piece of functionality.

## Installation
Install this package to easily assert on different output contents:

```shell
PM> Install-Package -Name Arcus.Testing.Assert
```

## XML
The library has an `AssertXml` class that exposes usable test assertions when dealing with XML outputs. The most popular one is comparing XML documents.

```csharp
using Arcus.Testing;

string expected = "<root>...</root>";
string actual = "<diff-root>...</diff-root>";

AssertXml.Equal(expected, actual);
// Arcus.Testing.EqualAssertionException
// AssertXml.Equal failure: expected and actual XML documents do not match
// Expected element tag name 'root' but was 'diff-root' at /root
//
// Expected:
// <root>
//      ...
// </root>
//
// Actual:
// <diff-root>
//      ...
// </diff-root>
```

💡 Currently, the input contents are trimmed in case the input is too big to be shown in a humanly readable manner to the test output. In case of large files, it might be best to log those files (or parts that interest you) separately before using this test assertion.

### Customization
The test assertion also exposes several options to tweak the behavior of the XML comparison.

```csharp
using Arcus.Testing;

AssertXml.Equal(..., options =>
{
    // Adds one ore more local names of XML nodes that should be excluded from the XML comparison.
    options.IgnoreNode("local-node-name");

    // Sets the type of order which should be used when comparing XML attributes.
    // REMARK: only the order of XML attributes can be set, XML elements are still compared by their contents.
    // Default: Ignore.
    options.Order = AssertXmlOrder.Include;

    // Sets the maximum characters of the expected and actual inputs that should be written to the test output.
    // Default: 500 characters.
    options.MaxInputCharacters = 1000;
});
```

### Loading XML documents yourself
The XML assertion equalization can be called directly with raw contents - internally it parses to a valid XML structure: `XmlDocument`. If you want to compare two XML nodes with different serialization settings, you can load the two nodes separately and do the equalization on the loaded nodes.

💡 It provides you with more options to control how your file should be loaded.

```csharp
using System.Xml;
using Arcus.Testing;

string xml = ...;

XmlDocument expected = AssertXml.Load(json);

// Or with options (same as available with `AssertJson.Equal`).
XmlDocument actual = AssertXml.Load(json, options => ...);

// Use overload with nodes instead.
AssertXml.Equal(expected, actual);
```

> ❓ Why use the `AssertXml` to load something that is already available with `XmlDocument.LoadXml`?

The `AssertXml.Load` is a special variant on the existing load functionality in such a way that it provides more descriptive information on the input file that was trying to be parsed, plus by throwing an assertion message, it makes the test output more clear on what the problem was and where it happened. 

## JSON
The library has an `AssertJson` class that exposes useful test assertions when dealing with JSON outputs. The most popular one is comparing JSON documents.

```csharp
using Arcus.Testing;

string expected = "{ \"root\": ... }";
string actual = "{ \"diff-root\": ... }";

AssertJson.Equal(expected, actual);
// Arcus.Testing.EqualAssertionException
// AssertJson.Equal failure: expected and actual JSON contents do not match
// Actual JSON misses property at $.root
//
// Expected:
// {
//    "root": ...
// }
//
// Actual:
// {
//    "diff-root": ...
// }
```

💡 Currently, the input contents are trimmed in case the input is too big to be shown in a humanly readable manner to the test output. In case of large files, it might be best to log those files (or parts that interest you) separately before using this test assertion.

### Customization
The test assertion also exposes several options to tweak the behavior of the JSON comparison.

```csharp
using Arcus.Testing;

AssertJson.Equal(..., options =>
{
    // Adds one ore more property names of JSON nodes that should be excluded from the JSON comparison.
    options.IgnoreNode("node-name");

    // Sets the type of order which should be used when comparing JSON array values.
    // REMARK: only the order of JSON values can be set, JSON objects within JSON arrays are still compared by their contents.
    // Default: Ignore.
    options.Order = AssertJsonOrder.Include;

    // Sets the maximum characters of the expected and actual inputs that should be written to the test output.
    // Default: 500 characters.
    options.MaxInputCharacters = 1000;
});
```

### Loading JSON nodes yourself
The JSON assertion equalization can be called directly with raw contents - internally it parses to a valid JSON structure: `JsonNode`. If you want to compare two JSON nodes with different serialization settings, you can load the two nodes separately and do the equalization on the loaded nodes.

💡 It provides you with more options to control how your file should be loaded.

```csharp
using System.Text.Json.Nodes;
using Arcus.Testing;

string json = ...;

JsonNode expected = AssertJson.Load(json);

// Or with options (same as available with `AssertJson.Equal`).
JsonNode actual = AssertJson.Load(json, options => ...);

// Use overload with nodes instead.
AssertJson.Equal(expected, actual);
```

> ❓ Why use the `AssertJson` to load something that is already available with `JsonNode.Parse`?

The `AssertJson.Load` is a special variant on the existing load functionality in such a way that it provides more descriptive information on the input file that was trying to be parsed, plus by throwing an assertion message, it makes the test output more clear on what the problem was and where it happened. 


## XSLT
The library has an `AssertXslt` class that exposes useful test assertions when dealing with XSLT transformation. The most popular one is transforming XML contents to either XML or JSON.

🎖️ Since the XSLT transformation execution is now run as a test assertion, any failure during transformation or loading of the input/output/transformation will be reported to the tester in a humanly readable manner.

```csharp
using Arcus.Testing;

// XML -> XML
// ----------------------------------------------------
string input = "<data>...</data>";
string xslt = "<xsl:stylesheet>...</xsl:stylesheet>";

string expected = "<other-data>...</other-data>"
string actual = AssertXslt.TransformXml(xslt, input);
// Use `AssertXml.Equal` to do the equalization.

// XML -> JSON
// ----------------------------------------------------
string input = "<data>...</data>";
string xslt = "<xsl:stylesheet>...</xsl:stylesheet>";

string expected = "{ \"data\": ... }";
string actual = AssertXslt.TransformJson(xslt, input);
// Use `AssertJson.Equal` to do the equalization.
```

💡 Both types of transformations can be adapted by passing any runtime XSLT arguments.

> ❓ Why use the `AssertXslt` to load something that is already available with `XsltCompiledTransform.Load`?

The `AssertXslt.TransformXml/Json` are special variants on the existing transform functionality in such a way that it provides more descriptive information on the input file that was trying to be parsed, plus by throwing an assertion message, it makes the test output more clear on what the problem was and where it happened. 

### Loading XSLT transformations yourself
The asserted XSLT transformation is loading valid `XslCompiledTransform` instances from raw contents, but you can also call this asserted loading yourself before calling the asserted transformation.

```csharp
using System.Text.Json.Nodes;
using System.Xml;
using System.Xml.Xsl;
using Arcus.Testing;

string xslt = "<xsl:stylesheet>...</xsl:stylesheet>";
XslCompiledTransform transformer = AssertXslt.Load(xslt);

// Use overload with compiled transform instead.
XmlDocument xml = AssertXslt.TransformXml(transformer, ...);
JsonNode json = AssertXslt.TransformJson(transformer, ...);
```

> ❓ Why use the `AssertXslt.Load` to load something that is already available with `XslCompiledTransform.Load`?

The `AssertXslt.Load` is a special variant on the existing load functionality in such a way that it provides more descriptive information on the input file that ws trying to be parsed, plus by throwing an assertion message, it mes the test output more clear on what the problem was and where it happened.
