using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Priority_Queue
{
    /// Credit: https://github.com/BlueRaja/High-Speed-Priority-Queue-for-C-Sharp
    public class GenericPriorityQueueNode<TPriority>
    {
        /// <summary>
        /// The Priority to insert this node at.  Must be set BEFORE adding a node to the queue (ideally just once, in the node's constructor).
        /// Should not be manually edited once the node has been enqueued - use queue.UpdatePriority() instead
        /// </summary>
        public TPriority Priority { get; protected internal set; }

        /// <summary>
        /// Represents the current position in the queue
        /// </summary>
        public int QueueIndex { get; internal set; }

        /// <summary>
        /// Represents the order the node was inserted in
        /// </summary>
        public long InsertionIndex { get; internal set; }
    }
}
