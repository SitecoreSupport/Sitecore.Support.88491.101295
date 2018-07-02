using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.ExperienceEditor.Speak.Server.Contexts;
using Sitecore.ExperienceEditor.Speak.Server.Responses;
using Sitecore.Globalization;
using Sitecore.Links;
using System.Text;

namespace Sitecore.Support.ExperienceEditor.Speak.Ribbon.Requests.SaveItem
{
  public class CheckLinks : Sitecore.Support.ExperienceEditor.Speak.Server.Requests.PipelineProcessorRequest<PageContext>
  {
    public override PipelineProcessorResponseValue ProcessRequest()
    {
      PipelineProcessorResponseValue pipelineProcessorResponseValue = new PipelineProcessorResponseValue();
      Item item = base.RequestContext.Item.Database.GetItem(base.RequestContext.ItemId, Language.Parse(base.RequestContext.Language), Version.Parse(base.RequestContext.Version));
      if (item != null)
      {
        ItemLink[] brokenLinks = item.Links.GetBrokenLinks(false);
        if (brokenLinks.Length == 0)
        {
          return pipelineProcessorResponseValue;
        }
        StringBuilder stringBuilder = new StringBuilder(Translate.Text("The item \"{0}\" contains broken links in these fields:\n\n", item.DisplayName));
        bool flag = false;
        ItemLink[] array = brokenLinks;
        foreach (ItemLink itemLink in array)
        {
          if (!itemLink.SourceFieldID.IsNull)
          {
            Field field = item.Fields[itemLink.SourceFieldID];
            stringBuilder.Append(" - ");
            stringBuilder.Append((field != null) ? field.DisplayName : Translate.Text("[Unknown field: {0}]", itemLink.SourceFieldID.ToString()));
            if (!string.IsNullOrEmpty(itemLink.TargetPath) && !ID.IsID(itemLink.TargetPath))
            {
              stringBuilder.Append(": \"");
              stringBuilder.Append(itemLink.TargetPath);
              stringBuilder.Append("\"");
            }
            stringBuilder.Append("\n");
          }
          else
          {
            flag = true;
          }
        }
        if (flag)
        {
          stringBuilder.Append("\n");
          stringBuilder.Append(Translate.Text("The template or branch for this item is missing."));
        }
        stringBuilder.Append("\n");
        stringBuilder.Append(Translate.Text("Do you want to save anyway?"));
        pipelineProcessorResponseValue.ConfirmMessage = stringBuilder.ToString();
      }
      return pipelineProcessorResponseValue;
    }
  }
}