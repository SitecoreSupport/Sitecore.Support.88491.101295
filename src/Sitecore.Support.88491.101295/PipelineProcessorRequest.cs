using Sitecore;
using Sitecore.Configuration;
using Sitecore.Diagnostics;
using Sitecore.Exceptions;
using Sitecore.ExperienceEditor.Exceptions;
using Sitecore.ExperienceEditor.Speak.Server;
using Sitecore.ExperienceEditor.Speak.Server.Contexts;
using Sitecore.ExperienceEditor.Speak.Server.Requests;
using Sitecore.ExperienceEditor.Speak.Server.Responses;
using Sitecore.ExperienceEditor.Utils;
using Sitecore.Globalization;
using Sitecore.Publishing;
using Sitecore.Security.Accounts;
using System;
using System.Web;

namespace Sitecore.ExperienceEditor.Speak.Server.Requests
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

    public abstract PipelineProcessorResponseValue ProcessRequest();

    public override Response Process(RequestArgs requestArgs)
    {
      try
      {
        Assert.ArgumentNotNull(requestArgs, "requestArgs");
        Args = requestArgs;
        RequestContext = ((Request)this).GetContext<T>(HttpUtility.HtmlDecode(Args.Data));
        Assert.IsNotNull(RequestContext, "Could not get context for requestArgs:{0}", requestArgs.Data);
        using (new UserSwitcher(GetUser()))
        {
          using (new LanguageSwitcher(WebUtility.ClientLanguage))
          {
            if (Sitecore.Context.User.IsAuthenticated)
            {
              PipelineProcessorResponse pipelineProcessorResponse = new PipelineProcessorResponse();
              pipelineProcessorResponse.Error = false;
              pipelineProcessorResponse.ErrorMessage = null;
              pipelineProcessorResponse.ResponseValue = ProcessRequest();
              return pipelineProcessorResponse;
            }
            string text = "Anonymous requests are not allowed";
            return GenerateExceptionResponse(text, new Exception(text), "");
          }
        }
      }
      catch (FieldValidationException ex)
      {
        string.Format("{0} <a href='#' onclick='javascript:window.parent.ExperienceEditor.getContext().instance.ValidationUtil.selectChrome(\"{1}\", \"{2}\");window.parent.ExperienceEditor.getContext().instance.ValidationUtil.setChromesNotValid(\"{1}\", \"{2}\", \"\");' class='OptionTitle'>{3}</a>", ex.Message, ex.FieldId, ex.FieldItemId, Translate.Text("Show error"));
        string postScriptFunc = $"window.parent.ExperienceEditor.getContext().instance.ValidationUtil.setChromesNotValid(\"{ex.FieldId}\", \"{ex.FieldItemId}\", \"\");";
        return GenerateExceptionResponse(ex.Message, ex, postScriptFunc);
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
        return GenerateExceptionResponse(Translate.Text("An error occurred."), exception2, "");
      }
    }

    [Obsolete("This method is obsolete and will be removed in the next product version. Use GenerateExceptionResponse(string errorMessage, Exception exception, string postScriptFunc) instead.")]
    protected Response GenerateExceptionResponse(string errorMessage, string logMessage, string postScriptFunc = "")
    {
      Log.Error(logMessage, this);
      Response response = new Response();
      response.Error = true;
      response.ErrorMessage = errorMessage;
      response.PostScriptFunc = (postScriptFunc ?? string.Empty);
      return response;
    }

    protected Response GenerateExceptionResponse(string errorMessage, Exception exception, string postScriptFunc = "")
    {
      Log.Error(exception.Message, exception, this);
      Response response = new Response();
      response.Error = true;
      response.ErrorMessage = errorMessage;
      response.PostScriptFunc = (postScriptFunc ?? string.Empty);
      return response;
    }

    protected User GetUser()
    {
      User user = Sitecore.Context.User;
      if (!(user.Name != "extranet\\Anonymous") && Sitecore.Context.PageMode.IsPreview && Sitecore.Configuration.Settings.Preview.AsAnonymous)
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
  }
}