using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProducerConsumerQueue.Queueing
{
   
        public class ProcessorQueueItem<T, U>
        {
            public T Item { get; set; }

            public TaskCompletionSource<U> CompletionSource { get; set; }

            public ProcessorQueueItem(T item)
            {
                this.Item = item;
                this.CompletionSource = new TaskCompletionSource<U>();
            }
        }
    }

