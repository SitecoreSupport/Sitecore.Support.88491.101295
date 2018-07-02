using Sitecore.Collections;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Data.Validators;
using Sitecore.Diagnostics;
using Sitecore.ExperienceEditor.Speak.Server.Contexts;
using Sitecore.ExperienceEditor.Speak.Server.Requests;
using Sitecore.ExperienceEditor.Speak.Server.Responses;
using Sitecore.ExperienceEditor.Switchers;
using Sitecore.ExperienceEditor.Utils;
using Sitecore.Globalization;
using System.Collections.Generic;
using System.Text;

namespace Sitecore.ExperienceEditor.Speak.Ribbon.Requests.SaveItem
{
  public class Validators : PipelineProcessorRequest<PageContext>
  {
    public override PipelineProcessorResponseValue ProcessRequest()
    {
      Item item = base.RequestContext.Item;
      PipelineProcessorResponseValue pipelineProcessorResponseValue = new PipelineProcessorResponseValue();
      if (!Sitecore.Configuration.Settings.WebEdit.ValidationEnabled)
      {
        return pipelineProcessorResponseValue;
      }
      string validatorsKey = base.RequestContext.ValidatorsKey;
      if (string.IsNullOrEmpty(validatorsKey))
      {
        return pipelineProcessorResponseValue;
      }
      Pair<ValidatorResult, BaseValidator> strongestResult = default(Pair<ValidatorResult, BaseValidator>);
      using (new ClientDatabaseSwitcher(item.Database))
      {
        ValidatorCollection validators = GetValidators(item) as ValidatorCollection;
        ValidatorOptions options = new ValidatorOptions(true);
        ValidatorManager.Validate(validators, options);
        strongestResult = ValidatorManager.GetStrongestResult(validators, true, true);
      }
      ValidatorResult part = strongestResult.Part1;
      BaseValidator part2 = strongestResult.Part2;
      if (part2 != null && part2.IsEvaluating)
      {
        pipelineProcessorResponseValue.AbortMessage = Translate.Text("The fields in this item have not been validated.\n\nWait until validation has been completed and then save your changes.");
        return pipelineProcessorResponseValue;
      }
      switch (part)
      {
        case ValidatorResult.CriticalError:
          {
            string text2 = Translate.Text("Some of the fields in this item contain critical errors.\n\nAre you sure you want to save this item?");
            if (part2 != null)
            {
              text2 += GetValidationErrorDetails(part2);
            }
            pipelineProcessorResponseValue.ConfirmMessage = Translate.Text(text2);
            return pipelineProcessorResponseValue;
          }
        case ValidatorResult.FatalError:
          {
            string text = Translate.Text("Some of the fields in this item contain fatal errors.\n\nYou must resolve these errors before you can save this item.");
            if (part2 != null)
            {
              text += GetValidationErrorDetails(part2);
            }
            pipelineProcessorResponseValue.AbortMessage = Translate.Text(text);
            return pipelineProcessorResponseValue;
          }
        default:
          return pipelineProcessorResponseValue;
      }
    }

    protected virtual IEnumerable<BaseValidator> GetValidators(Item item)
    {
      SafeDictionary<FieldDescriptor, string> controlsToValidate = base.RequestContext.GetControlsToValidate();
      ValidatorsMode validatorsMode;
      ValidatorCollection validators = PipelineUtil.GetValidators(item, controlsToValidate, out validatorsMode);
      validators.Key = base.RequestContext.ValidatorsKey;
      return validators;
    }

    private static string GetValidationErrorDetails(BaseValidator failedValidator)
    {
      Assert.ArgumentNotNull(failedValidator, "failedValidator");
      if (failedValidator.IsValid)
      {
        return string.Empty;
      }
      StringBuilder stringBuilder = new StringBuilder();
      if (!string.IsNullOrEmpty(failedValidator.Text))
      {
        stringBuilder.AppendLine("\n\n" + failedValidator.Text);
      }
      foreach (string error in failedValidator.Errors)
      {
        if (!string.IsNullOrEmpty(error))
        {
          stringBuilder.AppendLine(" - " + error);
        }
      }
      return stringBuilder.ToString();
    }
  }
}