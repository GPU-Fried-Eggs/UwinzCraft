using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
// ReSharper disable InconsistentNaming

namespace Utilities
{
    public class PriorityQueueNode
    {
        public float Priority { get; protected internal set; }
        public int QueueIndex { get; internal set; }
    }
    
    public sealed class PriorityQueue<T> : IEnumerable<T> where T : PriorityQueueNode
    {
        private int m_NumNodes;
        private T[] m_Nodes;
        
        public PriorityQueue(int maxNodes)
        {
            m_NumNodes = 0;
            m_Nodes = new T[maxNodes + 1];
        }
        
        public int Count => m_NumNodes;

        public int MaxSize => m_Nodes.Length - 1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            Array.Clear(m_Nodes, 1, m_NumNodes);
            m_NumNodes = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(T node) => m_Nodes[node.QueueIndex] == node;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Enqueue(T node, float priority)
        {
            node.Priority = priority;
            m_NumNodes++;
            m_Nodes[m_NumNodes] = node;
            node.QueueIndex = m_NumNodes;
            CascadeUp(node);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CascadeUp(T node)
        {
            int parent;
            if(node.QueueIndex > 1)
            {
                parent = node.QueueIndex >> 1;
                T parentNode = m_Nodes[parent];
                if(HasHigherOrEqualPriority(parentNode, node)) return;

                //Node has lower priority value, so move parent down the heap to make room
                m_Nodes[node.QueueIndex] = parentNode;
                parentNode.QueueIndex = node.QueueIndex;

                node.QueueIndex = parent;
            }
            else
            {
                return;
            }
            
            while(parent > 1)
            {
                parent >>= 1;
                T parentNode = m_Nodes[parent];
                if(HasHigherOrEqualPriority(parentNode, node)) break;

                //Node has lower priority value, so move parent down the heap to make room
                m_Nodes[node.QueueIndex] = parentNode;
                parentNode.QueueIndex = node.QueueIndex;

                node.QueueIndex = parent;
            }
            
            m_Nodes[node.QueueIndex] = node;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CascadeDown(T node)
        {
            int finalQueueIndex = node.QueueIndex;
            int childLeftIndex = 2 * finalQueueIndex;

            // If leaf node, we're done
            if(childLeftIndex > m_NumNodes) return;

            // Check if the left-child is higher-priority than the current node
            int childRightIndex = childLeftIndex + 1;
            T childLeft = m_Nodes[childLeftIndex];
            if(HasHigherPriority(childLeft, node))
            {
                // Check if there is a right child. If not, swap and finish.
                if(childRightIndex > m_NumNodes)
                {
                    node.QueueIndex = childLeftIndex;
                    childLeft.QueueIndex = finalQueueIndex;
                    m_Nodes[finalQueueIndex] = childLeft;
                    m_Nodes[childLeftIndex] = node;
                    return;
                }
                // Check if the left-child is higher-priority than the right-child
                T childRight = m_Nodes[childRightIndex];
                if(HasHigherPriority(childLeft, childRight))
                {
                    // left is highest, move it up and continue
                    childLeft.QueueIndex = finalQueueIndex;
                    m_Nodes[finalQueueIndex] = childLeft;
                    finalQueueIndex = childLeftIndex;
                }
                else
                {
                    // right is even higher, move it up and continue
                    childRight.QueueIndex = finalQueueIndex;
                    m_Nodes[finalQueueIndex] = childRight;
                    finalQueueIndex = childRightIndex;
                }
            }
            // Not swapping with left-child, does right-child exist?
            else if(childRightIndex > m_NumNodes)
            {
                return;
            }
            else
            {
                // Check if the right-child is higher-priority than the current node
                T childRight = m_Nodes[childRightIndex];
                if(HasHigherPriority(childRight, node))
                {
                    childRight.QueueIndex = finalQueueIndex;
                    m_Nodes[finalQueueIndex] = childRight;
                    finalQueueIndex = childRightIndex;
                }
                // Neither child is higher-priority than current, so finish and stop.
                else
                {
                    return;
                }
            }

            while(true)
            {
                childLeftIndex = 2 * finalQueueIndex;

                // If leaf node, we're done
                if(childLeftIndex > m_NumNodes)
                {
                    node.QueueIndex = finalQueueIndex;
                    m_Nodes[finalQueueIndex] = node;
                    break;
                }

                // Check if the left-child is higher-priority than the current node
                childRightIndex = childLeftIndex + 1;
                childLeft = m_Nodes[childLeftIndex];
                if(HasHigherPriority(childLeft, node))
                {
                    // Check if there is a right child. If not, swap and finish.
                    if(childRightIndex > m_NumNodes)
                    {
                        node.QueueIndex = childLeftIndex;
                        childLeft.QueueIndex = finalQueueIndex;
                        m_Nodes[finalQueueIndex] = childLeft;
                        m_Nodes[childLeftIndex] = node;
                        break;
                    }
                    // Check if the left-child is higher-priority than the right-child
                    T childRight = m_Nodes[childRightIndex];
                    if(HasHigherPriority(childLeft, childRight))
                    {
                        // left is highest, move it up and continue
                        childLeft.QueueIndex = finalQueueIndex;
                        m_Nodes[finalQueueIndex] = childLeft;
                        finalQueueIndex = childLeftIndex;
                    }
                    else
                    {
                        // right is even higher, move it up and continue
                        childRight.QueueIndex = finalQueueIndex;
                        m_Nodes[finalQueueIndex] = childRight;
                        finalQueueIndex = childRightIndex;
                    }
                }
                // Not swapping with left-child, does right-child exist?
                else if(childRightIndex > m_NumNodes)
                {
                    node.QueueIndex = finalQueueIndex;
                    m_Nodes[finalQueueIndex] = node;
                    break;
                }
                else
                {
                    // Check if the right-child is higher-priority than the current node
                    T childRight = m_Nodes[childRightIndex];
                    if(HasHigherPriority(childRight, node))
                    {
                        childRight.QueueIndex = finalQueueIndex;
                        m_Nodes[finalQueueIndex] = childRight;
                        finalQueueIndex = childRightIndex;
                    }
                    // Neither child is higher-priority than current, so finish and stop.
                    else
                    {
                        node.QueueIndex = finalQueueIndex;
                        m_Nodes[finalQueueIndex] = node;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Returns true if 'higher' has higher priority than 'lower', false otherwise.
        /// Note that calling HasHigherPriority(node, node) (ie. both arguments the same node) will return false
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool HasHigherPriority(T higher, T lower) => higher.Priority < lower.Priority;

        /// <summary>
        /// Returns true if 'higher' has higher priority than 'lower', false otherwise.
        /// Note that calling HasHigherOrEqualPriority(node, node) (ie. both arguments the same node) will return true
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool HasHigherOrEqualPriority(T higher, T lower) => higher.Priority <= lower.Priority;

        /// <summary>
        /// Removes the head of the queue and returns it.
        /// If queue is empty, result is undefined
        /// O(log n)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Dequeue()
        {
            T returnMe = m_Nodes[1];
            //If the node is already the last node, we can remove it immediately
            if(m_NumNodes == 1)
            {
                m_Nodes[1] = null;
                m_NumNodes = 0;
                return returnMe;
            }

            //Swap the node with the last node
            T formerLastNode = m_Nodes[m_NumNodes];
            m_Nodes[1] = formerLastNode;
            formerLastNode.QueueIndex = 1;
            m_Nodes[m_NumNodes] = null;
            m_NumNodes--;

            //Now bubble formerLastNode (which is no longer the last node) down
            CascadeDown(formerLastNode);
            return returnMe;
        }

        /// <summary>
        /// Resize the queue so it can accept more nodes.  All currently enqueued nodes are remain.
        /// Attempting to decrease the queue size to a size too small to hold the existing nodes results in undefined behavior
        /// O(n)
        /// </summary>
        public void Resize(int maxNodes)
        {
            var newArray = new T[maxNodes + 1];
            var highestIndexToCopy = Math.Min(maxNodes, m_NumNodes);
            Array.Copy(m_Nodes, newArray, highestIndexToCopy + 1);
            m_Nodes = newArray;
        }

        /// <summary>
        /// Returns the head of the queue, without removing it (use Dequeue() for that).
        /// If the queue is empty, behavior is undefined.
        /// O(1)
        /// </summary>
        public T First => m_Nodes[1];

        /// <summary>
        /// This method must be called on a node every time its priority changes while it is in the queue.  
        /// <b>Forgetting to call this method will result in a corrupted queue!</b>
        /// Calling this method on a node not in the queue results in undefined behavior
        /// O(log n)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdatePriority(T node, float priority)
        {
            node.Priority = priority;
            OnNodeUpdated(node);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void OnNodeUpdated(T node)
        {
            //Bubble the updated node up or down as appropriate
            var parentIndex = node.QueueIndex >> 1;

            if(parentIndex > 0 && HasHigherPriority(node, m_Nodes[parentIndex]))
            {
                CascadeUp(node);
            }
            else
            {
                //Note that CascadeDown will be called if parentNode == node (that is, node is the root)
                CascadeDown(node);
            }
        }

        /// <summary>
        /// Removes a node from the queue.  The node does not need to be the head of the queue.  
        /// If the node is not in the queue, the result is undefined.  If unsure, check Contains() first
        /// O(log n)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Remove(T node)
        {
            //If the node is already the last node, we can remove it immediately
            if(node.QueueIndex == m_NumNodes)
            {
                m_Nodes[m_NumNodes] = null;
                m_NumNodes--;
                return;
            }

            //Swap the node with the last node
            var formerLastNode = m_Nodes[m_NumNodes];
            m_Nodes[node.QueueIndex] = formerLastNode;
            formerLastNode.QueueIndex = node.QueueIndex;
            m_Nodes[m_NumNodes] = null;
            m_NumNodes--;

            //Now bubble formerLastNode (which is no longer the last node) up or down as appropriate
            OnNodeUpdated(formerLastNode);
        }

        /// <summary>
        /// By default, nodes that have been previously added to one queue cannot be added to another queue.
        /// If you need to do this, please call originalQueue.ResetNode(node) before attempting to add it in the new queue
        /// If the node is currently in the queue or belongs to another queue, the result is undefined
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ResetNode(T node) => node.QueueIndex = 0;

        public IEnumerator<T> GetEnumerator()
        {
            IEnumerable<T> e = new ArraySegment<T>(m_Nodes, 1, m_NumNodes);
            return e.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// <b>Should not be called in production code.</b>
        /// Checks to make sure the queue is still in a valid state.  Used for testing/debugging the queue.
        /// </summary>
        public bool IsValidQueue()
        {
            for(int i = 1; i < m_Nodes.Length; i++)
            {
                if(m_Nodes[i] != null)
                {
                    int childLeftIndex = 2 * i;
                    if(childLeftIndex < m_Nodes.Length && m_Nodes[childLeftIndex] != null && HasHigherPriority(m_Nodes[childLeftIndex], m_Nodes[i]))
                        return false;

                    int childRightIndex = childLeftIndex + 1;
                    if(childRightIndex < m_Nodes.Length && m_Nodes[childRightIndex] != null && HasHigherPriority(m_Nodes[childRightIndex], m_Nodes[i]))
                        return false;
                }
            }
            return true;
        }
    }
}