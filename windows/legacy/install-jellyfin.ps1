[CmdletBinding()]

param(
    [Switch]$Quiet,
    [Switch]$InstallAsService,
    [System.Management.Automation.pscredential]$ServiceUser,
    [switch]$CreateDesktopShorcut,
    [switch]$LaunchJellyfin,
    [switch]$MigrateEmbyLibrary,
    [string]$InstallLocation,
    [string]$EmbyLibraryLocation,
    [string]$JellyfinLibraryLocation
)
<# This form was created using POSHGUI.com  a free online gui designer for PowerShell
.NAME
    Install-Jellyfin
#>

#This doesn't need to be used by default anymore, but I am keeping it in as a function for future use.
function Elevate-Window {
    # Get the ID and security principal of the current user account
    $myWindowsID=[System.Security.Principal.WindowsIdentity]::GetCurrent()
    $myWindowsPrincipal=new-object System.Security.Principal.WindowsPrincipal($myWindowsID)
 
    # Get the security principal for the Administrator role
    $adminRole=[System.Security.Principal.WindowsBuiltInRole]::Administrator
 
    # Check to see if we are currently running "as Administrator"
    if ($myWindowsPrincipal.IsInRole($adminRole))
    {
        # We are running "as Administrator" - so change the title and background color to indicate this
        $Host.UI.RawUI.WindowTitle = $myInvocation.MyCommand.Definition + "(Elevated)"
        $Host.UI.RawUI.BackgroundColor = "DarkBlue"
        clear-host
    }
    else
    {
        # We are not running "as Administrator" - so relaunch as administrator
   
        # Create a new process object that starts PowerShell
        $newProcess = new-object System.Diagnostics.ProcessStartInfo "PowerShell";
   
        # Specify the current script path and name as a parameter
        $newProcess.Arguments = $myInvocation.MyCommand.Definition;
   
        # Indicate that the process should be elevated
        $newProcess.Verb = "runas";
   
        # Start the new process
        [System.Diagnostics.Process]::Start($newProcess);
   
        # Exit from the current, unelevated, process
        exit
    }
}

#FIXME The install methods should be a function that takes all the params, the quiet flag should be a paramset

if($Quiet.IsPresent -or $Quiet -eq $true){
    if([string]::IsNullOrEmpty($JellyfinLibraryLocation)){
        $Script:JellyfinDataDir = "$env:LOCALAPPDATA\jellyfin\"
    }else{
        $Script:JellyfinDataDir = $JellyfinLibraryLocation
    }
    if([string]::IsNullOrEmpty($InstallLocation)){
        $Script:DefaultJellyfinInstallDirectory = "$env:Appdata\jellyfin\"
    }else{
        $Script:DefaultJellyfinInstallDirectory = $InstallLocation
    }
    
    if([string]::IsNullOrEmpty($EmbyLibraryLocation)){
        $Script:defaultEmbyDataDir = "$env:Appdata\Emby-Server\data\"
    }else{
        $Script:defaultEmbyDataDir = $EmbyLibraryLocation
    }
    
    if($InstallAsService.IsPresent -or $InstallAsService -eq $true){
        $Script:InstallAsService = $true  
    }else{$Script:InstallAsService = $false}
    if($null -eq $ServiceUser){
        $Script:InstallServiceAsUser = $false
    }else{
        $Script:InstallServiceAsUser = $true
        $Script:UserCredentials = $ServiceUser
        $Script:JellyfinDataDir = "$env:HOMEDRIVE\Users\$($Script:UserCredentials.UserName)\Appdata\Local\jellyfin\"}
    if($CreateDesktopShorcut.IsPresent -or $CreateDesktopShorcut -eq $true) {$Script:CreateShortcut = $true}else{$Script:CreateShortcut = $false}
    if($MigrateEmbyLibrary.IsPresent -or $MigrateEmbyLibrary -eq $true){$Script:MigrateLibrary = $true}else{$Script:MigrateLibrary = $false}
    if($LaunchJellyfin.IsPresent -or $LaunchJellyfin -eq $true){$Script:StartJellyfin = $true}else{$Script:StartJellyfin = $false}
    
    if(-not (Test-Path $Script:DefaultJellyfinInstallDirectory)){
        mkdir $Script:DefaultJellyfinInstallDirectory
    }
    Copy-Item -Path $PSScriptRoot/* -DestinationPath "$Script:DefaultJellyfinInstallDirectory/" -Force -Recurse
    if($Script:InstallAsService){
        if($Script:InstallServiceAsUser){
            &"$Script:DefaultJellyfinInstallDirectory\nssm.exe" install Jellyfin `"$Script:DefaultJellyfinInstallDirectory\jellyfin.exe`" --datadir `"$Script:JellyfinDataDir`"
            Start-Sleep -Milliseconds 500
            &sc.exe config Jellyfin obj=".\$($Script:UserCredentials.UserName)" password="$($Script:UserCredentials.GetNetworkCredential().Password)"
            &"$Script:DefaultJellyfinInstallDirectory\nssm.exe" set Jellyfin Start SERVICE_DELAYED_AUTO_START 
        }else{
            &"$Script:DefaultJellyfinInstallDirectory\nssm.exe" install Jellyfin `"$Script:DefaultJellyfinInstallDirectory\jellyfin.exe`" --datadir `"$Script:JellyfinDataDir`"
            Start-Sleep -Milliseconds 500
            #&"$Script:DefaultJellyfinInstallDirectory\nssm.exe" set Jellyfin ObjectName $Script:UserCredentials.UserName $Script:UserCredentials.GetNetworkCredential().Password
            #Set-Service -Name Jellyfin -Credential $Script:UserCredentials
            &"$Script:DefaultJellyfinInstallDirectory\nssm.exe" set Jellyfin Start SERVICE_DELAYED_AUTO_START 
        }
    }
    if($Script:MigrateLibrary){
        Copy-Item -Path $Script:defaultEmbyDataDir/config -Destination $Script:JellyfinDataDir -force -Recurse
        Copy-Item -Path $Script:defaultEmbyDataDir/cache -Destination $Script:JellyfinDataDir -force -Recurse
        Copy-Item -Path $Script:defaultEmbyDataDir/data -Destination $Script:JellyfinDataDir -force -Recurse
        Copy-Item -Path $Script:defaultEmbyDataDir/metadata -Destination $Script:JellyfinDataDir -force -Recurse
        Copy-Item -Path $Script:defaultEmbyDataDir/root -Destination $Script:JellyfinDataDir -force -Recurse
    }
    if($Script:CreateShortcut){
        $WshShell = New-Object -comObject WScript.Shell
        $Shortcut = $WshShell.CreateShortcut("$Home\Desktop\Jellyfin.lnk")
        $Shortcut.TargetPath = "$Script:DefaultJellyfinInstallDirectory\jellyfin.exe"
        $Shortcut.Save()
    }
    if($Script:StartJellyfin){
        if($Script:InstallAsService){
            Get-Service Jellyfin | Start-Service
        }else{
            Start-Process -FilePath $Script:DefaultJellyfinInstallDirectory\jellyfin.exe -PassThru
        }
    }
}else{

}
Add-Type -AssemblyName System.Windows.Forms
[System.Windows.Forms.Application]::EnableVisualStyles()

$Script:JellyFinDataDir = "$env:LOCALAPPDATA\jellyfin\"
$Script:DefaultJellyfinInstallDirectory = "$env:Appdata\jellyfin\"
$Script:defaultEmbyDataDir = "$env:Appdata\Emby-Server\"
$Script:InstallAsService = $False
$Script:InstallServiceAsUser = $false
$Script:CreateShortcut = $false
$Script:MigrateLibrary = $false
$Script:StartJellyfin = $false

function InstallJellyfin {
    Write-Host "Install as service: $Script:InstallAsService" 
    Write-Host "Install as serviceuser: $Script:InstallServiceAsUser"
    Write-Host "Create Shortcut: $Script:CreateShortcut"
    Write-Host "MigrateLibrary: $Script:MigrateLibrary"
    $GUIElementsCollection | ForEach-Object {
        $_.Enabled = $false
    }
    Write-Host "Making Jellyfin directory"
    $ProgressBar.Minimum = 1
    $ProgressBar.Maximum = 100
    $ProgressBar.Value = 1
    if($Script:DefaultJellyfinInstallDirectory -ne $InstallLocationBox.Text){
        Write-Host "Custom Install Location Chosen: $($InstallLocationBox.Text)"
        $Script:DefaultJellyfinInstallDirectory = $InstallLocationBox.Text
    }
    if($Script:JellyfinDataDir -ne $CustomLibraryBox.Text){
        Write-Host "Custom Library Location Chosen: $($CustomLibraryBox.Text)"
        $Script:JellyfinDataDir = $CustomLibraryBox.Text
    }
    if(-not (Test-Path $Script:DefaultJellyfinInstallDirectory)){
        mkdir $Script:DefaultJellyfinInstallDirectory
    }
    Write-Host "Copying Jellyfin Data"
    $progressbar.Value = 10 
    Copy-Item -Path $PSScriptRoot/* -Destination $Script:DefaultJellyfinInstallDirectory/ -Force -Recurse
    Write-Host "Finished Copying"
    $ProgressBar.Value = 50
    if($Script:InstallAsService){
        if($Script:InstallServiceAsUser){
            Write-Host "Installing Service as user $($Script:UserCredentials.UserName)"
            &"$Script:DefaultJellyfinInstallDirectory\nssm.exe" install Jellyfin `"$Script:DefaultJellyfinInstallDirectory\jellyfin.exe`" --datadir `"$Script:JellyfinDataDir`"
            Start-Sleep -Milliseconds 2000
            &sc.exe config Jellyfin obj=".\$($Script:UserCredentials.UserName)" password="$($Script:UserCredentials.GetNetworkCredential().Password)"
            &"$Script:DefaultJellyfinInstallDirectory\nssm.exe" set Jellyfin Start SERVICE_DELAYED_AUTO_START 
        }else{
            Write-Host "Installing Service as LocalSystem"
            &"$Script:DefaultJellyfinInstallDirectory\nssm.exe" install Jellyfin `"$Script:DefaultJellyfinInstallDirectory\jellyfin.exe`" --datadir `"$Script:JellyfinDataDir`"
            Start-Sleep -Milliseconds 2000
            &"$Script:DefaultJellyfinInstallDirectory\nssm.exe" set Jellyfin Start SERVICE_DELAYED_AUTO_START 
        }
    }
    $progressbar.Value = 60
    if($Script:MigrateLibrary){
        if($Script:defaultEmbyDataDir -ne $LibraryLocationBox.Text){
           Write-Host "Custom location defined for emby library: $($LibraryLocationBox.Text)"
           $Script:defaultEmbyDataDir = $LibraryLocationBox.Text
        }
        Write-Host "Copying emby library from $Script:defaultEmbyDataDir to $Script:JellyFinDataDir"
        Write-Host "This could take a while depending on the size of your library. Please be patient"
        Write-Host "Copying config"
        Copy-Item -Path $Script:defaultEmbyDataDir/config -Destination $Script:JellyfinDataDir -force -Recurse
        Write-Host "Copying cache"
        Copy-Item -Path $Script:defaultEmbyDataDir/cache -Destination $Script:JellyfinDataDir -force -Recurse
        Write-Host "Copying data"
        Copy-Item -Path $Script:defaultEmbyDataDir/data -Destination $Script:JellyfinDataDir -force -Recurse
        Write-Host "Copying metadata"
        Copy-Item -Path $Script:defaultEmbyDataDir/metadata -Destination $Script:JellyfinDataDir -force -Recurse
        Write-Host "Copying root dir"
        Copy-Item -Path $Script:defaultEmbyDataDir/root -Destination $Script:JellyfinDataDir  -force -Recurse
    }
    $progressbar.Value = 80
    if($Script:CreateShortcut){
        Write-Host "Creating Shortcut"
        $WshShell = New-Object -comObject WScript.Shell
        $Shortcut = $WshShell.CreateShortcut("$Home\Desktop\Jellyfin.lnk")
        $Shortcut.TargetPath = "$Script:DefaultJellyfinInstallDirectory\jellyfin.exe"
        $Shortcut.Save()
    }
    $ProgressBar.Value = 90
    if($Script:StartJellyfin){
        if($Script:InstallAsService){
            Write-Host "Starting Jellyfin Service"
            Get-Service Jellyfin | Start-Service
        }else{
            Write-Host "Starting Jellyfin"
            Start-Process -FilePath $Script:DefaultJellyfinInstallDirectory\jellyfin.exe -PassThru
        }
    }
    $progressbar.Value = 100
    Write-Host Finished
    $wshell = New-Object -ComObject Wscript.Shell
    $wshell.Popup("Operation Completed",0,"Done",0x1)
    $InstallForm.Close()
}
function ServiceBoxCheckChanged {
    if($InstallAsServiceCheck.Checked){
        $Script:InstallAsService         = $true
        $ServiceUserLabel.Visible = $true
        $ServiceUserLabel.Enabled = $true
        $ServiceUserBox.Visible = $true
        $ServiceUserBox.Enabled = $true
    }else{
        $Script:InstallAsService         = $false
        $ServiceUserLabel.Visible = $false
        $ServiceUserLabel.Enabled = $false
        $ServiceUserBox.Visible = $false
        $ServiceUserBox.Enabled = $false
    }
}
function UserSelect {
    if($ServiceUserBox.Text -eq 'Local System')
    {
        $Script:InstallServiceAsUser = $false
         $Script:UserCredentials = $null
         $ServiceUserBox.Items.RemoveAt(1)
         $ServiceUserBox.Items.Add("Custom User")
    }elseif($ServiceUserBox.Text -eq 'Custom User'){
        $Script:InstallServiceAsUser = $true
        $Script:UserCredentials = Get-Credential -Message "Please enter the credentials of the user you with to run Jellyfin Service as" -UserName $env:USERNAME
        $ServiceUserBox.Items[1] = "$($Script:UserCredentials.UserName)"
    }
}
function CreateShortcutBoxCheckChanged {
    if($CreateShortcutCheck.Checked){
        $Script:CreateShortcut = $true
    }else{
        $Script:CreateShortcut = $False
    }
}
function StartJellyFinBoxCheckChanged {
    if($StartProgramCheck.Checked){
        $Script:StartJellyfin = $true
    }else{
        $Script:StartJellyfin = $false
    }
}

function CustomLibraryCheckChanged {
    if($CustomLibraryCheck.Checked){
        $Script:UseCustomLibrary = $true
        $CustomLibraryBox.Enabled = $true
    }else{
        $Script:UseCustomLibrary = $false
        $CustomLibraryBox.Enabled = $false
    }
}

function MigrateLibraryCheckboxChanged {

    if($MigrateLibraryCheck.Checked){
        $Script:MigrateLibrary         = $true
        $LibraryMigrationLabel.Visible = $true
        $LibraryMigrationLabel.Enabled = $true
        $LibraryLocationBox.Visible = $true
        $LibraryLocationBox.Enabled = $true
    }else{
        $Script:MigrateLibrary         = $false
        $LibraryMigrationLabel.Visible = $false
        $LibraryMigrationLabel.Enabled = $false
        $LibraryLocationBox.Visible = $false
        $LibraryLocationBox.Enabled = $false
    }

}


#region begin GUI{ 

$InstallForm                     = New-Object system.Windows.Forms.Form
$InstallForm.ClientSize          = '320,240'
$InstallForm.text                = "Terrible Jellyfin Installer"
$InstallForm.TopMost             = $false

$GUIElementsCollection = @()

$InstallButton                   = New-Object system.Windows.Forms.Button
$InstallButton.text              = "Install"
$InstallButton.width             = 60
$InstallButton.height            = 30
$InstallButton.location          = New-Object System.Drawing.Point(5,5)
$InstallButton.Font              = 'Microsoft Sans Serif,10'
$GUIElementsCollection += $InstallButton

$ProgressBar                     = New-Object system.Windows.Forms.ProgressBar
$ProgressBar.width               = 245
$ProgressBar.height              = 30
$ProgressBar.location            = New-Object System.Drawing.Point(70,5)

$InstallLocationLabel            = New-Object system.Windows.Forms.Label
$InstallLocationLabel.text       = "Install Location"
$InstallLocationLabel.TextAlign  = [System.Drawing.ContentAlignment]::MiddleLeft
$InstallLocationLabel.AutoSize   = $true
$InstallLocationLabel.width      = 100
$InstallLocationLabel.height     = 20
$InstallLocationLabel.location   = New-Object System.Drawing.Point(5,50)
$InstallLocationLabel.Font       = 'Microsoft Sans Serif,10'
$GUIElementsCollection += $InstallLocationLabel

$InstallLocationBox              = New-Object system.Windows.Forms.TextBox
$InstallLocationBox.multiline    = $false
$InstallLocationBox.width        = 205
$InstallLocationBox.height       = 20
$InstallLocationBox.location     = New-Object System.Drawing.Point(110,50)
$InstallLocationBox.Text            = $Script:DefaultJellyfinInstallDirectory
$InstallLocationBox.Font         = 'Microsoft Sans Serif,10'
$GUIElementsCollection += $InstallLocationBox

$CustomLibraryCheck                       = New-Object system.Windows.Forms.CheckBox
$CustomLibraryCheck.text                  = "Custom Library Location:"
$CustomLibraryCheck.TextAlign  = [System.Drawing.ContentAlignment]::MiddleLeft
$CustomLibraryCheck.AutoSize              = $false
$CustomLibraryCheck.width                 = 180
$CustomLibraryCheck.height                = 20
$CustomLibraryCheck.location              = New-Object System.Drawing.Point(5,75)
$CustomLibraryCheck.Font                  = 'Microsoft Sans Serif,10'
$GUIElementsCollection += $CustomLibraryCheck

$CustomLibraryBox              = New-Object system.Windows.Forms.TextBox
$CustomLibraryBox.multiline    = $false
$CustomLibraryBox.width        = 130
$CustomLibraryBox.height       = 20
$CustomLibraryBox.location     = New-Object System.Drawing.Point(185,75)
$CustomLibraryBox.Text            = $Script:JellyFinDataDir
$CustomLibraryBox.Font         = 'Microsoft Sans Serif,10'
$CustomLibraryBox.Enabled      = $false
$GUIElementsCollection += $CustomLibraryBox

$InstallAsServiceCheck                       = New-Object system.Windows.Forms.CheckBox
$InstallAsServiceCheck.text                  = "Install as Service"
$InstallAsServiceCheck.AutoSize              = $false
$InstallAsServiceCheck.width                 = 140
$InstallAsServiceCheck.height                = 20
$InstallAsServiceCheck.location              = New-Object System.Drawing.Point(5,125)
$InstallAsServiceCheck.Font                  = 'Microsoft Sans Serif,10'
$GUIElementsCollection += $InstallAsServiceCheck

$ServiceUserLabel            = New-Object system.Windows.Forms.Label
$ServiceUserLabel.text       = "Run Service As:"
$ServiceUserLabel.AutoSize   = $true
$ServiceUserLabel.TextAlign  = [System.Drawing.ContentAlignment]::MiddleLeft
$ServiceUserLabel.width      = 100
$ServiceUserLabel.height     = 20
$ServiceUserLabel.location   = New-Object System.Drawing.Point(15,145)
$ServiceUserLabel.Font       = 'Microsoft Sans Serif,10'
$ServiceUserLabel.Visible    = $false
$ServiceUserLabel.Enabled    = $false
$GUIElementsCollection += $ServiceUserLabel

$ServiceUserBox                  = New-Object system.Windows.Forms.ComboBox
$ServiceUserBox.text             = "Run Service As"
$ServiceUserBox.width            = 195
$ServiceUserBox.height           = 20
@('Local System','Custom User') | ForEach-Object {[void] $ServiceUserBox.Items.Add($_)}
$ServiceUserBox.location         = New-Object System.Drawing.Point(120,145)
$ServiceUserBox.Font             = 'Microsoft Sans Serif,10'
$ServiceUserBox.Visible          = $false
$ServiceUserBox.Enabled          = $false
$ServiceUserBox.DropDownStyle    = [System.Windows.Forms.ComboBoxStyle]::DropDownList
$GUIElementsCollection += $ServiceUserBox

$MigrateLibraryCheck                       = New-Object system.Windows.Forms.CheckBox
$MigrateLibraryCheck.text                  = "Import Emby/Old JF Library"
$MigrateLibraryCheck.AutoSize              = $false
$MigrateLibraryCheck.width                 = 160
$MigrateLibraryCheck.height                = 20
$MigrateLibraryCheck.location              = New-Object System.Drawing.Point(5,170)
$MigrateLibraryCheck.Font                  = 'Microsoft Sans Serif,10'
$GUIElementsCollection += $MigrateLibraryCheck

$LibraryMigrationLabel            = New-Object system.Windows.Forms.Label
$LibraryMigrationLabel.text       = "Emby/Old JF Library Path"
$LibraryMigrationLabel.TextAlign  = [System.Drawing.ContentAlignment]::MiddleLeft
$LibraryMigrationLabel.AutoSize   = $false
$LibraryMigrationLabel.width      = 120
$LibraryMigrationLabel.height     = 20
$LibraryMigrationLabel.location   = New-Object System.Drawing.Point(15,190)
$LibraryMigrationLabel.Font       = 'Microsoft Sans Serif,10'
$LibraryMigrationLabel.Visible    = $false
$LibraryMigrationLabel.Enabled    = $false
$GUIElementsCollection += $LibraryMigrationLabel

$LibraryLocationBox              = New-Object system.Windows.Forms.TextBox
$LibraryLocationBox.multiline    = $false
$LibraryLocationBox.width        = 175
$LibraryLocationBox.height       = 20
$LibraryLocationBox.location     = New-Object System.Drawing.Point(140,190)
$LibraryLocationBox.Text            = $Script:defaultEmbyDataDir
$LibraryLocationBox.Font         = 'Microsoft Sans Serif,10'
$LibraryLocationBox.Visible      = $false
$LibraryLocationBox.Enabled      = $false
$GUIElementsCollection += $LibraryLocationBox

$CreateShortcutCheck                       = New-Object system.Windows.Forms.CheckBox
$CreateShortcutCheck.text                  = "Desktop Shortcut"
$CreateShortcutCheck.AutoSize              = $false
$CreateShortcutCheck.width                 = 150
$CreateShortcutCheck.height                = 20
$CreateShortcutCheck.location              = New-Object System.Drawing.Point(5,215)
$CreateShortcutCheck.Font                  = 'Microsoft Sans Serif,10'
$GUIElementsCollection += $CreateShortcutCheck

$StartProgramCheck                       = New-Object system.Windows.Forms.CheckBox
$StartProgramCheck.text                  = "Start Jellyfin"
$StartProgramCheck.AutoSize              = $false
$StartProgramCheck.width                 = 160
$StartProgramCheck.height                = 20
$StartProgramCheck.location              = New-Object System.Drawing.Point(160,215)
$StartProgramCheck.Font                  = 'Microsoft Sans Serif,10'
$GUIElementsCollection += $StartProgramCheck

$InstallForm.controls.AddRange($GUIElementsCollection)
$InstallForm.Controls.Add($ProgressBar)

#region gui events {
$InstallButton.Add_Click({ InstallJellyfin })
$CustomLibraryCheck.Add_CheckedChanged({CustomLibraryCheckChanged})
$InstallAsServiceCheck.Add_CheckedChanged({ServiceBoxCheckChanged})
$ServiceUserBox.Add_SelectedValueChanged({ UserSelect })
$MigrateLibraryCheck.Add_CheckedChanged({MigrateLibraryCheckboxChanged})
$CreateShortcutCheck.Add_CheckedChanged({CreateShortcutBoxCheckChanged})
$StartProgramCheck.Add_CheckedChanged({StartJellyFinBoxCheckChanged})
#endregion events }

#endregion GUI }


[void]$InstallForm.ShowDialog()
