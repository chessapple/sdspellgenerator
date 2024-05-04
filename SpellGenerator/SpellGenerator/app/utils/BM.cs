using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using static System.Net.WebRequestMethods;

namespace SpellGenerator.app.utils
{
    /// <summary>
    /// Implements a multi-stage byte array. Uses less memory than a byte
    /// array large enough to hold an offset for each Unicode character.
    /// </summary>
    class UnicodeSkipArray
    {
        // Pattern length used for default byte value
        private byte _patternLength;
        // Default byte array (filled with default value)
        private byte[] _default;
        // Array to hold byte arrays
        private byte[][] _skipTable;
        // Size of each block
        private const int BlockSize = 0x100;

        /// <summary>
        /// Initializes this UnicodeSkipTable instance
        /// </summary>
        /// <param name="patternLength">Length of BM pattern</param>
        public UnicodeSkipArray(int patternLength)
        {
            // Default value (length of pattern being searched)
            _patternLength = (byte)patternLength;
            // Default table (filled with default value)
            _default = new byte[BlockSize];
            InitializeBlock(_default);
            // Master table (array of arrays)
            _skipTable = new byte[BlockSize][];
            for (int i = 0; i < BlockSize; i++)
                _skipTable[i] = _default;
        }

        /// <summary>
        /// Sets/gets a value in the multi-stage tables.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public byte this[int index]
        {
            get
            {
                // Return value
                return _skipTable[index / BlockSize][index % BlockSize];
            }
            set
            {
                // Get array that contains value to set
                int i = (index / BlockSize);
                // Does it reference the default table?
                if (_skipTable[i] == _default)
                {
                    // Yes, value goes in a new table
                    _skipTable[i] = new byte[BlockSize];
                    InitializeBlock(_skipTable[i]);
                }
                // Set value
                _skipTable[i][index % BlockSize] = value;
            }
        }

        /// <summary>
        /// Initializes a block to hold the current "nomatch" value.
        /// </summary>
        /// <param name="block">Block to be initialized</param>
        private void InitializeBlock(byte[] block)
        {
            for (int i = 0; i < BlockSize; i++)
                block[i] = _patternLength;
        }
    }

    /// <summary>
    /// Implements Boyer-Moore search algorithm
    /// </summary>
    class BoyerMoore
    {
        private string _pattern;
        private bool _ignoreCase;
        private UnicodeSkipArray _skipArray;

        // Returned index when no match found
        public const int InvalidIndex = -1;

        public BoyerMoore(string pattern)
        {
            Initialize(pattern, false);
        }

        public BoyerMoore(string pattern, bool ignoreCase)
        {
            Initialize(pattern, ignoreCase);
        }

        /// <summary>
        /// Initializes this instance to search a new pattern.
        /// </summary>
        /// <param name="pattern">Pattern to search for</param>
        public void Initialize(string pattern)
        {
            Initialize(pattern, false);
        }

        /// <summary>
        /// Initializes this instance to search a new pattern.
        /// </summary>
        /// <param name="pattern">Pattern to search for</param>
        /// <param name="ignoreCase">If true, search is case-insensitive</param>
        public void Initialize(string pattern, bool ignoreCase)
        {
            _pattern = pattern;
            _ignoreCase = ignoreCase;

            // Create multi-stage skip table
            _skipArray = new UnicodeSkipArray(_pattern.Length);
            // Initialize skip table for this pattern
            if (_ignoreCase)
            {
                for (int i = 0; i < _pattern.Length - 1; i++)
                {
                    _skipArray[Char.ToLower(_pattern[i])] = (byte)(_pattern.Length - i - 1);
                    _skipArray[Char.ToUpper(_pattern[i])] = (byte)(_pattern.Length - i - 1);
                }
            }
            else
            {
                for (int i = 0; i < _pattern.Length - 1; i++)
                    _skipArray[_pattern[i]] = (byte)(_pattern.Length - i - 1);
            }
        }

        /// <summary>
        /// Searches for the current pattern within the given text
        /// starting at the beginning.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public int Search(string text)
        {
            return Search(text, 0);
        }

        /// <summary>
        /// Searches for the current pattern within the given text
        /// starting at the specified index.
        /// </summary>
        /// <param name="text">Text to search</param>
        /// <param name="startIndex">Offset to begin search</param>
        /// <returns></returns>
        public int Search(string text, int startIndex)
        {
            int i = startIndex;

            // Loop while there's still room for search term
            while (i <= (text.Length - _pattern.Length))
            {
                // Look if we have a match at this position
                int j = _pattern.Length - 1;
                if (_ignoreCase)
                {
                    while (j >= 0 && Char.ToUpper(_pattern[j]) == Char.ToUpper(text[i + j]))
                        j--;
                }
                else
                {
                    while (j >= 0 && _pattern[j] == text[i + j])
                        j--;
                }

                if (j < 0)
                {
                    // Match found
                    return i;
                }

                // Advance to next comparision
                i += Math.Max(_skipArray[text[i + j]] - _pattern.Length + 1 + j, 1);
            }
            // No match found
            return InvalidIndex;
        }
    }
}

