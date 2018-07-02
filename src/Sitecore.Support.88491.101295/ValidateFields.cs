using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.ExperienceEditor.Speak.Server.Contexts;
using Sitecore.ExperienceEditor.Speak.Server.Responses;
using Sitecore.Globalization;
using Sitecore.Pipelines.Save;

namespace Sitecore.Support.ExperienceEditor.Speak.Ribbon.Requests.SaveItem
{
  public class ValidateFields : Sitecore.Support.ExperienceEditor.Speak.Server.Requests.PipelineProcessorRequest<PageContext>
  {
    public override PipelineProcessorResponseValue ProcessRequest()
    {
      PipelineProcessorResponseValue pipelineProcessorResponseValue = new PipelineProcessorResponseValue();
      SaveArgs.SaveItem saveItem = base.RequestContext.GetSaveArgs().Items[0];
      Item item = base.RequestContext.Item.Database.GetItem(saveItem.ID, saveItem.Language);
      if (item != null && !item.Paths.IsMasterPart && !StandardValuesManager.IsStandardValuesHolder(item))
      {
        SaveArgs.SaveField[] fields = saveItem.Fields;
        foreach (SaveArgs.SaveField saveField in fields)
        {
          string fieldRegexValidationError = FieldUtil.GetFieldRegexValidationError(item.Fields[saveField.ID], saveField.Value);
          if (!string.IsNullOrEmpty(fieldRegexValidationError))
          {
            pipelineProcessorResponseValue.AbortMessage = Translate.Text(fieldRegexValidationError);
            return pipelineProcessorResponseValue;
          }
        }
      }
      return pipelineProcessorResponseValue;
    }
  }
}