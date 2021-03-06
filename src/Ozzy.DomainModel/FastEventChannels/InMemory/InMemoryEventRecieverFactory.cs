﻿using Ozzy.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Ozzy.DomainModel
{  
    public class InMemoryEventRecieverFactory : IFastEventRecieverFactory
    {
        private InMemoryDomainEventsPubSub _domainQueue;
        private IDisposable _subscription = null;
        private DomainEventsLoop _loop;

        public InMemoryEventRecieverFactory(InMemoryDomainEventsPubSub domainQueue)
        {
            Guard.ArgumentNotNull(domainQueue, nameof(domainQueue));
            _domainQueue = domainQueue;
        }

        public IFastEventReciever CreateReciever(DomainEventsLoop loop)
        {
            return new InMemoryEventReciever(loop, _domainQueue);
        }
    }
}
