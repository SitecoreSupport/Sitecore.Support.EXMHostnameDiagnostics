using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Sitecore.DependencyInjection;
using Sitecore.Diagnostics;
using Sitecore.ExM.Framework.Diagnostics;
using Sitecore.Links;
using Sitecore.Modules.EmailCampaign;
using Sitecore.Modules.EmailCampaign.Core;
using Sitecore.Modules.EmailCampaign.Core.HostnameMapping;
using Sitecore.Modules.EmailCampaign.Core.Links;
using Sitecore.Modules.EmailCampaign.Core.Pipelines.GenerateLink;
using Sitecore.Sites;
using Sitecore.Web;
using HostnameMappingService = Sitecore.Support.Modules.EmailCampaign.Core.HostnameMapping.HostnameMappingService;

namespace Sitecore.Support.Modules.EmailCampaign.Core.Pipelines.GenerateLink
{
  public class MapHostname : GenerateLinkProcessor
  {
    private readonly IHostnameMappingService _hostnameMappingService;

    public MapHostname() : this(new HostnameMappingService(
      ServiceLocator.ServiceProvider.GetService<IHostnameMappingRepository>(),
      ServiceLocator.ServiceProvider.GetService<ILogger>()))
    {
    }

    public MapHostname(IHostnameMappingService hostnameMappingService)
    {
      Assert.ArgumentNotNull(hostnameMappingService, "hostnameMappingService");

      _hostnameMappingService = hostnameMappingService;
    }

    protected internal virtual bool IsInternalLink(GenerateLinkPipelineArgs args)
    {
      Assert.ArgumentNotNull(args, "args");

      Uri uri;
      if (!Uri.TryCreate(args.Url, UriKind.Absolute, out uri))
      {
        return false;
      }

      var leftPart = uri.GetLeftPart(UriPartial.Authority);
      var serverUrl = WebUtil.GetServerUrl();
      return _hostnameMappingService.GetKnownHostnames(serverUrl)
        .Contains(leftPart, StringComparer.InvariantCultureIgnoreCase);
    }

    public override void Process(GenerateLinkPipelineArgs args)
    {
      Assert.IsNotNull(args, "Arguments can't be null");
      Assert.IsNotNull(args.Url, "Url link can't be null");

      Log.Audit($"Sitecore Support In the MapHostname processor. Link {args.Url}, PreviewMode: {args.PreviewMode}, MailMessage.ID: {args.MailMessage.ID}, MailMessage.Language: {args.MailMessage.TargetLanguage}", this);

      if (args.Url.StartsWith("#") || args.Url.StartsWith("javascript:") || args.Url.StartsWith("mailto:"))
      {
        Log.Audit($"Sitecore Support link {args.Url} was skipped since it was not meant for encoding.", this);
        return;
      }

      DynamicLink link;
      if (args.Url.IndexOf("~/link.aspx?", StringComparison.InvariantCulture) >= 0 &&
          DynamicLink.TryParse(args.Url, out link))
      {
        var defaultUrlOptions = LinkManager.GetDefaultUrlOptions();
        defaultUrlOptions.SiteResolving = true;
        defaultUrlOptions.Language = args.MailMessage.TargetLanguage;
        defaultUrlOptions.AlwaysIncludeServerUrl = true;
        using (new SiteContextSwitcher(Util.GetContentSite()))
        {
          string encodedUrl = LinkManager.GetItemUrl(new ItemUtilExt().GetItem(link.ItemId), defaultUrlOptions);
          Log.Audit($"Dynamic link {args.Url} was transformed to {encodedUrl}", this);
          args.Url = encodedUrl;
        }
      }
      else
      {
        if (!LinksManager.IsAbsoluteLink(args.Url))
        {
          var transformedLink = args.ServerUrl + StringUtil.EnsurePrefix('/', args.Url);
          Log.Audit($"Relative link {args.Url} was transformed to {transformedLink}", this);
          args.Url = transformedLink;
          args.IsInternalLink = true;
        }
        else
        {
          args.IsInternalLink = IsInternalLink(args);
        }
      }

      Log.Audit($"Link {args.Url} is considered internal: {args.IsInternalLink}", this);

      args.Url = args.PreviewMode
        ? _hostnameMappingService.GetPreviewUrl(args.Url, args.MailMessage.ManagerRoot)
        : _hostnameMappingService.GetPublicUrl(args.Url, args.MailMessage.ManagerRoot);

      Log.Audit($"Link after all transformations: {args.Url}", this);
    }
  }
}