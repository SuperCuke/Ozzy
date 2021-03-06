﻿using System;
using Ozzy.DomainModel;
using System.Threading.Tasks;

namespace Ozzy.Server
{
    public class SingleInstanceProcess<T> : BackgroundProcessBase where T : IBackgroundProcess
    {
        private readonly IDistributedLockService _lockService;
        private readonly IBackgroundProcess _innerProcess;
        private IDistributedLock _dlock;

        public SingleInstanceProcess(IDistributedLockService lockService, T innerProcess)
        {
            _lockService = lockService;
            _innerProcess = innerProcess;
            ProcessName = innerProcess.ProcessName;
            ProcessId = innerProcess.ProcessId;
        }

        public override string ProcessState
        {
            get
            {
                string state = IsRunning ? "Started" : "Not Started";
                state += _innerProcess.IsRunning ? " (Doing Work)" : $" (Not Doing Work - Waiting For Distributed Lock '{ProcessName}')";
                return state;
            }
        }

        protected override async Task StartInternal()
        {
            using (_dlock = await _lockService.CreateLockAsync(this.ProcessName,
                TimeSpan.FromSeconds(1),
                TimeSpan.MaxValue,
                TimeSpan.FromSeconds(1),
                StopRequested.Token,
                () =>
                {
                    // if process was not stopped from outside, restart it so it can try to acquire lock again
                    if (!StopRequested.IsCancellationRequested)
                    {
                        try
                        {
                            Stop().Wait();
                        }
                        catch (Exception e)
                        {
                            //todo : log 
                        }
                        Start();
                    }
                }))
            {
                if (_dlock != null && _dlock.IsAcquired)
                {
                    await _innerProcess.Start();
                    //await Stop();
                    return;
                }
            }
            await StartInternal();
        }

        protected override void StopInternal()
        {
            _innerProcess.Stop().Wait();
            _dlock?.Dispose();
            base.StopInternal();
        }
    }
}
