using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.ExperienceEditor.Speak.Server.Contexts;
using Sitecore.ExperienceEditor.Speak.Server.Requests;
using Sitecore.ExperienceEditor.Speak.Server.Responses;
using Sitecore.Globalization;
using Sitecore.Pipelines.Save;

namespace Sitecore.ExperienceEditor.Speak.Ribbon.Requests.SaveItem
{
  public class ValidateFields : PipelineProcessorRequest<PageContext>
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
          Field field = item.Fields[saveField.ID];
          string fieldRegexValidationError = FieldUtil.GetFieldRegexValidationError(field, saveField.Value);
          if (!string.IsNullOrEmpty(fieldRegexValidationError))
          {
            pipelineProcessorResponseValue.AbortMessage = Translate.Text(fieldRegexValidationError);
            break;
          }
        }
        return pipelineProcessorResponseValue;
      }
      return pipelineProcessorResponseValue;
    }
  }
}