# Migrate your test suite from Testing Framework to Arcus.Testing
This guide will walk you through the process of migrating your test suite from using the Testing Framework to `Arcus.Testing`.

## Replace `Codit.Testing.OutputComparison/Xslt` with `Arcus.Testing.Assert`
The `Codit.Testing.OutputComparison` library has some functionality to compare different kinds of file types. The new `Arcus.Testing.Assert` library handles these comparisons from now on.

Start by installing this library:
```shell
PM > Install-Package -Name Arcus.Testing.Assert
```

### XML
You can use `AssertXml` like any other assertion method. Instead of returning a boolean and a message, it throws an exception with a detailed report in case of a difference.

```diff
- using Codit.Testing.OutputComparison;
+ using Arcus.Testing;

string expectedXml = ...;
string actualXml = ...;

- using var expectedStream = ...;
- using var actualStream = ...;
- bool isEqual = Xml.Compare(
-     expectedStream, actualStream, out string message, nodesToIgnore: Array.Empty<string>());

+ AssertXml.Equal(expectedXml, actualXml);
```

Any nodes that should be ignored can be configured with passing additional options:

```diff
- using Codit.Testing.OutputComparison;
+ using Arcus.Testing;

- bool isEqual = Xml.Compare(..., new[] { "ignore-this-node" });
+ AssertXml.Equal(..., options =>
+ {
+     options.AddIgnoreNode("ignore-this-node");
+ });
```

### JSON
You can use `AssertJson` like any other assertion method. Instead of returning a boolean and a message, it throws an exception with a detailed report in case of a difference.

```diff
- using Codit.Testing.Comparison;
+ using Arcus.Testing;

string expectedJson = ...;
string actualJson = ...;

- using var expectedStream = ...;
- using var actualStream = ...;
- bool isEqual = Json.Compare(
-    expectedStream, actualStream, out string message, nodesToIgnore: Array.Empty<string>());

+ AssertJson.Equal(expectedJson, actualJson);
```

Any nodes that should be ignored can be configured with passing additional options:

```diff
- using Codit.Testing.Comparison;
+ using Arcus.Testing;

- bool isEqual = Json.Compare(..., new[] { "ignore-this-node" });
+ AssertJson.Equal(..., options =>
+ {
+     options.AddIgnoreNode("ignore-this-node");
+ });
```

### XSLT
Transforming XML-XML to XML-JSON now also happens in a test asserted manner.

Here's how XML-XML now works:

```diff
- using.Codit.Testing.Xslt;
+ using Arcus.Testing;

- // _input.xml file is loaded implicitly.
- // _expected.xml file is loaded implicitly.
- bool successfullyTransformed = XsltHelper.TestXslt(
-     "transformer.xslt",
-     out string message,
-     xsltArgumentList: null,
-     MessageOutputType.Xml);

+ string inputXml = ... // Load _input.xml file explicitly.
+ string transformerXslt = ... // Load transformer.xslt file's contents here.

+ string actualXml = AssertXslt.TransformXml(transformerXslt, inputXml);
+ // Use `AssertXml.Equal` to determine the difference next.
```

> 💡You can use the test-friendly `AssertXml/Xslt.Load` functionality to load raw contents to their respectful XSLT/XML document. Upon failure, a load exception with a detailed description will be reported to the tester.

> 💡 You can use the test-friendly `ResourceDirectory` functionality in the `Arcus.Testing.Core` package to load raw file contents. Upon failure, a not-found exception with a detailed description will be reported to the tester.

Here's how XML-JSON now works:

```diff
- using.Codit.Testing.Xslt;
+ using Arcus.Testing;

- // _input.xml file is loaded implicitly.
- // _expected.json file is loaded implicitly.
- bool success = XsltHelper.TestXslt(
-     "transformer.xslt",
-     out string message,
-     xsltArgumentsList: null,
-     MessageOutputType.Json);

+ string inputXml = ... // Load _input.xml file explicitly.
+ string transformerXslt = ... // Load transformer.xslt file's contents here.

+ string actualJson = AssertXslt.TransformJson(transformerXslt, inputXml);
+ // Use `AssertJson.Equal` to determine the difference next.
```

> 💡You can use the test-friendly `AssertXml/Json/Xslt.Load` functionality to load raw contents to their respectful XSLT/XML document. Upon failure, a load exception with a detailed description will be reported to the tester.

> 💡 You can use the test-friendly `ResourceDirectory` functionality in the `Arcus.Testing.Core` package to load raw file contents. Upon failure, a not-found exception with a detailed description will be reported to the tester.