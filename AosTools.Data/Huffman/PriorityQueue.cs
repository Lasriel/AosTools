using System;
using System.Collections.Generic;

namespace AosTools.Data {

    /// <summary>
    /// Represents a collection of items that have a value and a priority.
    /// On dequeue, the item with the highest/lowest priority value is removed.
    /// Priority is decided by implementing <see cref="IComparable"/>
    /// <para> Implemented using BinaryHeap https://en.wikipedia.org/wiki/Binary_heap </para>
    /// </summary>
    public class PriorityQueue<T> where T : IComparable {

        private List<T> m_Heap = new List<T>();

        /// <summary>
        /// Gets the number of items contained in the PriorityQueue.
        /// </summary>
        public int Count => m_Heap.Count;

        /// <summary>
        /// Adds item to the PriorityQueue.
        /// </summary>
        /// <param name="value"> Item to add. </param>
        public void Enqueue(T value) {
            m_Heap.Add(value);
            UpHeap(m_Heap.Count - 1);
        }

        /// <summary>
        /// Removes and returns the max/min priority item from the PriorityQueue.
        /// </summary>
        /// <returns> Item with the max/min priority. </returns>
        /// <exception cref="IndexOutOfRangeException"></exception>
        public T Dequeue() {
            if (m_Heap.Count == 0) {
                throw new IndexOutOfRangeException("Trying to dequeue an empty priority queue.");
            }

            T minItem = m_Heap[0];

            SetAt(0, m_Heap[m_Heap.Count - 1]);
            m_Heap.RemoveAt(m_Heap.Count - 1);
            DownHeap(0);
            return minItem;
        }

        /// <summary>
        /// Returns the max/min priority item from the PriorityQueue without removing it.
        /// </summary>
        /// <returns> Item with the max/min priority. </returns>
        /// <exception cref="IndexOutOfRangeException"></exception>
        public T Peek() {
            if (m_Heap.Count == 0) {
                throw new IndexOutOfRangeException("Trying to peek at an empty priority queue.");
            }
            return m_Heap[0];
        }

        /// <summary> Swaps the items around at the given indexes. </summary>
        private void Swap(int i, int j) {
            T tmp = ArrayVal(i);
            SetAt(i, m_Heap[j]);
            SetAt(j, tmp);
        }

        /// <summary> Moves the given index up the heap until correct priority is met. </summary>
        private void UpHeap(int i) {
            while (i > 0 && ArrayVal(i).CompareTo(Parent(i)) > 0) {
                Swap(i, ParentIndex(i));
                i = ParentIndex(i);
            }
        }

        /// <summary> Moves the given index down the heap until correct priority is met. </summary>
        private void DownHeap(int i) {
            while (i >= 0) {
                int iContinue = -1;

                if (RightSonExists(i) && Right(i).CompareTo(ArrayVal(i)) > 0) {
                    iContinue = Left(i).CompareTo(Right(i)) < 0 ? RightChildIndex(i) : LeftChildIndex(i);
                } else if (LeftSonExists(i) && Left(i).CompareTo(ArrayVal(i)) > 0) {
                    iContinue = LeftChildIndex(i);
                }

                if (iContinue >= 0 && iContinue < m_Heap.Count) {
                    Swap(i, iContinue);
                }

                i = iContinue;
            }
        }

        /// <summary> Gets the item at given index. </summary>
        private T ArrayVal(int i) => m_Heap[i];

        /// <summary> Gets the parent item index. </summary>
        private int ParentIndex(int i) => (i - 1) / 2;

        /// <summary> Gets the parent item from the given index. </summary>
        private T Parent(int i) => m_Heap[ParentIndex(i)];

        /// <summary> Sets item at the given index. </summary>
        private void SetAt(int i, T value) => m_Heap[i] = value;

        /// <summary> Gets the right side child item of the given index. </summary>
        private T Right(int i) => m_Heap[RightChildIndex(i)];

        /// <summary> Gets the left side child item of the given index. </summary>
        private T Left(int i) => m_Heap[LeftChildIndex(i)];

        /// <summary> Checks whether or not item has right child in the heap. </summary>
        private bool RightSonExists(int i) => RightChildIndex(i) < m_Heap.Count;

        /// <summary> Checks whether or not item has left child in the heap. </summary>
        private bool LeftSonExists(int i) => LeftChildIndex(i) < m_Heap.Count;

        /// <summary> Gets index of the right child of the given index. </summary>
        private int RightChildIndex(int i) => 2 * (i + 1);

        /// <summary> Gets index of the left child of the given index. </summary>
        private int LeftChildIndex(int i) => 2 * i + 1;

    }

}