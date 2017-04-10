﻿using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Ozzy.DomainModel;
using Ozzy.DomainModel.Queues;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ozzy.Server.Queues
{
    public class QueueService<T> : IQueueService<T> where T : class
    {
        private IQueueRepository _queueRepository;
        public virtual string QueueName { get; protected set; } = "GeneralQueue";

        public QueueService(IQueueRepository queueRepository)
        {
            _queueRepository = queueRepository;
        }

        public void Acknowledge(QueueItem<T> item)
        {
            _queueRepository.Acknowledge(item.QueueId);
        }

        public void Add(T item)
        {
            _queueRepository.Create(new QueueRecord(Guid.NewGuid().ToString())
            {
                CreatedAt = DateTime.Now,
                Status = QueueStatus.Awaiting,
                ItemType = typeof(T).AssemblyQualifiedName,
                Content = JsonConvert.SerializeObject(item),
                QueueName = QueueName
            });
        }

        public QueueItem<T> FetchNext()
        {
            var queueItem = _queueRepository.FetchNext(QueueName);
            if (queueItem == null)
                return null;

            var rez =  JsonConvert.DeserializeObject<T>(queueItem.Content);

            return new QueueItem<T>()
            {
                QueueId = queueItem.Id,
                Item = rez
            };
        }
    }
}
