using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProducerConsumerQueue.Model.Interfaces;

namespace ProducerConsumerQueue.Model
{
    public class ConsoleQueueModel:IQueueItem
    {
        public string Message
        {
            get;
            set;
        }
        public  ConsoleQueueModel(string message)
        {
            this.Message = message;
        }
       
    
    }
}
