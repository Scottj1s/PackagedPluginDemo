using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Namespaces required to implement a packaged plugin
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.System.Diagnostics.DevicePortal;
using Windows.Web.Http;
using Windows.Web.Http.Headers;

namespace MyComponent
{
public sealed class MyBackgroundHandler : IBackgroundTask
{
    private BackgroundTaskDeferral taskDeferral;
    private DevicePortalConnection devicePortalConnection;
    private static Uri statusUri = new Uri("/mycomponent/api/status", UriKind.Relative);

    // Implement background task handler with a DevicePortalConnection
    public void Run(IBackgroundTaskInstance taskInstance)
    {
        // Implement as for foreground handler's OnBackgroundActivated
        // Take a deferral to allow the background task to continue executing 
        this.taskDeferral = taskInstance.GetDeferral();
        taskInstance.Canceled += TaskInstance_Canceled;

        // Create a DevicePortal client from an AppServiceConnection 
        var details = taskInstance.TriggerDetails as AppServiceTriggerDetails;
        var appServiceConnection = details.AppServiceConnection;
        this.devicePortalConnection = DevicePortalConnection.GetForAppServiceConnection(appServiceConnection);

        // Add Closed, RequestReceived handlers 
        devicePortalConnection.Closed += DevicePortalConnection_Closed;
        devicePortalConnection.RequestReceived += DevicePortalConnection_RequestReceived;
    }

    // Sample RequestReceived handler demonstrating response construction, based on request
    private void DevicePortalConnection_RequestReceived(DevicePortalConnection sender, DevicePortalConnectionRequestReceivedEventArgs args)
    {
        if (args.RequestMessage.RequestUri.AbsolutePath.ToString() == statusUri.ToString())
        {
            args.ResponseMessage.StatusCode = HttpStatusCode.Ok;
            args.ResponseMessage.Content = new HttpStringContent("{ \"status\": \"good\" }");
            args.ResponseMessage.Content.Headers.ContentType = new HttpMediaTypeHeaderValue("application/json");
        }
        else
        {
            args.ResponseMessage.StatusCode = HttpStatusCode.NotFound;
        }
    }

    // Complete the deferral if task is canceled or DevicePortal connection closed
    private void Close()
    {
        this.devicePortalConnection = null;
        this.taskDeferral.Complete();
    }

    private void TaskInstance_Canceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
    {
        Close();
    }

    private void DevicePortalConnection_Closed(DevicePortalConnection sender, DevicePortalConnectionClosedEventArgs args)
    {
        Close();
    }
}
}
