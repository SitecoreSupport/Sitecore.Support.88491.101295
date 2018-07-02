using Sitecore.Configuration;
using Sitecore.Diagnostics;
using Sitecore.Exceptions;
using Sitecore.ExperienceEditor.Exceptions;
using Sitecore.ExperienceEditor.Speak.Server;
using Sitecore.ExperienceEditor.Speak.Server.Requests;
using Sitecore.ExperienceEditor.Speak.Server.Responses;
using Sitecore.ExperienceEditor.Utils;
using Sitecore.Globalization;
using Sitecore.Publishing;
using Sitecore.Security.Accounts;
using System;

namespace Sitecore.Support.ExperienceEditor.Speak.Server.Requests
{
  public abstract class PipelineProcessorRequest<T> : Request where T : Sitecore.ExperienceEditor.Speak.Server.Contexts.Context
  {
    public RequestArgs Args
    {
      get;
      private set;
    }

    public T RequestContext
    {
      get;
      set;
    }

    protected Response GenerateExceptionResponse(string errorMessage, Exception exception, string postScriptFunc = "")
    {
      Log.Error(exception.Message, exception, this);
      return new Response
      {
        Error = true,
        ErrorMessage = errorMessage,
        PostScriptFunc = (postScriptFunc ?? string.Empty)
      };
    }

    [Obsolete("This method is obsolete and will be removed in the next product version. Use GenerateExceptionResponse(string errorMessage, Exception exception, string postScriptFunc) instead.")]
    protected Response GenerateExceptionResponse(string errorMessage, string logMessage, string postScriptFunc = "")
    {
      Log.Error(logMessage, this);
      return new Response
      {
        Error = true,
        ErrorMessage = errorMessage,
        PostScriptFunc = (postScriptFunc ?? string.Empty)
      };
    }

    protected User GetUser()
    {
      User user = Sitecore.Context.User;
      if (user.Name == "extranet\\Anonymous" && Sitecore.Context.PageMode.IsPreview && Settings.Preview.AsAnonymous)
      {
        string shellUser = PreviewManager.GetShellUser();
        if (string.IsNullOrEmpty(shellUser))
        {
          return user;
        }
        return User.FromName(shellUser, true);
      }
      return user;
    }

    public override Response Process(RequestArgs requestArgs)
    {
      try
      {
        Assert.ArgumentNotNull(requestArgs, "requestArgs");
        Args = requestArgs;
        RequestContext = ((Request)this).GetContext<T>(Args.Data);
        Assert.IsNotNull(RequestContext, "Could not get context for requestArgs:{0}", requestArgs.Data);
        using (new UserSwitcher(GetUser()))
        {
          using (new LanguageSwitcher(WebUtility.ClientLanguage))
          {
            return new PipelineProcessorResponse
            {
              Error = false,
              ErrorMessage = null,
              ResponseValue = ProcessRequest()
            };
          }
        }
      }
      catch (FieldValidationException ex)
      {
        string errorMessage = string.Format("MY: {0} <a href='#' onclick='javascript:window.parent.ExperienceEditor.getContext().instance.ValidationUtil.selectChrome(\"{1}\", \"{2}\");window.parent.ExperienceEditor.getContext().instance.ValidationUtil.setChromesNotValid(\"{1}\", \"{2}\", \"\");' class='OptionTitle'>{3}</a>", ex.Message, ex.FieldId, ex.FieldItemId, Translate.Text("Show error"));
        string postScriptFunc = $"window.parent.ExperienceEditor.getContext().instance.ValidationUtil.setChromesNotValid(\"{ex.FieldId}\", \"{ex.FieldItemId}\", \"\");";
        return GenerateExceptionResponse(errorMessage, ex, postScriptFunc);
      }
      catch (ItemNotFoundException exception)
      {
        using (new LanguageSwitcher(WebUtility.ClientLanguage))
        {
          return GenerateExceptionResponse(Translate.Text("The item does not exist. It may have been deleted by another user."), exception, "");
        }
      }
      catch (Exception exception2)
      {
        return GenerateExceptionResponse(Translate.Text("MY:  An error occurred."), exception2, "");
      }
    }

    public abstract PipelineProcessorResponseValue ProcessRequest();
  }
}