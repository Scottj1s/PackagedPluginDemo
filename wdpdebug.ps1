# c:\>powershell -File wdpdebug.ps1
# Execute WDP in debug mode, running as SYSTEM (via psexec), and 'inheriting' console state
# Params supported: -traceLevel, -traceFlags (see below)

param (
    [int]$bufferWidth = 0,
    [int]$bufferHeight = 0,
    [int]$windowWidth = 0,
    [int]$windowHeight = 0,
    [int]$traceLevel = 4,       # Default = INFORMATION (see below)
    [int]$traceFlags = -1,      # Default = All Categories (see below)
    [string]$wdp
)

# First execution (without params): 
# Capture console state and invoke script again with params.
if($bufferWidth -eq 0)
{
    $h = Get-Host
    $bufferWidth = $h.UI.RawUI.BufferSize.Width
    $bufferHeight = $h.UI.RawUI.BufferSize.Height
    $windowWidth = $h.UI.RawUI.WindowSize.Width
    $windowHeight = $h.UI.RawUI.WindowSize.Height
    $scriptPath = $MyInvocation.MyCommand.Definition
    
    # Prefer enlistment-built webmanagement for debug mode
    $wdp = "$env:_nttree\WebManagement\WebManagement.exe"
    if ((Test-Path $wdp) -eq $False)
    {
        Try
        {
            # Fall back to production binary
            $wdp = (get-command -ea stop 'WebManagement.exe').Path
        }
        Catch [System.Management.Automation.CommandNotFoundException] 
        {
            # No WebManagement found - bail out
            write-host -ForegroundColor Red "WebManagement.exe not found in enlistment or system folder."
            write-host -ForegroundColor Red "You may need to Feature-On-Demand deploy it."
            write-host 
            exit
        }    
    }
    
    # Verify psexec found, otherwise prompt user to download
    $psexec = $null
    try
    {
        $psexec = (get-command -ea stop 'psexec.exe').Path
    }
    catch [System.Management.Automation.CommandNotFoundException] 
    {
        write-host -ForegroundColor Red "Could not find psexec.exe in path, which is required to run WDP as system."
        write-host -ForegroundColor Red "It can be downloaded from https://technet.microsoft.com/en-us/sysinternals/pxexec"
        write-host 
        exit
    }

    # Launch psexec
    write-host "Launching WDP as SYSTEM..."
    $psargs = @('-d', '-i', '-s', '-accepteula', "$pshome\powershell.exe", '-File', $scriptPath, 
        '-bufferWidth', $bufferWidth, '-bufferHeight', $bufferHeight, 
        '-windowWidth', $windowWidth, '-windowHeight', $windowHeight, 
        '-traceLevel', $traceLevel, '-traceFlags', $traceFlags, '-wdp', $wdp)
    & $psexec $psargs >$null 2>$null
}
# Second execution:
# Psexec powershell (as SYSTEM), apply console state, execute wdp in debug mode.
else
{
    $h = Get-Host
    $bufferSize = $h.UI.RawUI.BufferSize
    $bufferSize.Width = $bufferWidth
    $bufferSize.Height = $bufferHeight
    $h.UI.RawUI.BufferSize = $bufferSize
    $windowSize = $h.UI.RawUI.WindowSize
    $windowSize.Width = $windowWidth
    $windowSize.Height = $windowHeight
    $h.UI.RawUI.WindowSize = $windowSize
    $h.UI.RawUI.ForegroundColor = 'White'
    $h.UI.RawUI.WindowTitle = 'Windows Device Portal - Debug Mode Console'

    write-host -ForegroundColor Cyan "To debug WDP process, attach to the PID a few lines below, or to webmanagement.exe"
    write-host -ForegroundColor Cyan "To debug children (packaged plugin sponsors),"
    write-host -ForegroundColor Cyan "  WinDbg: .childdbg 1"
    write-host -ForegroundColor Cyan "  Visual Studio: Child Process Debugging Power Tool (https://aka.ms/r24988)"
    write-host
    
    # Trace Levels (default is Info, 4):  
    #   TRACE_LEVEL_NONE        0   // Tracing is not on
    #   TRACE_LEVEL_FATAL       1   // Deprecated name for Abnormal exit or termination
    #   TRACE_LEVEL_ERROR       2   // Severe errors that need logging
    #   TRACE_LEVEL_WARNING     3   // Warnings such as allocation failure
    #   TRACE_LEVEL_INFORMATION 4   // Includes non-error cases(e.g.,Entry-Exit)
    #   TRACE_LEVEL_VERBOSE     5   // Detailed traces from intermediate steps
    # Trace Flags (function category mask, default is -1, all):
    #   Plugin: 1
    #   Service: 2
    #   Resource: 4
    #   Utils: 8
    #   REST: 16
    #   ETW: 32
    #   DNSSD: 64
    # Add additional params as desired: -clearandssl, -httpport, etc (run webmanagement -debug for options)
    #& $wdp @('-debug', '-clearandssl', '-traceLevel', $traceLevel, '-traceFlags', $traceFlags, '-protectionmode', '2', '-usedefaultauth')
    & $wdp @('-debug', '-clearandssl', '-traceLevel', $traceLevel, '-traceFlags', $traceFlags, '-protectionmode', '2')
}
