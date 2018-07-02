using Newtonsoft.Json;
using Sitecore.Collections;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Data.Validators;
using Sitecore.Diagnostics;
using Sitecore.ExperienceEditor.Speak.Server.Contexts;
using Sitecore.ExperienceEditor.Utils;
using Sitecore.Pipelines.Save;
using Sitecore.Shell.Applications.WebEdit.Commands;
using System.Collections.Generic;

namespace Sitecore.ExperienceEditor.Speak.Server.Contexts
{
  public class PageContext : ItemContext
  {
    [JsonProperty("scLayout")]
    public string LayoutSource
    {
      get;
      set;
    }

    [JsonProperty("scValidatorsKey")]
    public string ValidatorsKey
    {
      get;
      set;
    }

    [JsonProperty("scFieldValues")]
    public Dictionary<string, string> FieldValues
    {
      get;
      set;
    }

    public SaveArgs GetSaveArgs()
    {
      IEnumerable<PageEditorField> fields = WebUtility.GetFields(base.Item.Database, FieldValues);
      string empty = string.Empty;
      string layoutSource = LayoutSource;
      SaveArgs saveArgs = PipelineUtil.GenerateSaveArgs(base.Item, fields, empty, layoutSource, string.Empty, WebUtility.GetCurrentLayoutFieldId().ToString());
      saveArgs.HasSheerUI = false;
      ParseXml parseXml = new ParseXml();
      parseXml.Process(saveArgs);
      return saveArgs;
    }

    public SafeDictionary<FieldDescriptor, string> GetControlsToValidate()
    {
      Item item = base.Item;
      Assert.IsNotNull(item, "The item is null.");
      IEnumerable<PageEditorField> fields = WebUtility.GetFields(item.Database, FieldValues);
      SafeDictionary<FieldDescriptor, string> safeDictionary = new SafeDictionary<FieldDescriptor, string>();
      foreach (PageEditorField item3 in fields)
      {
        Item item2 = (item.ID == item3.ItemID) ? item : item.Database.GetItem(item3.ItemID);
        Field field = item.Fields[item3.FieldID];
        string value = WebUtility.HandleFieldValue(item3.Value, field.TypeKey);
        FieldDescriptor key = new FieldDescriptor(item2.Uri, field.ID, value, false);
        string obj = item3.ControlId ?? string.Empty;
        string text2 = safeDictionary[key] = obj;
        if (!string.IsNullOrEmpty(text2))
        {
          RuntimeValidationValues.Current[text2] = value;
        }
      }
      return safeDictionary;
    }
  }
}