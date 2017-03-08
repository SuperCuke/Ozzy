﻿using Newtonsoft.Json;

namespace Ozzy.Server.FeatureFlags
{
    public class FeatureFlagConfiguration
    {
        public FeatureFlagConfiguration(bool isEnabled = false)
        {
            IsEnabled = isEnabled;
        }

        [JsonProperty]
        public bool IsEnabled { get; private set; }
    }
}
