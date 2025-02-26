using System;

namespace AosTools.Data {

    /// <summary>
    /// A single huffman node used to construct huffman tree.
    /// </summary>
    /// <typeparam name="T"> Leaf node value type. </typeparam>
    public class HuffmanNode<T> : IComparable {

        /// <summary>
        /// Parent node.
        /// </summary>
        public HuffmanNode<T> Parent;

        /// <summary>
        /// Left side child node.
        /// </summary>
        public HuffmanNode<T> LeftChild;

        /// <summary>
        /// Right side child node.
        /// </summary>
        public HuffmanNode<T> RightChild;

        /// <summary>
        /// Edge bit.
        /// </summary>
        public bool Bit;

        /// <summary>
        /// Leaf node value.
        /// </summary>
        public T Value;

        /// <summary>
        /// Node probability.
        /// </summary>
        public uint Probability;

        /// <summary>
        /// Is this node a leaf node.
        /// </summary>
        public bool IsLeaf { get; private set; }

        /// <summary>
        /// Is this node the root of the huffman tree.
        /// </summary>
        public bool IsRoot => Parent == null;

        /// <summary>
        /// Gets leaf node binary code, if node is not leaf returns null instead.
        /// </summary>
        public bool[] BinaryCode {
            get => IsLeaf == true ? m_BinaryCode : null;
            set => m_BinaryCode = value;
        }

        // Leaf node binary code
        private bool[] m_BinaryCode;

        /// <summary>
        /// Creates a new huffman leaf node.
        /// </summary>
        /// <param name="probability"> Probability of this leaf node. </param>
        /// <param name="value"> Leaf node value. </param>
        public HuffmanNode(uint probability, T value) {
            Probability = probability;
            Value = value;
            IsLeaf = true;
        }

        /// <summary>
        /// Creates a new huffman internal node.
        /// </summary>
        /// <param name="leftChild"> Left side child node. </param>
        /// <param name="rightChild"> Right side child node. </param>
        public HuffmanNode(HuffmanNode<T> leftChild, HuffmanNode<T> rightChild) {
            LeftChild = leftChild;
            RightChild = rightChild;
            leftChild.Bit = false; // 0
            rightChild.Bit = true; // 1
            Probability = leftChild.Probability + rightChild.Probability;
            leftChild.Parent = rightChild.Parent = this;
            IsLeaf = false;
        }

        // Use negative comparison value to make PriorityQueue behave like MinHeap instead of MaxHeap
        public int CompareTo(object obj) => -Probability.CompareTo(((HuffmanNode<T>)obj).Probability);
    }

}