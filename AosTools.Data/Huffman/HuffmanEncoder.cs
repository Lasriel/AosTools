using System;
using System.Collections.Generic;

namespace AosTools.Data {

    /// <summary>
    /// Creates a new huffman tree from values and their counts.
    /// Precalculates binary codes for each value for better encoding performance.
    /// <para> https://en.wikipedia.org/wiki/Huffman_coding </para>
    /// </summary>
    /// <typeparam name="T"> Type of the value stored in the leaf nodes. </typeparam>
    public class HuffmanEncoder<T> {

        /// <summary>
        /// Root node of the created huffman tree.
        /// </summary>
        public HuffmanNode<T> RootNode { get; private set; }

        private readonly Dictionary<T, HuffmanNode<T>> m_LeafNodes = new Dictionary<T, HuffmanNode<T>>();

        /// <summary>
        /// Creates a new huffman tree using values and their counts.
        /// </summary>
        /// <param name="counts"> Dictionary of values and their counts. </param>
        public HuffmanEncoder(Dictionary<T, uint> counts) {
            PriorityQueue<HuffmanNode<T>> priorityQueue = new PriorityQueue<HuffmanNode<T>>();

            // Create leaf nodes from values and counts
            foreach (T value in counts.Keys) {
                HuffmanNode<T> leafNode = new HuffmanNode<T>(counts[value], value);
                priorityQueue.Enqueue(leafNode);
                m_LeafNodes[value] = leafNode;
            }

            // Build huffman tree using the created leaf nodes and internal nodes
            while (priorityQueue.Count > 1) {
                HuffmanNode<T> leftChild = priorityQueue.Dequeue();
                HuffmanNode<T> rightChild = priorityQueue.Dequeue();
                HuffmanNode<T> internalNode = new HuffmanNode<T>(leftChild, rightChild);
                priorityQueue.Enqueue(internalNode);
            }

            // Last node in the priority queue is the root node of the tree
            RootNode = priorityQueue.Dequeue();

            CalculateLeafNodeBinaryCodes();
        }

        /// <summary>
        /// Gets binary code encoding for given value.
        /// </summary>
        /// <param name="value"> Value to get binary code for. </param>
        /// <returns> Binary code as bool array. </returns>
        public bool[] Encode(T value) {
            if (!m_LeafNodes.ContainsKey(value)) {
                throw new ArgumentException("Tried to get encoding for a value that does not exist in leaf nodes.");
            }

            return m_LeafNodes[value].BinaryCode;
        }

        /// <summary>
        /// Precalculates leaf node binary codes for encoding.
        /// </summary>
        private void CalculateLeafNodeBinaryCodes() {

            // Create binary codes for each leaf node by climbing the created tree
            foreach (KeyValuePair<T, HuffmanNode<T>> pair in m_LeafNodes) {
                List<bool> binaryCode = new List<bool>();
                HuffmanNode<T> currentNode = pair.Value;
                while (!currentNode.IsRoot) {
                    binaryCode.Add(currentNode.Bit);
                    currentNode = currentNode.Parent;
                }

                binaryCode.Reverse();
                pair.Value.BinaryCode = binaryCode.ToArray();
            }
        }


    }

}