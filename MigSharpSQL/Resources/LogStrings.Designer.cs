﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.18444
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace MigSharpSQL.Resources
{


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
    internal class LogStrings {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal LogStrings() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("MigSharpSQL.Resources.LogStrings", typeof(LogStrings).Assembly);
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
        ///   Looks up a localized string similar to Cannot fetch the current state: {0}..
        /// </summary>
        internal static string CannotFetchCurrentState {
            get {
                return ResourceManager.GetString("CannotFetchCurrentState", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Current state: {0}.
        /// </summary>
        internal static string CurrentState {
            get {
                return ResourceManager.GetString("CurrentState", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The current database state is {0}. The substate is {1}..
        /// </summary>
        internal static string DbStateSubstateInfo {
            get {
                return ResourceManager.GetString("DbStateSubstateInfo", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Figuring out the current database state..
        /// </summary>
        internal static string FiguringOutCurrentDbState {
            get {
                return ResourceManager.GetString("FiguringOutCurrentDbState", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Loading script: {0}..
        /// </summary>
        internal static string LoadingScript {
            get {
                return ResourceManager.GetString("LoadingScript", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Migration completed successfully..
        /// </summary>
        internal static string MigrationCompletedSuccefully {
            get {
                return ResourceManager.GetString("MigrationCompletedSuccefully", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Migration started..
        /// </summary>
        internal static string MigrationStarted {
            get {
                return ResourceManager.GetString("MigrationStarted", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Moving database to state {0}, substate {1}..
        /// </summary>
        internal static string MovingDbState {
            get {
                return ResourceManager.GetString("MovingDbState", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The database is already at specified state. No action required..
        /// </summary>
        internal static string NoActionForTheSameState {
            get {
                return ResourceManager.GetString("NoActionForTheSameState", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Performing the downgrading scripts {0}...{1} has been started..
        /// </summary>
        internal static string PerformingDowngrade {
            get {
                return ResourceManager.GetString("PerformingDowngrade", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Performing the upgrading scripts {0}...{1} has been started..
        /// </summary>
        internal static string PerformingUpgrade {
            get {
                return ResourceManager.GetString("PerformingUpgrade", resourceCulture);
            }
        }
    }
}
