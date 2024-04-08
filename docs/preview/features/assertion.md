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

💡 Currently, the input contents are trimmed in case if the input is too big to be shown in a humanly readable manner to the test output. In case of large files, it might be best to log those files (or parts that interest you) separately before using this test assertion.

### Configuration
The test assertion also expose several options to tweak the behavior of the XML comparison.

```csharp
AssertXml.Equal(..., options =>
{
    // Adds one ore more local names of XML nodes that should be excluded from the XML comparison.
    options.IgnoreNode("local-node-name");
});
```

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

💡 Currently, the input contents are trimmed in case if the input is too big to be shown in a humanly readable manner to the test output. In case of large files, it might be best to log those files (or parts that interest you) separately before using this test assertion.

### Configuration
The test assertion also expose several options to tweak the behavior of the JSON comparison.

```csharp
AssertJson.Equal(..., options =>
{
    // Adds one ore more property names of JSON nodes that should be excluded from the JSON comparison.
    options.IgnoreNode("node-name");
});
```

## XSLT
The library has an `AssertXslt` class that exposes useful test assertions when dealing with XSLT transformation. The most popular one is transforming XML contents to either XML or JSON.

🎖️ Since the XSLT transformation execution is now run as a test assertion, any failure during transformation or loading of the input/output/transformation will be reported to the tester in a humanly readable manner.

```csharp
using Arcus.Testing.Assert;

// XML -> XML
// ----------------------------------------------------
string input = "<data>...</data>";
string xslt = "<xsl:stylesheet>...</xsl:stylesheet>";

string expected = "<other-data>...</other-data>"
string actual = AssertXslt.TransformXml(xslt, input);

// XML -> JSON
// ----------------------------------------------------
string input = "<data>...</data>";
string xslt = "<xsl:stylesheet>...</xsl:stylesheet>";

string expected = "{ \"data\": ... }";
string actual = AssertXslt.TransformJson(xslt, input);
```

💡 Both types of transformations can be adapted by passing any runtime XSLT arguments.