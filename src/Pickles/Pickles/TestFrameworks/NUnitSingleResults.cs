using System;
using System.IO.Abstractions;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

using PicklesDoc.Pickles.Parser;

namespace PicklesDoc.Pickles.TestFrameworks
{
  public class NUnitSingleResults : ITestResults
  {
    internal NUnitExampleSignatureBuilder ExampleSignatureBuilder { get; set; }

    private readonly XDocument resultsDocument;

    public NUnitSingleResults(FileInfoBase testResultsFile)
    {
      this.resultsDocument = this.ReadResultsFile(testResultsFile);
    }

    #region ITestResults Members

    public TestResult GetFeatureResult(Feature feature)
    {
      var featureElement = this.GetFeatureElement(feature);

      if (featureElement == null)
      {
        return TestResult.Inconclusive;
      }
      var results = featureElement.Descendants("test-case")
        .Select(GetResultFromElement);

      return results.Merge();
    }

    public TestResult GetScenarioResult(Scenario scenario)
    {
      XElement featureElement = this.GetFeatureElement(scenario.Feature);
      XElement scenarioElement = null;
      if (featureElement != null)
      {
        scenarioElement = featureElement
          .Descendants("test-case")
          .Where(x => x.Attribute("description") != null)
          .FirstOrDefault(x => x.Attribute("description").Value == scenario.Name);
      }
      return this.GetResultFromElement(scenarioElement);
    }

    public TestResult GetScenarioOutlineResult(ScenarioOutline scenarioOutline)
    {
      XElement featureElement = this.GetFeatureElement(scenarioOutline.Feature);
      XElement scenarioOutlineElement = null;
      if (featureElement != null)
      {
        scenarioOutlineElement = this.GetFeatureElement(scenarioOutline.Feature)
          .Descendants("test-suite")
          .Where(x => x.Attribute("description") != null)
          .FirstOrDefault(x => x.Attribute("description").Value == scenarioOutline.Name);
      }
      return this.GetResultFromElement(scenarioOutlineElement);
    }

    #endregion

    private XDocument ReadResultsFile(FileInfoBase testResultsFile)
    {
      XDocument document;
      using (var stream = testResultsFile.OpenRead())
      {
        XmlReader xmlReader = XmlReader.Create(stream);
        document = XDocument.Load(xmlReader);
        stream.Close();
      }
      return document;
    }

    private XElement GetFeatureElement(Feature feature)
    {
      return this.resultsDocument
        .Descendants("test-suite")
        .Where(x => x.Attribute("description") != null)
        .FirstOrDefault(x => x.Attribute("description").Value == feature.Name);
    }

    private TestResult GetResultFromElement(XElement element)
    {
      if (element == null)
      {
        return TestResult.Inconclusive;
      }
      else if (IsAttributeSetToValue(element, "result", "Ignored"))
      {
        return TestResult.Inconclusive;
      }
      else if (IsAttributeSetToValue(element, "result", "Inconclusive"))
      {
        return TestResult.Inconclusive;
      }
      else if (IsAttributeSetToValue(element, "result", "Failure"))
      {
        return TestResult.Failed;
      }
      else if (IsAttributeSetToValue(element, "result", "Success"))
      {
        return TestResult.Passed;
      }
      else
      {
        bool wasExecuted = IsAttributeSetToValue(element, "executed", "true");

        if (!wasExecuted) return TestResult.Inconclusive;

        bool wasSuccessful = IsAttributeSetToValue(element, "success", "true");

        return wasSuccessful ? TestResult.Passed : TestResult.Failed;
      }
    }

    private static bool IsAttributeSetToValue(XElement element, string attributeName, string expectedValue)
    {
      return element.Attribute(attributeName) != null
               ? string.Equals(
                 element.Attribute(attributeName).Value, 
                 expectedValue, 
                 StringComparison.InvariantCultureIgnoreCase)
               : false;
    }

    public TestResult GetExampleResult(ScenarioOutline scenarioOutline, string[] row)
    {
      XElement featureElement = this.GetFeatureElement(scenarioOutline.Feature);
      XElement examplesElement = null;
      if (featureElement != null)
      {
        var signatureBuilder = this.ExampleSignatureBuilder;

        if (signatureBuilder == null)
        {
          throw new InvalidOperationException("You need to set the ExampleSignatureBuilder before using GetExampleResult.");
        }

        Regex exampleSignature = signatureBuilder.Build(scenarioOutline, row);
        examplesElement = featureElement
          .Descendants("test-suite")
          .Where(x => x.Attribute("description") != null)
          .FirstOrDefault(x => x.Attribute("description").Value == scenarioOutline.Name)
          .Descendants("test-case")
          .Where(x => x.Attribute("name") != null)
          .FirstOrDefault(x => exampleSignature.IsMatch(x.Attribute("name").Value.ToLowerInvariant()));
      }
      return this.GetResultFromElement(examplesElement);
    }
  }
}