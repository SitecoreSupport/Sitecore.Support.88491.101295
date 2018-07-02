using Sitecore.Caching;
using Sitecore.Data;
using Sitecore.ExperienceEditor.Speak.Server.Responses;
using Sitecore.ExperienceEditor.Switchers;
using Sitecore.Globalization;
using Sitecore.Pipelines;
using Sitecore.Pipelines.Save;

namespace Sitecore.Support.ExperienceEditor.Speak.Ribbon.Requests.SaveItem
{
  public class CallServerSavePipeline : Sitecore.Support.ExperienceEditor.Speak.Server.Requests.PipelineProcessorRequest<Sitecore.Support.ExperienceEditor.Speak.Server.Contexts.PageContext>
  {
    public override PipelineProcessorResponseValue ProcessRequest()
    {
      PipelineProcessorResponseValue pipelineProcessorResponseValue = new PipelineProcessorResponseValue();
      Pipeline pipeline = PipelineFactory.GetPipeline("saveUI");
      pipeline.ID = ShortID.Encode(ID.NewID);
      SaveArgs saveArgs = base.RequestContext.GetSaveArgs();
      using (new ClientDatabaseSwitcher(base.RequestContext.Item.Database))
      {
        pipeline.Start(saveArgs);
        CacheManager.GetItemCache(base.RequestContext.Item.Database).Clear();
        pipelineProcessorResponseValue.AbortMessage = Translate.Text(saveArgs.Error);
        return pipelineProcessorResponseValue;
      }
    }
  }
}