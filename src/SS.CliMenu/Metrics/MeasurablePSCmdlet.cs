using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Management.Automation;
using System.Text;
using System.Threading;

namespace SS.CliMenu.Metrics
{
    public abstract class MeasurablePSCmdlet : PSCmdlet, IDisposable
    {
        public ConcurrentQueue<string> DebugMessages { get; private set; }

        private object lockObject = new object();
        private PSDataCollectionProfile _cachedProfile = null;

        protected PSDataCollectionProfile _dataCollectionProfile
        {
            get
            {
                lock (lockObject)
                {
                    DataCollectionController controller;
                    if (_cachedProfile == null && DataCollectionController.TryCreate(ProductName, out controller))
                    {
                        _cachedProfile = controller.GetProfile(() => WriteWarning(DataCollectionWarning));
                    }
                    else if (_cachedProfile == null)
                    {
                        _cachedProfile = new PSDataCollectionProfile(true);
                        if (!string.IsNullOrWhiteSpace(DataCollectionWarning))
                            WriteWarning(DataCollectionWarning);
                    }

                    return _cachedProfile;
                }
            }
            set
            {
                lock (lockObject)
                {
                    _cachedProfile = value;
                }
            }
        }

        protected static string _errorRecordFolderPath = null;
        protected static string _sessionId = Guid.NewGuid().ToString();
        protected const string _fileTimeStampSuffixFormat = "yyyy-MM-dd-THH-mm-ss-fff";
        protected string _clientRequestId = Guid.NewGuid().ToString();
        protected MetricHelper _metricHelper;
        protected PSQoSEvent _qosEvent;

        protected virtual bool IsUsageMetricEnabled
        {
            get { return true; }
        }

        protected virtual bool IsErrorMetricEnabled
        {
            get { return true; }
        }

        /// <summary>
        /// Check whether the data collection is opted in from user
        /// </summary>
        /// <returns>true if allowed</returns>
        public bool IsDataCollectionAllowed()
        {
            if (_dataCollectionProfile != null &&
                _dataCollectionProfile.EnableDataCollection.HasValue &&
                _dataCollectionProfile.EnableDataCollection.Value)
            {
                return true;
            }

            return false;
        }

        public new SessionState SessionState { get; set; }

        protected abstract string DataCollectionWarning { get; }
        protected abstract string InstrumentationKey { get; }
        /// <summary>
        /// The unique name of the product menu. This is used to store local settings for users in their AppData folder.
        /// </summary>
        protected abstract string ProductName { get; }

        public MeasurablePSCmdlet()
        {
            DebugMessages = new ConcurrentQueue<string>();
        }

        protected abstract void InitializeQosEvent();

        /// <summary>
        /// Cmdlet begin process. Write to logs, setup Http Tracing and initialize profile
        /// </summary>
        protected override void BeginProcessing()
        {
            SessionState = base.SessionState;
            var profile = _dataCollectionProfile;
            lock (lockObject)
            {
                if (_metricHelper == null)
                {
                    _metricHelper = new MetricHelper(profile);
                    if (!string.IsNullOrWhiteSpace(this.InstrumentationKey))
                    {
                        var config = TelemetryConfiguration.CreateDefault();
                        config.InstrumentationKey = this.InstrumentationKey;
                        //TODO: Look into adding ITelemetryInitializer for static data between all logging
                        // https://docs.microsoft.com/en-us/azure/azure-monitor/app/usage-overview
                        _metricHelper.AddTelemetryClient(new TelemetryClient(config));
                    }
                }
            }

            InitializeQosEvent();
            LogCmdletStartInvocationInfo();
            base.BeginProcessing();
        }

        public virtual void ExecuteCmdlet()
        {
            // Do nothing.
        }

        protected override void ProcessRecord()
        {
            try
            {
                base.ProcessRecord();
                this.ExecuteCmdlet();
            }
            catch (Exception ex) when (!IsTerminatingError(ex))
            {
                WriteExceptionError(ex);
            }
        }

        /// <summary>
        /// Perform end of pipeline processing.
        /// </summary>
        protected override void EndProcessing()
        {
            LogQosEvent();
            LogCmdletEndInvocationInfo();
            base.EndProcessing();
        }

        /// <summary>
        /// Invoke this method when the cmdlet is completed or terminated.
        /// </summary>
        protected void LogQosEvent()
        {
            if (_qosEvent == null)
            {
                return;
            }

            _qosEvent.FinishQosEvent();

            if (!IsUsageMetricEnabled && (!IsErrorMetricEnabled || _qosEvent.IsSuccess))
            {
                return;
            }

            if (!IsDataCollectionAllowed())
            {
                return;
            }

            WriteDebug(_qosEvent.ToString());

            try
            {
                _metricHelper.SetPSHost(this.Host);
                _metricHelper.LogQoSEvent(_qosEvent, IsUsageMetricEnabled, IsErrorMetricEnabled);
                _metricHelper.FlushMetric();
                WriteDebug("Finish sending metric.");
            }
            catch (Exception e)
            {
                //Swallow error from Application Insights event collection.
                WriteWarning(e.ToString());
            }
        }

        /// <summary>
        /// Invoke this method when the cmdlet is completed processing menu setup/build.
        /// </summary>
        protected void LogPerfEvent(TimeSpan processingTime)
        {
            if (_qosEvent == null)
            {
                return;
            }

            if (!IsUsageMetricEnabled && (!IsErrorMetricEnabled || _qosEvent.IsSuccess))
            {
                return;
            }

            if (!IsDataCollectionAllowed())
            {
                return;
            }

            WriteDebug(_qosEvent.ToString());

            try
            {
                _qosEvent.ProcessingTime = processingTime;
                _metricHelper.SetPSHost(this.Host);
                _metricHelper.LogPerfEvent(_qosEvent, IsUsageMetricEnabled, IsErrorMetricEnabled);
                _metricHelper.FlushMetric();
                WriteDebug("Finish sending performance metric.");
            }
            catch (Exception e)
            {
                //Swallow error from Application Insights event collection.
                WriteWarning(e.ToString());
            }
        }

        protected virtual void LogCmdletStartInvocationInfo()
        {
            if (string.IsNullOrEmpty(ParameterSetName))
            {
                WriteDebugWithTimestamp(string.Format(CultureInfo.CurrentCulture, "{0} begin processing without ParameterSet.", this.GetType().Name));
            }
            else
            {
                WriteDebugWithTimestamp(string.Format(CultureInfo.CurrentCulture, "{0} begin processing with ParameterSet '{1}'.", this.GetType().Name, ParameterSetName));
            }
        }

        protected virtual void LogCmdletEndInvocationInfo()
        {
            string message = string.Format(CultureInfo.CurrentCulture, "{0} end processing.", this.GetType().Name);
            WriteDebugWithTimestamp(message);
        }

        protected new void WriteError(ErrorRecord errorRecord)
        {
            FlushDebugMessages(IsDataCollectionAllowed());
            if (_qosEvent != null && errorRecord != null)
            {
                _qosEvent.Exception = errorRecord.Exception;
                _qosEvent.IsSuccess = false;
            }

            base.WriteError(errorRecord);
        }

        protected new void ThrowTerminatingError(ErrorRecord errorRecord)
        {
            FlushDebugMessages(IsDataCollectionAllowed());
            base.ThrowTerminatingError(errorRecord);
        }

        protected new void WriteObject(object sendToPipeline)
        {
            FlushDebugMessages();
            base.WriteObject(sendToPipeline);
        }

        protected new void WriteObject(object sendToPipeline, bool enumerateCollection)
        {
            FlushDebugMessages();
            base.WriteObject(sendToPipeline, enumerateCollection);
        }

        protected new void WriteVerbose(string text)
        {
            FlushDebugMessages();
            base.WriteVerbose(text);
        }

        protected new void WriteWarning(string text)
        {
            FlushDebugMessages();
            base.WriteWarning(text);
        }

        protected new void WriteCommandDetail(string text)
        {
            FlushDebugMessages();
            base.WriteCommandDetail(text);
        }

        protected new void WriteProgress(ProgressRecord progressRecord)
        {
            FlushDebugMessages();
            base.WriteProgress(progressRecord);
        }

        protected new void WriteDebug(string text)
        {
            FlushDebugMessages();
            base.WriteDebug(text);
        }

        protected void WriteVerboseWithTimestamp(string message, params object[] args)
        {
            if (CommandRuntime != null)
            {
                WriteVerbose(string.Format(CultureInfo.CurrentCulture, "{0:T} - {1}", DateTime.Now, string.Format(CultureInfo.CurrentCulture, message, args)));
            }
        }

        protected void WriteVerboseWithTimestamp(string message)
        {
            if (CommandRuntime != null)
            {
                WriteVerbose(string.Format(CultureInfo.CurrentCulture, "{0:T} - {1}", DateTime.Now, message));
            }
        }

        protected void WriteWarningWithTimestamp(string message)
        {
            if (CommandRuntime != null)
            {
                WriteWarning(string.Format(CultureInfo.CurrentCulture, "{0:T} - {1}", DateTime.Now, message));
            }
        }

        protected void WriteDebugWithTimestamp(string message, params object[] args)
        {
            if (CommandRuntime != null)
            {
                WriteDebug(string.Format(CultureInfo.CurrentCulture, "{0:T} - {1}", DateTime.Now, string.Format(CultureInfo.CurrentCulture, message, args)));
            }
        }

        protected void WriteDebugWithTimestamp(string message)
        {
            if (CommandRuntime != null)
            {
                WriteDebug(string.Format(CultureInfo.CurrentCulture, "{0:T} - {1}", DateTime.Now, message));
            }
        }

        protected void WriteErrorWithTimestamp(string message)
        {
            if (CommandRuntime != null)
            {
                WriteError(
                new ErrorRecord(new Exception(string.Format(CultureInfo.CurrentCulture, "{0:T} - {1}", DateTime.Now, message)),
                string.Empty,
                ErrorCategory.NotSpecified,
                null));
            }
        }

        /// <summary>
        /// Write an error message for a given exception.
        /// </summary>
        /// <param name="ex">The exception resulting from the error.</param>
        protected virtual void WriteExceptionError(Exception ex)
        {
            Debug.Assert(ex != null, "ex cannot be null or empty.");
            WriteError(new ErrorRecord(ex, string.Empty, ErrorCategory.CloseError, null));
        }
        private void FlushDebugMessages(bool record = false)
        {
            if (record)
            {
                RecordDebugMessages();
            }

            string message;
            while (DebugMessages.TryDequeue(out message))
            {
                base.WriteDebug(message);
            }
        }

        private void RecordDebugMessages()
        {
            try
            {
                // Create 'ErrorRecords' folder under profile directory, if not exists
                if (string.IsNullOrEmpty(_errorRecordFolderPath)
                    || !Directory.Exists(_errorRecordFolderPath))
                {
                    _errorRecordFolderPath = Path.Combine(SSPowerShell.ProfileDirectory, "ErrorRecords");
                    Directory.CreateDirectory(_errorRecordFolderPath);
                }

                CommandInfo cmd = this.MyInvocation.MyCommand;

                string filePrefix = cmd.Name;
                string timeSampSuffix = DateTime.Now.ToString(_fileTimeStampSuffixFormat, CultureInfo.CurrentCulture);
                string fileName = filePrefix + "_" + timeSampSuffix + ".log";
                string filePath = Path.Combine(_errorRecordFolderPath, fileName);

                StringBuilder sb = new StringBuilder();
                sb.Append("Module : ").AppendLine(cmd.ModuleName);
                sb.Append("Cmdlet : ").AppendLine(cmd.Name);

                sb.AppendLine("Parameters");
                foreach (var item in this.MyInvocation.BoundParameters)
                {
                    sb.Append(" -").Append(item.Key).Append(" : ");
                    sb.AppendLine(item.Value == null ? "null" : item.Value.ToString());
                }

                sb.AppendLine();

                foreach (var content in DebugMessages)
                {
                    sb.AppendLine(content);
                }

                //AzureSession.Instance.DataStore.WriteFile(filePath, sb.ToString());
            }
            catch
            {
                // do not throw an error if recording debug messages fails
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            try
            {
                FlushDebugMessages();
            }
            catch { }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public virtual bool IsTerminatingError(Exception ex)
        {
            var pipelineStoppedEx = ex as PipelineStoppedException;
            if (pipelineStoppedEx != null && pipelineStoppedEx.InnerException == null)
            {
                return true;
            }

            return false;
        }

    }
}
