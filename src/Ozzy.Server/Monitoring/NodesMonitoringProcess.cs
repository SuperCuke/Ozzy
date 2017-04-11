﻿using Microsoft.AspNetCore.Builder;
using Ozzy.Core;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Ozzy.DomainModel.Monitoring;
using Ozzy.Server.BackgroundProcesses;

namespace Ozzy.Server.Monitoring
{
    public class NodesMonitoringProcess : PeriodicActionProcess
    {
        private INodesManager _monitoringManager;
        private IServiceProvider _serviceProvider;
        public NodesMonitoringProcess(INodesManager monitoringManager, IServiceProvider serviceProvider)
        {
            _monitoringManager = monitoringManager;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ActionAsync(CancellationToken cts)
        {
            var ozzyNode = _serviceProvider.GetService<OzzyNode>();

            var data = new NodeMonitoringInfo()
            {
                NodeId = ozzyNode.NodeId,
                MachineName = Environment.MachineName,
                BackgroundTasks = ozzyNode.BackgroundProcesses.Select(p => new BackgroundTaskMonitoringInfo()
                {
                    TaskId = p.ProcessId,
                    TaskName = p.ProcessName,
                    TaskState = p.ProcessState,
                    IsRunning = p.IsRunning
                }).ToList(),
                MonitoringTimeStamp = DateTime.Now
            };

            await _monitoringManager.SaveNodeMonitoringInfo(data);
        }
    }
}
