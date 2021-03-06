using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;

using Newtonsoft.Json;

using PicklesDoc.Pickles.Parser;

using Feature = PicklesDoc.Pickles.Parser.JsonResult.Feature;

namespace PicklesDoc.Pickles.TestFrameworks
{
  public class CucumberJsonSingleResults : ITestResults
  {
    private readonly List<Feature> resultsDocument;

    public CucumberJsonSingleResults(FileInfoBase configuration)
    {
      this.resultsDocument = this.ReadResultsFile(configuration);
    }

    private List<Feature> ReadResultsFile(FileInfoBase testResultsFile)
    {
      List<Feature> result;
      using (var stream = testResultsFile.OpenRead())
      {
        using (var reader = new StreamReader(stream))
        {
          result = JsonConvert.DeserializeObject<List<Feature>>(reader.ReadToEnd());
        }
      }
      return result;
    }

    #region ITestResults Members

    public TestResult GetFeatureResult(Parser.Feature feature)
    {
      var cucumberFeature = this.GetFeatureElement(feature);
      return this.GetResultFromFeature(cucumberFeature);

    }

    public TestResult GetScenarioOutlineResult(ScenarioOutline scenarioOutline)
    {
      //Not applicable
      return new TestResult();
    }

    public TestResult GetScenarioResult(Scenario scenario)
    {
      Parser.JsonResult.Element cucumberScenario  = null;
      var cucumberFeature = this.GetFeatureElement(scenario.Feature);
      if(cucumberFeature != null)
        cucumberScenario = cucumberFeature.elements.FirstOrDefault(x => x.name == scenario.Name);
      return this.GetResultFromScenario(cucumberScenario);

    }

    #endregion

    private Feature GetFeatureElement(Parser.Feature feature)
    {
      return this.resultsDocument.FirstOrDefault(x => x.name == feature.Name);
    }

    private TestResult GetResultFromScenario(Parser.JsonResult.Element cucumberScenario)
    {
      if (cucumberScenario == null) return TestResult.Inconclusive;

      bool wasSuccessful = CheckScenarioStatus(cucumberScenario);

      return wasSuccessful ? TestResult.Passed : TestResult.Failed;
    }

    private static bool CheckScenarioStatus(Parser.JsonResult.Element cucumberScenario)
    {
      return cucumberScenario.steps.All(x => x.result.status == "passed");
    }

    private TestResult GetResultFromFeature(Feature cucumberFeature)
    {
      if (cucumberFeature == null || cucumberFeature.elements == null) return TestResult.Inconclusive;

      bool wasSuccessful = cucumberFeature.elements.All(CheckScenarioStatus);

      return wasSuccessful ? TestResult.Passed : TestResult.Failed;
    }

  }
}