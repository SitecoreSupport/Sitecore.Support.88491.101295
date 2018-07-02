using Sitecore;
using Sitecore.Collections;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Data.Validators;
using Sitecore.Diagnostics;
using Sitecore.ExperienceEditor;
using Sitecore.ExperienceEditor.Exceptions;
using Sitecore.ExperienceEditor.Utils;
using Sitecore.Globalization;
using Sitecore.Links;
using Sitecore.Pipelines;
using Sitecore.Shell.Applications.WebEdit.Commands;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Sites;
using Sitecore.Text;
using Sitecore.Web;
using Sitecore.Web.Configuration;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Xml;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI;
using System.Xml;

namespace Sitecore.ExperienceEditor.Utils
{
  public static class WebUtility
  {
    private static bool? isGetLayoutSourceFieldsExists;

    public static bool IsSublayoutInsertingMode => !string.IsNullOrEmpty(WebUtil.GetQueryString("sc_ruid"));

    public static string ClientLanguage => WebUtil.GetCookieValue("shell", "lang", Context.Language.Name);

    public static SiteInfo GetCurrentSiteInfo()
    {
      Assert.IsNotNull(Context.Request, "request");
      string name = string.IsNullOrEmpty(Context.Request.QueryString["sc_pagesite"]) ? Sitecore.Configuration.Settings.Preview.DefaultSite : Context.Request.QueryString["sc_pagesite"];
      return SiteContextFactory.GetSiteInfo(name);
    }

    public static bool IsLayoutPresetApplied()
    {
      if (IsSublayoutInsertingMode)
      {
        return false;
      }
      if (string.IsNullOrEmpty(Context.PageDesigner.PageDesignerHandle))
      {
        return false;
      }
      if (string.IsNullOrEmpty(WebUtil.GetSessionString(Context.PageDesigner.PageDesignerHandle)))
      {
        return false;
      }
      if (string.IsNullOrEmpty(WebUtil.GetSessionString(Context.PageDesigner.PageDesignerHandle + "_SAFE")))
      {
        return false;
      }
      return true;
    }

    public static string GetDevice(UrlString url)
    {
      Assert.ArgumentNotNull(url, "url");
      string result = string.Empty;
      DeviceItem device = Context.Device;
      if (device != null)
      {
        url["dev"] = device.ID.ToString();
        result = device.ID.ToShortID().ToString();
      }
      return Assert.ResultNotNull(result);
    }

    public static void RenderLoadingIndicator(HtmlTextWriter output)
    {
      System.Web.UI.Page page = new System.Web.UI.Page();
      System.Web.UI.Control control = page.LoadControl("~/sitecore/shell/client/Sitecore/ExperienceEditor/PageEditbar/LoadingIndicator.ascx");
      control.RenderControl(output);
    }

    public static void RenderLayout(Item item, HtmlTextWriter output, string siteName, string deviceId)
    {
      string layout = GetLayout(item);
      layout = FixEmptyPlaceholders(layout);
      layout = ConvertToJson(layout);
      output.Write("<input id=\"scLayout\" type=\"hidden\" value='" + layout + "' />");
      output.Write("<input id=\"scDeviceID\" type=\"hidden\" value=\"" + StringUtil.EscapeQuote(deviceId) + "\" />");
      output.Write("<input id=\"scItemID\" type=\"hidden\" value=\"" + StringUtil.EscapeQuote(item.ID.ToShortID().ToString()) + "\" />");
      output.Write("<input id=\"scLanguage\" type=\"hidden\" value=\"" + StringUtil.EscapeQuote(item.Language.Name) + "\" />");
      output.Write("<input id=\"scSite\" type=\"hidden\" value=\"" + StringUtil.EscapeQuote(siteName) + "\" />");
    }

    private static string FixEmptyPlaceholders(string layout)
    {
      XmlDocument xmlDocument = new XmlDocument();
      xmlDocument.LoadXml(layout);
      XmlNodeList xmlNodeList = xmlDocument.SelectNodes("//r[@ph='']");
      if (xmlNodeList != null)
      {
        foreach (XmlNode item2 in xmlNodeList)
        {
          string value = item2.Attributes["id"].Value;
          Item item = Context.Database.GetItem(new ID(value));
          string value2 = item.Fields[Sitecore.ExperienceEditor.Constants.FieldNames.Placeholder].Value;
          if (!string.IsNullOrEmpty(value2))
          {
            item2.Attributes["ph"].Value = value2;
          }
        }
      }
      layout = xmlDocument.OuterXml;
      return layout;
    }

    public static string ConvertToJson(string layout)
    {
      Assert.ArgumentNotNull(layout, "layout");
      string result = WebEditUtil.ConvertXMLLayoutToJSON(layout);
      return Assert.ResultNotNull(result);
    }

    public static string GetLayout(Item item)
    {
      Assert.ArgumentNotNull(item, "item");
      LayoutField layoutField = new LayoutField(item);
      return GetLayout(layoutField);
    }

    public static string GetLayout(Field field)
    {
      Assert.ArgumentNotNull(field, "field");
      LayoutField layoutField = new LayoutField(field);
      return GetLayout(layoutField);
    }

    public static string GetLayout(LayoutField layoutField)
    {
      Assert.ArgumentNotNull(layoutField, "field");
      string result = layoutField.Value;
      if (!Context.PageDesigner.IsDesigning)
      {
        return Assert.ResultNotNull(result);
      }
      string pageDesignerHandle = Context.PageDesigner.PageDesignerHandle;
      if (string.IsNullOrEmpty(pageDesignerHandle))
      {
        return Assert.ResultNotNull(result);
      }
      string sessionString = WebUtil.GetSessionString(pageDesignerHandle);
      if (!string.IsNullOrEmpty(sessionString))
      {
        result = sessionString;
      }
      return Assert.ResultNotNull(result);
    }

    public static Dictionary<string, string> ConvertFormKeysToDictionary(NameValueCollection form)
    {
      Assert.ArgumentNotNull(form, "dictionaryForm");
      return (from string key in form.Keys
              where !string.IsNullOrEmpty(key)
              select key).ToDictionary((string key) => key, (string key) => form[key]);
    }

    public static IEnumerable<PageEditorField> GetFields(Database database, Dictionary<string, string> dictionaryForm)
    {
      Assert.ArgumentNotNull(dictionaryForm, "dictionaryForm");
      List<PageEditorField> list = new List<PageEditorField>();
      foreach (string key in dictionaryForm.Keys)
      {
        if (key.StartsWith("fld_", StringComparison.InvariantCulture) || key.StartsWith("flds_", StringComparison.InvariantCulture))
        {
          string text = key;
          string text2 = dictionaryForm[key];
          int num = text.IndexOf('$');
          if (num >= 0)
          {
            text = StringUtil.Left(text, num);
          }
          string[] array = text.Split('_');
          ID iD = ShortID.DecodeID(array[1]);
          ID fieldID = ShortID.DecodeID(array[2]);
          Language language = Language.Parse(array[3]);
          Sitecore.Data.Version version = Sitecore.Data.Version.Parse(array[4]);
          string revision = array[5];
          Item item = database.GetItem(iD);
          if (item != null)
          {
            Field field = item.Fields[fieldID];
            if (key.StartsWith("flds_", StringComparison.InvariantCulture))
            {
              text2 = (string)WebUtil.GetSessionValue(text2);
              if (string.IsNullOrEmpty(text2))
              {
                text2 = field.Value;
              }
            }
            switch (field.TypeKey)
            {
              case "html":
              case "rich text":
                text2 = text2.TrimEnd(' ');
                break;
              case "text":
                text2 = StringUtil.RemoveTags(text2);
                break;
              case "multi-line text":
              case "memo":
                {
                  Regex regex = new Regex("<br.*/*>", RegexOptions.IgnoreCase);
                  text2 = regex.Replace(text2, "\r\n");
                  text2 = StringUtil.RemoveTags(text2);
                  break;
                }
            }
            PageEditorField pageEditorField = new PageEditorField();
            pageEditorField.ControlId = text;
            pageEditorField.FieldID = fieldID;
            pageEditorField.ItemID = iD;
            pageEditorField.Language = language;
            pageEditorField.Revision = revision;
            pageEditorField.Value = text2;
            pageEditorField.Version = version;
            PageEditorField item2 = pageEditorField;
            list.Add(item2);
          }
        }
      }
      return list;
    }

    public static IEnumerable<PageEditorField> GetFields(Item item)
    {
      List<PageEditorField> list = new List<PageEditorField>();
      foreach (Field field in item.Fields)
      {
        PageEditorField pageEditorField = new PageEditorField();
        pageEditorField.ControlId = null;
        pageEditorField.FieldID = field.ID;
        pageEditorField.ItemID = field.Item.ID;
        pageEditorField.Language = field.Language;
        pageEditorField.Revision = ((BaseItem)item)[FieldIDs.Revision];
        pageEditorField.Value = field.Value;
        pageEditorField.Version = item.Version;
        PageEditorField item2 = pageEditorField;
        list.Add(item2);
      }
      return list;
    }

    public static Packet CreatePacket(Database database, IEnumerable<PageEditorField> fields, out SafeDictionary<FieldDescriptor, string> controlsToValidate)
    {
      Assert.ArgumentNotNull(fields, "fields");
      Packet packet = new Packet();
      controlsToValidate = new SafeDictionary<FieldDescriptor, string>();
      foreach (PageEditorField field in fields)
      {
        FieldDescriptor fieldDescriptor = AddField(database, packet, field);
        if (fieldDescriptor != null)
        {
          string text = field.ControlId ?? string.Empty;
          controlsToValidate[fieldDescriptor] = text;
          if (!string.IsNullOrEmpty(text))
          {
            RuntimeValidationValues.Current[text] = fieldDescriptor.Value;
          }
        }
      }
      return packet;
    }

    public static FieldDescriptor AddField(Database database, Packet packet, PageEditorField pageEditorField)
    {
      Assert.ArgumentNotNull(packet, "packet");
      Assert.ArgumentNotNull(pageEditorField, "pageEditorField");
      Item item = database.GetItem(pageEditorField.ItemID, pageEditorField.Language, pageEditorField.Version);
      if (item == null)
      {
        return null;
      }
      Field field = item.Fields[pageEditorField.FieldID];
      string text = HandleFieldValue(pageEditorField.Value, field.TypeKey);
      string fieldValidationErrorMessage = GetFieldValidationErrorMessage(field, text);
      if (fieldValidationErrorMessage != string.Empty)
      {
        throw new FieldValidationException(fieldValidationErrorMessage, field);
      }
      if (text == field.Value)
      {
        string fieldRegexValidationError = FieldUtil.GetFieldRegexValidationError(field, text);
        if (!string.IsNullOrEmpty(fieldRegexValidationError))
        {
          if (!item.Paths.IsMasterPart && !StandardValuesManager.IsStandardValuesHolder(item))
          {
            throw new FieldValidationException(fieldRegexValidationError, field);
          }
          return new FieldDescriptor(item.Uri, field.ID, text, field.ContainsStandardValue);
        }
        return new FieldDescriptor(item.Uri, field.ID, text, field.ContainsStandardValue);
      }
      XmlNode xmlNode = packet.XmlDocument.SelectSingleNode("/*/field[@itemid='" + pageEditorField.ItemID + "' and @language='" + pageEditorField.Language + "' and @version='" + pageEditorField.Version + "' and @fieldid='" + pageEditorField.FieldID + "']");
      if (xmlNode != null)
      {
        Item item2 = database.GetItem(pageEditorField.ItemID, pageEditorField.Language, pageEditorField.Version);
        if (item2 == null)
        {
          return null;
        }
        if (text != ((BaseItem)item2)[pageEditorField.FieldID])
        {
          xmlNode.ChildNodes[0].InnerText = text;
        }
      }
      else
      {
        packet.StartElement("field");
        packet.SetAttribute("itemid", pageEditorField.ItemID.ToString());
        packet.SetAttribute("language", pageEditorField.Language.ToString());
        packet.SetAttribute("version", pageEditorField.Version.ToString());
        packet.SetAttribute("fieldid", pageEditorField.FieldID.ToString());
        packet.SetAttribute("itemrevision", pageEditorField.Revision);
        packet.AddElement("value", text);
        packet.EndElement();
      }
      return new FieldDescriptor(item.Uri, field.ID, text, false);
    }

    public static string HandleFieldValue(string value, string fieldTypeKey)
    {
      switch (fieldTypeKey)
      {
        case "html":
        case "rich text":
          value = value.TrimEnd(' ');
          value = WebEditUtil.RepairLinks(value);
          break;
        case "text":
        case "single-line text":
          value = HttpUtility.HtmlDecode(value);
          break;
        case "integer":
        case "number":
          value = StringUtil.RemoveTags(value);
          break;
        case "multi-line text":
        case "memo":
          {
            Regex regex = new Regex("<br.*/*>", RegexOptions.IgnoreCase);
            value = regex.Replace(value, "\r\n");
            value = StringUtil.RemoveTags(value);
            break;
          }
        case "word document":
          value = string.Join(Environment.NewLine, value.Split(new string[3]
          {
                "\r\n",
                "\n\r",
                "\n"
          }, StringSplitOptions.None));
          break;
      }
      return value;
    }

    public static string GetFieldValidationErrorMessage(Field field, string value)
    {
      Assert.ArgumentNotNull(field, "field");
      Assert.ArgumentNotNull(value, "value");
      if (!Sitecore.Configuration.Settings.WebEdit.ValidationEnabled)
      {
        return string.Empty;
      }
      CultureInfo cultureInfo = LanguageUtil.GetCultureInfo();
      if (value.Length == 0)
      {
        return string.Empty;
      }
      switch (field.TypeKey)
      {
        case "integer":
          {
            long num2;
            if (long.TryParse(value, NumberStyles.Integer, cultureInfo, out num2))
            {
              return string.Empty;
            }
            return Translate.Text("\"{0}\" is not a valid integer.", value);
          }
        case "number":
          {
            double num;
            if (double.TryParse(value, NumberStyles.Float, cultureInfo, out num))
            {
              return string.Empty;
            }
            return Translate.Text("\"{0}\" is not a valid number.", value);
          }
        default:
          return string.Empty;
      }
    }

    public static void AddLayoutField(string layout, Packet packet, Item item, string fieldId = null)
    {
      Assert.ArgumentNotNull(packet, "packet");
      Assert.ArgumentNotNull(item, "item");
      if (fieldId == null)
      {
        fieldId = FieldIDs.FinalLayoutField.ToString();
      }
      if (!string.IsNullOrEmpty(layout))
      {
        layout = WebEditUtil.ConvertJSONLayoutToXML(layout);
        Assert.IsNotNull(layout, layout);
        if (!IsEditAllVersionsTicked())
        {
          layout = XmlDeltas.GetDelta(layout, new LayoutField(item.Fields[FieldIDs.LayoutField]).Value);
        }
        packet.StartElement("field");
        packet.SetAttribute("itemid", item.ID.ToString());
        packet.SetAttribute("language", item.Language.ToString());
        packet.SetAttribute("version", item.Version.ToString());
        packet.SetAttribute("fieldid", fieldId);
        packet.AddElement("value", layout);
        packet.EndElement();
      }
    }

    public static UrlString BuildChangeLanguageUrl(UrlString url, ItemUri itemUri, string languageName)
    {
      UrlString urlString = new UrlString(url.GetUrl());
      if (itemUri == (ItemUri)null)
      {
        return null;
      }
      SiteContext site = SiteContext.GetSite(WebEditUtil.SiteName);
      if (site == null)
      {
        return null;
      }
      Item itemNotNull = Client.GetItemNotNull(itemUri);
      using (new SiteContextSwitcher(site))
      {
        using (new LanguageSwitcher(itemNotNull.Language))
        {
          urlString = BuildChangeLanguageNewUrl(languageName, url, itemNotNull);
          LanguageEmbedding languageEmbedding = LinkManager.LanguageEmbedding;
          if (languageEmbedding == LanguageEmbedding.Never)
          {
            urlString["sc_lang"] = languageName;
            return urlString;
          }
          urlString.Remove("sc_lang");
          return urlString;
        }
      }
    }

    public static string GetContentEditorDialogFeatures()
    {
      string text = "location=0,menubar=0,status=0,toolbar=0,resizable=1,getBestDialogSize:true";
      DeviceItem device = Context.Device;
      if (device == null)
      {
        return text;
      }
      SitecoreClientDeviceCapabilities sitecoreClientDeviceCapabilities = device.Capabilities as SitecoreClientDeviceCapabilities;
      if (sitecoreClientDeviceCapabilities == null)
      {
        return text;
      }
      if (sitecoreClientDeviceCapabilities.RequiresScrollbarsOnWindowOpen)
      {
        text += ",scrollbars=1,dependent=1";
      }
      return text;
    }

    public static bool IsQueryStateEnabled<T>(Item contextItem) where T : Command, new()
    {
      T val = new T();
      CommandContext context = new CommandContext(new Item[1]
      {
            contextItem
      });
      return val.QueryState(context) == CommandState.Enabled;
    }

    public static bool IsEditAllVersionsTicked()
    {
      if (StringUtility.EvaluateCheckboxRegistryKeyValue(Registry.GetString(Sitecore.ExperienceEditor.Constants.RegistryKeys.EditAllVersions)))
      {
        return IsEditAllVersionsAllowed();
      }
      return false;
    }

    public static bool IsEditAllVersionsAllowed()
    {
      if (!isGetLayoutSourceFieldsExists.HasValue)
      {
        isGetLayoutSourceFieldsExists = (CorePipelineFactory.GetPipeline("getLayoutSourceFields", string.Empty) != null);
        if (!isGetLayoutSourceFieldsExists.Value)
        {
          Log.Warn("Pipeline getLayoutSourceFields is turned off.", new object());
        }
      }
      return Context.Site != null && isGetLayoutSourceFieldsExists.Value && Sitecore.ExperienceEditor.Settings.WebEdit.ExperienceEditorEditAllVersions && Context.Site.DisplayMode != 0 && WebUtil.GetQueryString("sc_disable_edit") != "yes" && WebUtil.GetQueryString("sc_duration") != "temporary";
    }

    public static ID GetCurrentLayoutFieldId()
    {
      if (!IsEditAllVersionsTicked())
      {
        return FieldIDs.FinalLayoutField;
      }
      return FieldIDs.LayoutField;
    }

    private static UrlString BuildChangeLanguageNewUrl(string languageName, UrlString url, Item item)
    {
      Assert.ArgumentNotNull(languageName, "languageName");
      Assert.ArgumentNotNull(url, "url");
      Assert.ArgumentNotNull(item, "item");
      Language language;
      bool condition = Language.TryParse(languageName, out language);
      Assert.IsTrue(condition, $"Cannot parse the language ({languageName}).");
      UrlOptions defaultOptions = UrlOptions.DefaultOptions;
      defaultOptions.Language = language;
      Item item2 = item.Database.GetItem(item.ID, language);
      Assert.IsNotNull(item2, $"Item not found ({item.ID}, {language}).");
      string itemUrl = LinkManager.GetItemUrl(item2, defaultOptions);
      UrlString urlString = new UrlString(itemUrl);
      foreach (string key in url.Parameters.Keys)
      {
        urlString.Parameters[key] = url.Parameters[key];
      }
      return urlString;
    }
  }
}