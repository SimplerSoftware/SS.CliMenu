// ----------------------------------------------------------------------------------
// Copyright 2019 Simpler Software
// Copyright Microsoft Corporation
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// ----------------------------------------------------------------------------------

using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SS.CliMenu.Metrics.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management.Automation.Host;
using System.Security.Cryptography;
using System.Text;

namespace SS.CliMenu.Metrics
{
    public class MetricHelper
    {
        protected INetworkHelper _networkHelper;
        private const int FlushTimeoutInMilli = 5000;
        private const string DefaultPSVersion = "3.0.0.0";

        /// <summary>
        /// The collection of telemetry clients.
        /// </summary>
        private readonly List<TelemetryClient> _telemetryClients =
            new List<TelemetryClient>();

        /// <summary>
        /// A read-only, thread-safe collection of telemetry clients.  Since
        /// List is only thread-safe for reads (and adding/removing tracing
        /// interceptors isn't a very common operation), we simply replace the
        /// entire collection of interceptors so any enumeration of the list
        /// in progress on a different thread will not be affected by the
        /// change.
        /// </summary>
        private List<TelemetryClient> _threadSafeTelemetryClients =
            new List<TelemetryClient>();

        /// <summary>
        /// Lock used to synchronize mutation of the tracing interceptors.
        /// </summary>
        private readonly object _lock = new object();

        private string _hashMacAddress = string.Empty;

        private PSDataCollectionProfile _profile;

        private static PSHost _host;

        private static string _psVersion;

        protected string PSVersion
        {
            get
            {
                if (_host != null)
                {
                    _psVersion = _host.Version.ToString();
                }
                else
                {
                    _psVersion = DefaultPSVersion;
                }
                return _psVersion;
            }
        }

        public string HashMacAddress
        {
            get
            {
                lock (_lock)
                {
                    if (_hashMacAddress == string.Empty)
                    {
                        _hashMacAddress = null;

                        try
                        {
                            var macAddress = _networkHelper.GetMACAddress();
                            _hashMacAddress = string.IsNullOrWhiteSpace(macAddress)
                                ? null : GenerateSha256HashString(macAddress)?.Replace("-", string.Empty)?.ToLowerInvariant();
                        }
                        catch
                        {
                            // ignore exceptions in getting the network address
                        }
                    }

                    return _hashMacAddress;
                }
            }

            // Add test hook to reset
            set { lock (_lock) { _hashMacAddress = value; } }
        }

        public bool LogPIIData { get; set; }

        public MetricHelper(PSDataCollectionProfile profile) 
            : this(new NetworkHelper())
        {
            _profile = profile;
        }

        public MetricHelper(INetworkHelper network)
        {
            _networkHelper = network;
#if DEBUG
            if (TestMockSupport.RunningMocked)
            {
                TelemetryConfiguration.Active.DisableTelemetry = true;
            }
#endif
        }

        /// <summary>
        /// Gets a sequence of the telemetry clients to notify of changes.
        /// </summary>
        internal IEnumerable<TelemetryClient> TelemetryClients
        {
            get { return _threadSafeTelemetryClients; }
        }

        /// <summary>
        /// Add a telemetry client.
        /// </summary>
        /// <param name="client">The telemetry client.</param>
        public void AddTelemetryClient(TelemetryClient client)
        {
            if (client == null)
            {
                throw new ArgumentNullException("client");
            }

            lock (_lock)
            {
                _telemetryClients.Add(client);
                _threadSafeTelemetryClients = new List<TelemetryClient>(_telemetryClients);
            }
        }

        public void LogQoSEvent(PSQoSEvent qos, bool isUsageMetricEnabled, bool isErrorMetricEnabled)
        {
            if (qos == null || !IsMetricTermAccepted())
            {
                return;
            }

            if (isUsageMetricEnabled)
            {
                LogUsageEvent(qos);
            }

            if (isErrorMetricEnabled && qos.Exception != null)
            {
                LogExceptionEvent(qos);
            }
        }
        public void LogPerfEvent(PSQoSEvent qos, bool isUsageMetricEnabled, bool isErrorMetricEnabled)
        {
            if (qos == null || !IsMetricTermAccepted())
            {
                return;
            }

            if (isUsageMetricEnabled)
            {
                LogPerfEvent(qos);
            }

            if (isErrorMetricEnabled && qos.Exception != null)
            {
                LogExceptionEvent(qos);
            }
        }

        public void LogCustomEvent<T>(string eventName, T payload, bool force = false)
        {
            if (payload == null || (!force && !IsMetricTermAccepted()))
            {
                return;
            }

            foreach (TelemetryClient client in TelemetryClients)
            {
                client.TrackEvent(eventName, SerializeCustomEventPayload(payload));
            }
        }

        private void LogUsageEvent(PSQoSEvent qos)
        {
            if (qos != null)
            {
                foreach (TelemetryClient client in TelemetryClients)
                {
                    var pageViewTelemetry = new PageViewTelemetry(qos.Name)
                    {
                        Id = qos.Id,
                        Duration = qos.Duration,
                        Timestamp = qos.StartTime
                    };
                    LoadTelemetryClientContext(qos, pageViewTelemetry.Context);
                    PopulatePropertiesFromQos(qos, pageViewTelemetry.Properties);

                    #region User-Agent work around
                    Microsoft.ApplicationInsights.Channel.Transmission trans = new Microsoft.ApplicationInsights.Channel.Transmission(new Uri("http://www.contoso.com/"), new byte[0], "", "");
                    Type type = typeof(Microsoft.ApplicationInsights.Channel.Transmission);
                    System.Reflection.FieldInfo info = type.GetField("client", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                    System.Net.Http.HttpClient value = info.GetValue(null) as System.Net.Http.HttpClient;
                    if (!value.DefaultRequestHeaders.UserAgent.Contains(qos.UserAgent))
                    {
                        value.DefaultRequestHeaders.UserAgent.Clear();
                        value.DefaultRequestHeaders.UserAgent.Add(qos.UserAgent);
                    }
                    #endregion

                    client.TrackPageView(pageViewTelemetry);
                    //var perf = new PageViewPerformanceTelemetry(qos.Name)
                    //{
                    //    Id = qos.Id,
                    //    DomProcessing = qos.ProcessingTime,
                    //    PerfTotal = qos.ProcessingTime,
                    //    Timestamp = qos.StartTime,
                    //};
                    //LoadTelemetryClientContext(qos, perf.Context);
                    //PopulatePropertiesFromQos(qos, perf.Properties);
                    //client.Track(perf);
                }
            }
        }

        private void LogPerfEvent(PSQoSEvent qos)
        {
            if (qos != null)
            {
                foreach (TelemetryClient client in TelemetryClients)
                {
                    var perf = new PageViewPerformanceTelemetry(qos.Name)
                    {
                        Id = qos.Id,
                        DomProcessing = qos.ProcessingTime,
                        PerfTotal = qos.ProcessingTime,
                        Timestamp = qos.StartTime
                    };
                    LoadTelemetryClientContext(qos, perf.Context);
                    PopulatePropertiesFromQos(qos, perf.Properties);

                    #region User-Agent work around
                    Microsoft.ApplicationInsights.Channel.Transmission trans = new Microsoft.ApplicationInsights.Channel.Transmission(new Uri("http://www.contoso.com/"), new byte[0], "", "");
                    Type type = typeof(Microsoft.ApplicationInsights.Channel.Transmission);
                    System.Reflection.FieldInfo info = type.GetField("client", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                    System.Net.Http.HttpClient value = info.GetValue(null) as System.Net.Http.HttpClient;
                    if (!value.DefaultRequestHeaders.UserAgent.Contains(qos.UserAgent))
                    {
                        value.DefaultRequestHeaders.UserAgent.Clear();
                        value.DefaultRequestHeaders.UserAgent.Add(qos.UserAgent);
                    }
                    #endregion

                    client.Track(perf);
                }
            }
        }

        private void LogExceptionEvent(PSQoSEvent qos)
        {
            if (qos == null || qos.Exception == null)
            {
                return;
            }

            Dictionary<string, double> eventMetrics = new Dictionary<string, double>();
            eventMetrics.Add("Duration", qos.Duration.TotalMilliseconds);

            foreach (TelemetryClient client in TelemetryClients)
            {
                Dictionary<string, string> eventProperties = new Dictionary<string, string>();
                LoadTelemetryClientContext(qos, client.Context);
                PopulatePropertiesFromQos(qos, eventProperties);
                // qos.Exception contains exception message which may contain Users specific data. 
                // We should not collect users specific data unless told to.
                if (LogPIIData)
                    eventProperties.Add("Message", qos.Exception.Message);
                else
                    eventProperties.Add("Message", "Message removed due to PII.");
                eventProperties.Add("StackTrace", qos.Exception.StackTrace);
                eventProperties.Add("ExceptionType", qos.Exception.GetType().ToString());
                Exception innerEx = qos.Exception.InnerException;
                int exceptionCount = 0;
                //keep going till we get to the last inner exception
                while (innerEx != null)
                {
                    //Increment the inner exception count so that we can tell which is the outermost
                    //and which the innermost
                    eventProperties.Add("InnerExceptionType-" + exceptionCount++, innerEx.GetType().ToString());
                    innerEx = innerEx.InnerException;
                }
                client.TrackException(null, eventProperties, eventMetrics);
            }
        }

        private void LoadTelemetryClientContext(PSQoSEvent qos, TelemetryContext clientContext)
        {
            if (clientContext != null && qos != null)
            {
                clientContext.Device.Type = "Browser";
                clientContext.User.Id = qos.Uid;
                clientContext.User.AuthenticatedUserId = qos.AuthenticatedUserId;
                clientContext.User.AccountId = qos.AccountId;
                // This is not used for some reason, will create issue with MS on Github
                //clientContext.User.UserAgent = qos.UserAgent ?? SSPowerShell.UserAgentValue;
                clientContext.Component.Version = qos.AppVersion;
                clientContext.Session.Id = qos.SessionId;
                clientContext.Device.OperatingSystem = Environment.OSVersion.ToString();
                clientContext.Cloud.RoleName = qos.RoleName;
            }
        }

        public void SetPSHost(PSHost host)
        {
            _host = host;
        }

        private void PopulatePropertiesFromQos(PSQoSEvent qos, IDictionary<string, string> eventProperties)
        {
            if (qos == null)
            {
                return;
            }

            eventProperties.Add("Title", qos.Title);
            eventProperties.Add("Selected", qos.Selected);
            eventProperties.Add("Command", qos.CommandName);
            eventProperties.Add("IsSuccess", qos.IsSuccess.ToString());
            eventProperties.Add("ModuleName", qos.ModuleName);
            eventProperties.Add("ModuleVersion", qos.ModuleVersion);
            eventProperties.Add("HostVersion", qos.HostVersion);
            eventProperties.Add("CommandParameters", qos.Parameters);
            eventProperties.Add("x-client-request-id", qos.ClientRequestId);
            eventProperties.Add("HashMacAddress", HashMacAddress);
            eventProperties.Add("PowerShellVersion", PSVersion);
            eventProperties.Add("MenuVersion", SSPowerShell.AssemblyVersion);
            eventProperties.Add("CommandParameterSetName", qos.ParameterSetName);
            eventProperties.Add("CommandInvocationName", qos.InvocationName);

            if (qos.InputFromPipeline != null)
            {
                eventProperties.Add("InputFromPipeline", qos.InputFromPipeline.Value.ToString());
            }
            if (qos.OutputToPipeline != null)
            {
                eventProperties.Add("OutputToPipeline", qos.OutputToPipeline.Value.ToString());
            }
            foreach (var key in qos.CustomProperties.Keys)
            {
                eventProperties[key] = qos.CustomProperties[key];
            }
        }

        public bool IsMetricTermAccepted()
        {
            return  _profile != null
                && _profile.EnableDataCollection.HasValue
                && _profile.EnableDataCollection.Value;
        }

        public void FlushMetric()
        {
            if (!IsMetricTermAccepted())
            {
                return;
            }

            try
            {
                foreach (TelemetryClient client in TelemetryClients)
                {
                    client.Flush();
                }
            }
            catch
            {
                // ignored
            }
        }

        /// <summary>
        /// Generate a SHA256 Hash string from the originInput.
        /// </summary>
        /// <param name="originInput"></param>
        /// <returns>The Sha256 hash, or empty if the input is only whitespace</returns>
        public static string GenerateSha256HashString(string originInput)
        {
            if (string.IsNullOrWhiteSpace(originInput))
            {
                return string.Empty;
            }

            string result = null;
            try
            {
                using (var sha256 = new SHA256CryptoServiceProvider())
                {
                    var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(originInput));
                    result = BitConverter.ToString(bytes);
                }
            }
            catch
            {
                // do not throw if CryptoProvider is not provided
            }

            return result;
        }

        /// <summary>
        /// Generate a serialized payload for custom events.
        /// </summary>
        /// <param name="payload">The payload object for the custom event.</param>
        /// <returns>The serialized payload.</returns>
        public static Dictionary<string, string> SerializeCustomEventPayload<T>(T payload)
        {
            var payloadAsJson = JsonConvert.SerializeObject(payload, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });

            return JsonConvert.DeserializeObject<Dictionary<string, string>>(payloadAsJson);
        }
    }
}