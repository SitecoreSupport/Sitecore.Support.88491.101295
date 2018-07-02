using Sitecore;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.ExperienceEditor.Speak.Server.Contexts;
using Sitecore.ExperienceEditor.Speak.Server.Requests;
using Sitecore.ExperienceEditor.Speak.Server.Responses;
using Sitecore.Globalization;
using System;
using System.Linq;

namespace Sitecore.ExperienceEditor.Speak.Ribbon.Requests.SaveItem
{
  public class CheckBaseTemplateFieldChange : PipelineProcessorRequest<PageContext>
  {
    public override PipelineProcessorResponseValue ProcessRequest()
    {
      PipelineProcessorResponseValue pipelineProcessorResponseValue = new PipelineProcessorResponseValue();
      Item item = base.RequestContext.Item.Database.GetItem(base.RequestContext.ItemId, Language.Parse(base.RequestContext.Language), Sitecore.Data.Version.Parse(base.RequestContext.Version));
      if (item != null && item.Database.Engines.TemplateEngine.IsTemplate(item))
      {
        Field field2 = item.Fields.FirstOrDefault((Field field) => field.ID == FieldIDs.BaseTemplate);
        if (field2 != null && AreBaseTemplatesRemoved(((BaseItem)item)[FieldIDs.BaseTemplate], field2.Value))
        {
          pipelineProcessorResponseValue.ConfirmMessage = Translate.Text("You are about to remove one or more base templates from the current template.\n\nWhen you remove a base template, Sitecore updates all the items based on the current template and clears any field values in these items that are associated with the fields in the base template that you removed. These field values cannot be restored once you have removed them.\n\nDo you want to proceed?");
        }
        return pipelineProcessorResponseValue;
      }
      return pipelineProcessorResponseValue;
    }

    internal bool AreBaseTemplatesRemoved(string baseTemplateFieldValue, string newFieldValue)
    {
      Assert.ArgumentNotNull(baseTemplateFieldValue, "baseTemplateFieldValue");
      Assert.ArgumentNotNull(newFieldValue, "newFieldValue");
      string[] first = newFieldValue.Split(new char[1]
      {
        '|'
      }, StringSplitOptions.RemoveEmptyEntries);
      string[] array = baseTemplateFieldValue.Split(new char[1]
      {
        '|'
      }, StringSplitOptions.RemoveEmptyEntries);
      return first.Intersect(array, StringComparer.InvariantCultureIgnoreCase).Count() < array.Length;
    }
  }
}