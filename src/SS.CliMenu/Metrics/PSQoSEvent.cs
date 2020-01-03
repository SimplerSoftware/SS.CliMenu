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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http.Headers;

public class PSQoSEvent
{
    private readonly Stopwatch _timer;

    public DateTimeOffset StartTime { get; set; }
    public TimeSpan Duration { get; set; }
    public TimeSpan ProcessingTime { get; set; }
    public bool IsSuccess { get; set; }
    public string Id { get; set; }
    public string Name { get; set; }
    public string Title { get; set; }
    public string CommandName { get; set; }
    public string ModuleName { get; set; }
    public string ModuleVersion { get; set; }
    public string HostVersion { get; set; }
    public string Parameters { get; set; }
    public bool? InputFromPipeline { get; set; }
    public bool? OutputToPipeline { get; set; }
    public Exception Exception { get; set; }
    public string Uid { get; set; }
    public string ClientRequestId { get; set; }
    public string SessionId { get; set; }
    public string ParameterSetName { get; set; }
    public string InvocationName { get; set; }
    public Dictionary<string, string> CustomProperties { get; private set; }
    public string Selected { get; internal set; }
    public string AccountId { get; internal set; }
    public string AuthenticatedUserId { get; internal set; }
    public string AppVersion { get; internal set; }
    public ProductInfoHeaderValue UserAgent { get; internal set; }
    public string RoleName { get; internal set; }

    public PSQoSEvent()
    {
        StartTime = DateTimeOffset.Now;
        _timer = new Stopwatch();
        _timer.Start();
        CustomProperties = new Dictionary<string, string>();
    }

    public void PauseQoSTimer()
    {
        _timer.Stop();
    }

    public void ResumeQosTimer()
    {
        _timer.Start();
    }

    public void FinishQosEvent()
    {
        _timer.Stop();
        Duration = _timer.Elapsed;
    }

    public override string ToString()
    {
        return string.Format(
            "QoSEvent: CommandName - {0}; IsSuccess - {1}; Duration - {2}; Exception - {3};",
            CommandName, IsSuccess, Duration, Exception);
    }
}