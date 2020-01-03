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

using Newtonsoft.Json;
using System;
using System.IO;

namespace SS.CliMenu.Metrics
{
    public abstract class DataCollectionController
    {
        public const string RegistryKey = "DataCollectionController";
        public abstract PSDataCollectionProfile GetProfile(Action warningWriter);

        static PSDataCollectionProfile Initialize(string product, bool ignoreError = true)
        {
            PSDataCollectionProfile result = new PSDataCollectionProfile();
            try
            {
                var environmentValue = Environment.GetEnvironmentVariable(PSDataCollectionProfile.EnvironmentVariableName);
                bool enabled = true;
                if (!string.IsNullOrWhiteSpace(environmentValue) && bool.TryParse(environmentValue, out enabled))
                {
                    result.EnableDataCollection = enabled;
                }
                else
                {
                    var store = new DiskDataStore();
                    string dataPath = Path.Combine(SSPowerShell.ProfileDirectory, product, PSDataCollectionProfile.DefaultFileName);
                    if (store.FileExists(dataPath))
                    {
                        string contents = store.ReadFileAsText(dataPath);
                        var localResult = JsonConvert.DeserializeObject<PSDataCollectionProfile>(contents);
                        if (localResult != null && localResult.EnableDataCollection.HasValue)
                        {
                            result = localResult;
                        }
                    }
                    else
                    {
                        WritePSDataCollectionProfile(product, new PSDataCollectionProfile(true));
                    }
                }
            }
            catch
            {
                // do not throw for i/o or serialization errors
                if (!ignoreError)
                    throw;
            }

            return result;
        }

        public static void WritePSDataCollectionProfile(string product, PSDataCollectionProfile profile)
        {
            try
            {
                var store = new DiskDataStore();
                string dataPath = Path.Combine(SSPowerShell.ProfileDirectory, product, PSDataCollectionProfile.DefaultFileName);
                if (!store.DirectoryExists(Path.Combine(SSPowerShell.ProfileDirectory, product)))
                {
                    store.CreateDirectory(Path.Combine(SSPowerShell.ProfileDirectory, product));
                }

                string contents = JsonConvert.SerializeObject(profile);
                store.WriteFile(dataPath, contents);
            }
            catch
            {
                // do not throw for i/o or serialization errors
            }
        }

        public static DataCollectionController Create(string product)
        {
            return new MemoryDataCollectionController(Initialize(product));
        }
        public static bool TryCreate(string product, out DataCollectionController controller)
        {
            try
            {
                controller = new MemoryDataCollectionController(Initialize(product, false));
                return true;
            }
            catch
            {
                controller = null;
                return false;
            }
        }

        public static DataCollectionController Create(PSDataCollectionProfile profile)
        {
            return new MemoryDataCollectionController(profile);
        }

        class MemoryDataCollectionController : DataCollectionController
        {
            object _lock;
            bool? _enabled;

            public MemoryDataCollectionController()
            {
                _lock = new object();
                _enabled = null;
            }

            public MemoryDataCollectionController(PSDataCollectionProfile enabled)
            {
                _lock = new object();
                _enabled = enabled?.EnableDataCollection;
            }

            public override PSDataCollectionProfile GetProfile(Action warningWriter)
            {
                lock (_lock)
                {
                    if (!_enabled.HasValue)
                    {
                        _enabled = true;
                        warningWriter();
                    }

                    return new PSDataCollectionProfile(_enabled.Value);
                }
            }
        }
    }
}