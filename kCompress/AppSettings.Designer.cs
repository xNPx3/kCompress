﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace kCompress {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "17.10.0.0")]
    internal sealed partial class AppSettings : global::System.Configuration.ApplicationSettingsBase {
        
        private static AppSettings defaultInstance = ((AppSettings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new AppSettings())));
        
        public static AppSettings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public string FFmpegPath {
            get {
                return ((string)(this["FFmpegPath"]));
            }
            set {
                this["FFmpegPath"] = value;
            }
        }
    }
}
