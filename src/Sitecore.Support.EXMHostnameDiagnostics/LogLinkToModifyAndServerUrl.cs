using Sitecore.Diagnostics;
using Sitecore.Modules.EmailCampaign.Core.Pipelines.GenerateLink;

namespace Sitecore.Support
{
  public class LogLinkToModifyAndServerUrl : GenerateLinkProcessor
  {
    public override void Process(GenerateLinkPipelineArgs args)
    {
      Log.Audit($"Sitecore Support link to be processed: {args.Url}", this);
      Log.Audit($"Sitecore Support Server Url: {args.ServerUrl}", this);
    }
  }
}