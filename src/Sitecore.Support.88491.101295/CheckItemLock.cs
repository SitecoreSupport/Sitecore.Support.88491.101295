﻿using Sitecore.Data.Items;
using Sitecore.ExperienceEditor.Speak.Server.Contexts;
using Sitecore.ExperienceEditor.Speak.Server.Responses;
using Sitecore.Globalization;

namespace Sitecore.Support.ExperienceEditor.Speak.Ribbon.Requests.SaveItem
{
  public class CheckItemLock : Sitecore.Support.ExperienceEditor.Speak.Server.Requests.PipelineProcessorRequest<PageContext>
  {
    public override PipelineProcessorResponseValue ProcessRequest()
    {
      base.RequestContext.ValidateContextItem();
      PipelineProcessorResponseValue pipelineProcessorResponseValue = new PipelineProcessorResponseValue();
      Item item = base.RequestContext.Item;
      if (item.Locking.IsLocked() && !item.Locking.HasLock())
      {
        pipelineProcessorResponseValue.AbortMessage = Translate.Text("Unable to save changes because the corresponding content has been locked by another user.");
      }
      return pipelineProcessorResponseValue;
    }
  }
}