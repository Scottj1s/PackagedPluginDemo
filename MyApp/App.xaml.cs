using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// Namespaces required to implement a packaged plugin
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.System.Diagnostics.DevicePortal;
using Windows.Web.Http;
using Windows.Web.Http.Headers;

namespace MyApp
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
sealed partial class App : Application
{
    private BackgroundTaskDeferral taskDeferral;
    private DevicePortalConnection devicePortalConnection;
    private static Uri statusUri = new Uri("/myapp/api/status", UriKind.Relative);

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;
        }

// Implement background task handler with a DevicePortalConnection
protected override void OnBackgroundActivated(BackgroundActivatedEventArgs args)
{
    // Take a deferral to allow the background task to continue executing 
    var taskInstance = args.TaskInstance;
    this.taskDeferral = taskInstance.GetDeferral();
    taskInstance.Canceled += TaskInstance_Canceled;

    // Create a DevicePortal client from an AppServiceConnection 
    var details = taskInstance.TriggerDetails as AppServiceTriggerDetails;
    var appServiceConnection = details.AppServiceConnection;
    this.devicePortalConnection = DevicePortalConnection.GetForAppServiceConnection(appServiceConnection);

    // Add handlers for RequestReceived and Closed events
    devicePortalConnection.RequestReceived += DevicePortalConnection_RequestReceived;
    devicePortalConnection.Closed += DevicePortalConnection_Closed;
}

// RequestReceived handler demonstrating response construction, based on request
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

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (e.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    // When the navigation stack isn't restored navigate to the first page,
                    // configuring the new page by passing required information as a navigation
                    // parameter
                    rootFrame.Navigate(typeof(MainPage), e.Arguments);
                }
                // Ensure the current window is active
                Window.Current.Activate();
            }
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
            deferral.Complete();
        }
    }
}
