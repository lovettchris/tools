﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.34006
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace OutlookSync.Properties {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("OutlookSync.Properties.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Your Windows Firewall settings appear to be missing a rule to allow TCP and UDP network connections to this app over your private network. .
        /// </summary>
        internal static string FirewallNotRight {
            get {
                return ResourceManager.GetString("FirewallNotRight", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to When this app is first launched you need to check both boxes on the Windows Firewall Security Alert dialog allowing Private and Public networks then click &quot;Allow Access&quot;.  If you already dismissed the dialog then you will need to click the link below..
        /// </summary>
        internal static string FirstLaunchFirewallSettings {
            get {
                return ResourceManager.GetString("FirstLaunchFirewallSettings", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Click here to fix your firewall..
        /// </summary>
        internal static string FixFirewallLinkCaption {
            get {
                return ResourceManager.GetString("FixFirewallLinkCaption", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Loaded {0} contacts from Outlook.
        /// </summary>
        internal static string LoadedCount {
            get {
                return ResourceManager.GetString("LoadedCount", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Checking Outlook for changes....
        /// </summary>
        internal static string LoadingOutlookContacts {
            get {
                return ResourceManager.GetString("LoadingOutlookContacts", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Your computer does not appear to have any network available, so it will not be able to communicate with your phone over wifi..
        /// </summary>
        internal static string NoNetwork {
            get {
                return ResourceManager.GetString("NoNetwork", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Outlook does not appear to be installed on this machine.  .
        /// </summary>
        internal static string NoOutlook {
            get {
                return ResourceManager.GetString("NoOutlook", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to All TCP ports are being used, please close any other instances of this program..
        /// </summary>
        internal static string NoPortsAvailable {
            get {
                return ResourceManager.GetString("NoPortsAvailable", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Your phone is running version {0} and your Outlook Sync Server version is {1}.  Please check to see if there is an update available for the app on your phone..
        /// </summary>
        internal static string PhoneUpdateRequired {
            get {
                return ResourceManager.GetString("PhoneUpdateRequired", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Congratulations your phone is now in sync!.
        /// </summary>
        internal static string SyncComplete {
            get {
                return ResourceManager.GetString("SyncComplete", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to An unknown networking error has occured, the reason is &apos;{0}&apos;.
        /// </summary>
        internal static string UnknownNetworkingError {
            get {
                return ResourceManager.GetString("UnknownNetworkingError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Your phone is running version {0} and your Outlook Sync Server version is {1} and no new update for Outlook Sync Server seems to be available yet.  Please contact chris@lovettsoftware.com for a suggested fix..
        /// </summary>
        internal static string UpdateMissing {
            get {
                return ResourceManager.GetString("UpdateMissing", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Your phone is running a newer version, and there is a matching update for Outlook Sync Server.  Please restart this app to install the update..
        /// </summary>
        internal static string UpdateRequired {
            get {
                return ResourceManager.GetString("UpdateRequired", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Waiting for new phone app version.
        /// </summary>
        internal static string WaitingForNewPhoneVersion {
            get {
                return ResourceManager.GetString("WaitingForNewPhoneVersion", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Waiting for you to launch Outlook Sync on your phone.
        /// </summary>
        internal static string WaitingForPhone {
            get {
                return ResourceManager.GetString("WaitingForPhone", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Waiting for you to restart this app.
        /// </summary>
        internal static string WaitingForRestart {
            get {
                return ResourceManager.GetString("WaitingForRestart", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Waiting for app update from lovettsoftware.com.
        /// </summary>
        internal static string WaitingForUpdate {
            get {
                return ResourceManager.GetString("WaitingForUpdate", resourceCulture);
            }
        }
    }
}
