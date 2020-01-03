using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;

namespace SS.CliMenu.Models
{
    /// <summary>
    /// Metric data object used for metric logging.
    /// <para type="description">Metric data object used for metric logging.</para>
    /// </summary>
    public class MetricData
    {
        private ProductInfoHeaderValue userAgent = null;

        /// <summary>
        /// Default constructor, sets default values.
        /// </summary>
        public MetricData()
        {
            if (!string.IsNullOrWhiteSpace(Environment.UserName))
                UserId = Environment.UserName;
            else
                UserId = "defaultid";
        }

        /// <summary>
        /// The name of the user/company that owns the Azure App Insight account used for metric logging. Used in the data collection warning.
        /// </summary>
        public string DataCollectedBy { get; set; } = "the user/company who created this menu, and not the SS.CliMenu developers.";
        /// <summary>
        /// The Azure App Insight key to use for metric logging.
        /// </summary>
        public string CollectionKey { get; set; }
        /// <summary>
        /// Allow collecting PII user information. (Usually only enabled when used for internal company logging) Default: false
        /// </summary>
        public bool LogPIIData { get; set; } = false;
        /// <summary>
        /// The unique name of the product menu. This is used to store local settings for users in their AppData folder.
        /// </summary>
        public string Product { get; set; } = ".Default";
        /// <summary>
        /// UserId used for metric logging. If <see cref="LogPIIData"/> is false, this value will be hashed automatically.
        /// </summary>
        /// <remarks>Default to Environment.UserName, else 'defaultid' if Environment.UserName is empty.</remarks>
        public string UserId { get; set; }
        /// <summary>
        /// Module's name to report in metric logging
        /// </summary>
        public string ModuleName { get; set; }
        /// <summary>
        /// Module's version to report in metric logging
        /// </summary>
        public string ModuleVersion { get; set; }
        /// <summary>
        /// UserAgent to report in metric logging
        /// </summary>
        public ProductInfoHeaderValue UserAgent { get => userAgent ?? (string.IsNullOrWhiteSpace(ModuleName) || string.IsNullOrWhiteSpace(ModuleVersion) ? null : new ProductInfoHeaderValue(ModuleName, ModuleVersion)); set => userAgent = value; }
        /// <summary>
        /// A AccountId to report in metric logging
        /// </summary>
        public string AccountId { get; set; }
        /// <summary>
        /// The readable value for the user, a value that is easily recognizable like a email or user name. This value is not logged unless <see cref="LogPIIData"/> is set to true.
        /// </summary>
        public string AuthenticatedUserId { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string AppVersion { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string RoleName { get; set; }
        /// <summary>
        /// The amount of time it took to build the menu. Ex: Like a web page, how much time did it take to pull all the data and prepare the menu for display.
        /// </summary>
        public TimeSpan ProcessingTime { get; set; } = TimeSpan.Zero;
    }
}
