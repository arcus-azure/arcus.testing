# Migrate your test suite from Testing Framework to Arcus.Testing v1.0
This guide will walk you through the process of migrating your test suite from using the Testing Framework to `Arcus.Testing`.

> ⚠️ **IMPORTANT** to note that the `Arcus.Testing` approach uses the files in the build output in any of its functionality (`TestConfig`, `ResourceDirectory`...). It uses this approach for more easily access to the actual files used (instead of hidden as an embedded resource). It is best to add your files needed in your test project either as links ([see how](https://jeremybytes.blogspot.com/2019/07/linking-files-in-visual-studio.html)) or as actual files if only used for testing. **In both cases, they need to be copied to the output.**:
> 
> ```xml
> <ItemGroup>
>   <None Update="resource.xml">
>     <CopyToOutputDirectory>Always</CopyToOutputDirectory>
>   </None>
> </ItemGroup>
> ```

## Replace `Codit.Testing.OutputComparison/Xslt` with `Arcus.Testing.Assert`
The `Codit.Testing.OutputComparison` library has some functionality to compare different kinds of file types. The new `Arcus.Testing.Assert` library handles these comparisons from now on.

Start by installing this library:
```shell
PM > Install-Package -Name Arcus.Testing.Assert
```

🔗 See the [feature documentation](../02-Features/assertion.md) for more info on the supported assertions.

🔗 See the [code samples](https://github.com/arcus-azure/arcus.testing/tree/main/samples) for fully-implemented examples on before/after with the Testing Framework.

### XML
You can use `AssertXml` like any other assertion method. Instead of returning a boolean and a message, it throws an exception with a detailed report in case of a difference.

```diff
- using Codit.Testing.OutputComparison;
+ using Arcus.Testing;

string expectedXml = ...;
string actualXml = ...;

- using Stream expectedXmlStream = ...;
- using Stream actualXmlStream = ...;
- bool isEqual = Xml.Compare(
-     actualXmlStream, expectedXmlStream, out string userMessage, nodesToIgnore: Array.Empty<string>());

+ AssertXml.Equal(expectedXml, actualXml);
```

Any nodes that should be ignored can be configured by passing additional options:

```diff
- using Codit.Testing.OutputComparison;
+ using Arcus.Testing;

- bool isEqual = Xml.Compare(..., new[] { "ignore-this-node" });
+ AssertXml.Equal(..., options =>
+ {
+     options.AddIgnoreNode("ignore-this-node");
+ });
```

🔗 See the [feature documentation](../02-Features/assertion.md) for more info on the `AssertXml`.

### JSON
You can use `AssertJson` like any other assertion method. Instead of returning a boolean and a message, it throws an exception with a detailed report in case of a difference.

```diff
- using Codit.Testing.OutputComparison;
+ using Arcus.Testing;

string expectedJson = ...;
string actualJson = ...;

- using Stream expectedJsonStream = ...;
- using Stream actualJsonStream = ...;
- bool isEqual = Json.Compare(
-    actualJsonStream, expectedJsonStream, out string userMessage, nodesToIgnore: Array.Empty<string>());

+ AssertJson.Equal(expectedJson, actualJson);
```

Any nodes that should be ignored can be configured by passing additional options:

```diff
- using Codit.Testing.OutputComparison;
+ using Arcus.Testing;

- bool isEqual = Json.Compare(..., new[] { "ignore-this-node" });
+ AssertJson.Equal(..., options =>
+ {
+     options.AddIgnoreNode("ignore-this-node");
+ });
```

🔗 See the [feature documentation](../02-Features/assertion.md) for more info on the `AssertJson` and the available options.

### CSV
You can use `AssertCsv` like any other assertion method. Instead of returning a boolean and a message, it throws an exception with a detailed report in case of a difference. The Arcus variant also allows for raw CSV to be compared, without the need for you to create a dedicated DTO serialization model before the comparison can happen. It is advised to use your custom domain comparison if you need custom comparison of rows.

#### Use order of rows & columns

```diff
- using Codit.Testing.OutputComparison;
+ using Arcus.Testing;

string expectedCSv = ...;
string actualCsv = ...;

- using Stream expectedCsvStream = ...;
- using Stream actualCsvStream = ...;
- bool isEqual = Csv.Compare(actualCsvStream, expectedCsvStream, out string userMessage);

+ AssertCsv.Equal(expectedCsv, actualCsv);
```

#### Ignore order of rows & columns

```diff
- using Codit.Testing.OutputComparison;
+ using Arcus.Testing;

string expectedCSv = ...;
string actualCsv = ...;

- using Stream expectedCsvStream = ...;
- using Stream actualCsvStream = ...;
- bool isEqual = Csv.CompareWithoutOrdering<MyCsvRowModel>(
-       actualCsvStream, expectedCsvStream, out string userMessage);

+ AssertCsv.Equal(expectedCsv, actualCsv, options =>
+ {
+     options.ColumnOrder = AssertCsvOrder.Ignore;
+     options.RowOrder = AssertCsvOrder.Ignore;
+ });
```

🔗 See the [feature documentation](../02-Features/assertion.md) for more info on the `AssertCsv` and the available options.

### XSLT
Transforming XML-XML to XML-JSON now also happens in a test asserted manner. It does not use the file name anymore and a 'convention by configuration' file structure, but needs the actual contents. You can use the test-friendly `ResourceDirectory` in the `Arcus.Testing.Core` package to load the files.

⚠️ **IMPORTANT** that you [add your XSLT files as links](https://jeremybytes.blogspot.com/2019/07/linking-files-in-visual-studio.html) to the test project, that way any changes to the XSLT in the implementation project will be automatically copied to the XSLT transformation used in the tests. 

Here's how XML-XML now works:

```diff
- using.Codit.Testing.Xslt;
+ using Arcus.Testing;

- // _input.xml file is loaded implicitly.
- // _expected.xml file is loaded implicitly.
- bool successfullyTransformed = XsltHelper.TestXslt(
-     "transformer.xslt",
-     out string userMessage,
-     xsltArgumentList: null,
-     MessageOutputType.Xml);

+ string inputXml = ... // Load _input.xml file explicitly.
+ string transformerXslt = ... // Load transformer.xslt file's contents here.

+ string actualXml = AssertXslt.TransformToXml(transformerXslt, inputXml);
+ // Use `AssertXml.Equal` to determine the difference next.
```

> 💡You can use the test-friendly `AssertXml/Xslt.Load` functionality to load raw contents to their respectful XSLT/XML document. Upon failure, a load exception with a detailed description will be reported to the tester.

> 💡 You can use the test-friendly [`ResourceDirectory`](../02-Features/core.md) functionality in the `Arcus.Testing.Core` package to load raw file contents. Upon failure, a not-found exception with a detailed description will be reported to the tester.

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

+ string actualJson = AssertXslt.TransformToJson(transformerXslt, inputXml);
+ // Use `AssertJson.Equal` to determine the difference next.
```

> 💡You can use the test-friendly `AssertXml/Json/Xslt.Load` functionality to load raw contents to their respectful XSLT/XML/JSON document. Upon failure, a load exception with a detailed description will be reported to the tester.

> 💡 You can use the test-friendly [`ResourceDirectory`](../02-Features/core.md) functionality in the `Arcus.Testing.Core` package to load raw file contents. Upon failure, a not-found exception with a detailed description will be reported to the tester.

Here's how XML-CSV now works:

```diff
- using Codit.Testing.Xslt;
+ using Arcus.Testing;

- // _input.xml file is loaded implicitly.
- // _expected.csv file is loaded implicitly.
- bool success = XsltHelper.TestXslt(
-     "transformer.xslt",
-     out string message,
-     xsltArgumentsList: null,
-     MessageOutputType.Csv);

+ string inputXml = ... // Load _input.xml file explicitly.
+ string transformerXslt = ... // Load transformer.xslt file's contents here.

+ string actualCsv = AssertXslt.TransformToCsv(transformerXslt, inputXml);
+ // Use `AssertCsv.Equal` to determine the difference next.
```

> 💡You can use the test-friendly `AssertXml/Csv/Xslt.Load` functionality to load raw contents to their respectful XSLT/XML/CSV document. Upon failure, a load exception with a detailed description will be reported to the tester.

> 💡 You can use the test-friendly [`ResourceDirectory`](../02-Features/core.md) functionality in the `Arcus.Testing.Core` package to load raw file contents. Upon failure, a not-found exception with a detailed description will be reported to the tester.


🔗 See the [feature documentation](../02-Features/assertion.md) for more info on the `AssertXslt`.

#### Sequential transformations
The original Testing Framework had a `TestXsltSequential` exposed functionality to run multiple XSLT transformation in sequence (output of one transformation becomes input of another). This was using separate input types for file names. Since this is made explicit in the `Arcus.Testing` packages, you can load those files yourself using the `ResourceDirectory` type and do the sequential transformation in a more explicit manner:

```diff
- using Codit.Testing.Xslt;
+ using Arcus.Testing;

- // XlstAndArgumentList[] xsltFileNames = ...
- bool success = XsltHelper.TestXsltSequential(xsltFileNames, out string message);

+ string[] xsltFileContents = ...
+ string inputContent = ...
+ string output = xsltFileContents.Aggregate(inputContent, (xslt, xml) => AssertXslt.TransformToXml(xslt, xml));
```