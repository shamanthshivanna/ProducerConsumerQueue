using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace ProducerConsumerQueue.Queueing
{
    public sealed class ProcessorQueue<T, U>
    {
        private readonly BlockingCollection<ProcessorQueueItem<T, U>> _processFileQueue = new BlockingCollection<ProcessorQueueItem<T, U>>();
        private readonly BlockingCollection<ProcessorQueueItem<T, U>> _processConsoleQueue = new BlockingCollection<ProcessorQueueItem<T, U>>();
        private readonly Func<T, U> _processFunction;

        public ProcessorQueue(Func<T, U> ProcessFunction)
        {
            this._processFunction = ProcessFunction;
            Task.Factory.StartNew(() => this.ProcessFileQueue(), TaskCreationOptions.LongRunning);
            Task.Factory.StartNew(() => this.ProcessConsoleQueue(), TaskCreationOptions.LongRunning);
        }

        public Task<U> QueueFileItem(T item)
        {
            ProcessorQueueItem<T, U> processorQueueItem = new ProcessorQueueItem<T, U>(item);
            this._processFileQueue.TryAdd(processorQueueItem);
            return processorQueueItem.CompletionSource.Task;
        }
        public Task<U> QueueConsoleItem(T item)
        {
            ProcessorQueueItem<T, U> processorQueueItem = new ProcessorQueueItem<T, U>(item);
            this._processConsoleQueue.TryAdd(processorQueueItem);
            return processorQueueItem.CompletionSource.Task;
        }
        private void ProcessConsoleQueue()
        {
            ProcessorQueueItem<T, U> queueItem;
            while (this._processConsoleQueue.TryTake(out queueItem, -1))
            {
                T obj = queueItem.Item;

                TaskCompletionSource<U> tcs = queueItem.CompletionSource;
                try
                {
                    TaskCompletionSource<U> completionSource = tcs;
                    U result = this._processFunction(obj);
                    completionSource.SetResult(result);
                    completionSource = (TaskCompletionSource<U>)null;
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
                tcs = (TaskCompletionSource<U>)null;
            }
        }
        private void ProcessFileQueue()
        {
            ProcessorQueueItem<T, U> queueItem;
            while (this._processFileQueue.TryTake(out queueItem, -1))
            {
                T obj = queueItem.Item;

                TaskCompletionSource<U> tcs = queueItem.CompletionSource;
                try
                {
                    TaskCompletionSource<U> completionSource = tcs;
                    U result = this._processFunction(obj);
                    completionSource.SetResult(result);
                    completionSource = (TaskCompletionSource<U>)null;
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
                tcs = (TaskCompletionSource<U>)null;
            }

        }
    }
}
