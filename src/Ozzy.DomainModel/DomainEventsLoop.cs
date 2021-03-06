﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Disruptor;
using Disruptor.Dsl;
using Ozzy.Core;

namespace Ozzy.DomainModel
{
    /// <summary>
    /// Менеджер очереди доменных событий
    /// </summary>
    public class DomainEventsLoop
    {
        public long CurrentSequence => _disruptor.RingBuffer.BufferSize - _disruptor.RingBuffer.RemainingCapacity();

        //  0 - not started, 1 - starting, 2 - started 
        private int _stage;
        private readonly IPeristedEventsReader _persistedEventsReader;
        private readonly List<IDomainEventsProcessor> _eventProcessors;
        private readonly int _bufferSize;
        private readonly int _pollTimeout;
        private readonly IWaitStrategy _waitStrategy;
        private readonly IExceptionHandler _exceptionHandler;
        private Disruptor<DomainEventEntry> _disruptor;
        private RingBuffer<DomainEventEntry> _ringBuffer;
        private Task _supervisorTask;
        private CancellationTokenSource _stopSupervisor;
        private ISequenceBarrier _barrier;

        public DomainEventsLoop(IPeristedEventsReader persistedEventsReader,
            IEnumerable<IDomainEventsProcessor> eventProcessors = null,
            int bufferSize = 16384,
            int pollTimeout = 2000,
            IWaitStrategy waitStrategy = null,
            IExceptionHandler exceptionHandler = null)
        {
            Guard.ArgumentNotNull(persistedEventsReader, nameof(persistedEventsReader));
            _persistedEventsReader = persistedEventsReader;
            _eventProcessors = (eventProcessors ?? new List<IDomainEventsProcessor>()).ToList();
            _bufferSize = bufferSize;
            _pollTimeout = pollTimeout;
            _waitStrategy = waitStrategy ?? new BlockingWaitStrategy();
            _exceptionHandler = exceptionHandler ?? new LogAndIgnoreExceptionHandler();
        }

        public void AddHandler(IDomainEventsProcessor processor)
        {
            if (_stage != 0) throw new InvalidOperationException("It is forbidden to add EventsProcessors after start");
            this._eventProcessors.Add(processor);
        }

        public void AddHandlers(IEnumerable<IDomainEventsProcessor> processors)
        {
            if (_stage != 0) throw new InvalidOperationException("It is forbidden to add EventsProcessors after start");
            this._eventProcessors.AddRange(processors);
        }

        private bool Started()
        {
            return _stage == 2;
        }

        public bool AddEventForProcessing(IDomainEventRecord message)
        {
            if (!Started()) return false;

            //Если добавляется сообщение, которое заведомо не влезет в буфер дисруптора и будет заблокировано - не нужно его обрабатывать
            if (message.Sequence - _ringBuffer.Cursor > _ringBuffer.BufferSize) return false;

            AddEventToDisruptor(message);
            return true;
        }

        //this will block if too many events published
        private void AddEventToDisruptor(IDomainEventRecord message)
        {
            if (message.Sequence <= _ringBuffer.Cursor) return;
            while ((_ringBuffer.Cursor + _ringBuffer.RemainingCapacity()) < message.Sequence)
            {
                //this will block until message.Sequence will be available or timeout 
                _barrier.WaitFor(message.Sequence, TimeSpan.FromMilliseconds(100));
            }
            _ringBuffer[message.Sequence].Value = message;
            _ringBuffer.Publish(message.Sequence);
        }

        private IEnumerable<IDomainEventRecord> GetEventsFromDurableStore()
        {
            var checkpoint = _ringBuffer.Cursor;
            var count = _ringBuffer.RemainingCapacity();
            if (count < 0) count = 0;
            var events = _persistedEventsReader.GetEvents(checkpoint, Convert.ToInt32(count));
            if (!events.Any()) return events;
            // ожидается, что события получаются из IPeristedEventsReader уже отсортированными
            var sortedEvents = events;
            var lastSeq = sortedEvents.Last().Sequence;
            var eventsCount = lastSeq - checkpoint;
            // если событий столько сколько нужно, значит заполнять пробелы не нужно
            if (eventsCount == events.Count()) return events;
            // заполняем пробелы
            List<IDomainEventRecord> result = new List<IDomainEventRecord>();
            long index = checkpoint + 1;
            foreach (var domainEventRecord in sortedEvents)
            {
                while (domainEventRecord.Sequence > index)
                {
                    result.Add(new EmptyEventRecord(index));
                    index++;
                }
                result.Add(domainEventRecord);
                index++;
            }
            return result;
        }

        private void PollData(CancellationToken stopRequested)
        {
            var events = GetEventsFromDurableStore();
            OzzyLogger<IDomainModelTracing>.LogFor<DomainEventsLoop>().Polling(events.Count());

            foreach (var e in events)
            {
                if (stopRequested.IsCancellationRequested)
                {
                    return;
                }
                AddEventToDisruptor(e);
            }
        }


        public long Start()
        {
            OzzyLogger<IDomainModelTracing>.Log.TraceInformationalEvent("Start event loop");

            if (Interlocked.CompareExchange(ref _stage, 1, 0) == 0)
            {
                var minCheckpoint = 0L;
                var checkpoints = _eventProcessors.Select(e => e.GetCheckpoint()).ToList();
                if (checkpoints.Any()) minCheckpoint = checkpoints.Min();

                _disruptor = new Disruptor<DomainEventEntry>(() => new DomainEventEntry(),
                    new MultiThreadedClaimStrategy(_bufferSize),
                    _waitStrategy ?? new BlockingWaitStrategy(),
                    TaskScheduler.Default,
                    minCheckpoint);
                _disruptor.HandleExceptionsWith(_exceptionHandler);
                foreach (var domainEventsProcessor in _eventProcessors)
                {
                    _disruptor.HandleEventsWith(domainEventsProcessor.GetCheckpoint(), domainEventsProcessor);
                }
                _ringBuffer = _disruptor.Start();
                _barrier = _ringBuffer.NewBarrier();
                StartCursorSupervisor(minCheckpoint, TimeSpan.FromMilliseconds(_pollTimeout));
                Interlocked.Exchange(ref _stage, 2);
                return minCheckpoint;
            }
            else
            {
                throw new InvalidOperationException("Domain Manager already started");
            }
        }

        public long Stop()
        {
            if (Interlocked.CompareExchange(ref _stage, 0, 2) == 2)
            {
                if (_supervisorTask != null) _stopSupervisor.Cancel();
                _disruptor.Shutdown();
                return _ringBuffer.Cursor;
            }
            throw new InvalidOperationException("Domain Manager not started");

        }

        private void StartCursorSupervisor(long startCursor, TimeSpan timeout)
        {
            var start = startCursor;
            var barrier = _ringBuffer.NewBarrier();
            _stopSupervisor = new CancellationTokenSource();
            var token = _stopSupervisor.Token;
            _supervisorTask = Task.Factory.StartNew(() =>
            {
                while (!token.IsCancellationRequested)
                {
                    var seq = barrier.WaitFor(start, timeout);
                    if (seq >= start) start = seq + 1;
                    else
                    {
                        try
                        {
                            PollData(token);
                        }
                        catch (Exception e)
                        {
                            OzzyLogger<IDomainModelTracing>.Log.PollException(e);
                        }
                    }
                }
            }, token, TaskCreationOptions.None, TaskScheduler.Default);
        }
    }
}
