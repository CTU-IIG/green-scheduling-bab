// ---------------------------------------------------------------------------------------------------------------------
// <copyright file="TranslatedArray.cs" company="Czech Technical University in Prague">
//   Copyright (c) 2018 Czech Technical University in Prague
// </copyright>
// ---------------------------------------------------------------------------------------------------------------------

namespace Iirc.Utils.Collections
{
    using System.Collections.Generic;

    public class TranslatedArray<T>
    {
        public TranslatedArray(int minIndex, int maxIndex)
        {
            this.MinIndex = minIndex;
            this.MaxIndex = maxIndex;
            this.Array = new T[this.MaxIndex - this.MinIndex + 1];
        }
        
        public T this[int index] 
        { 
            get
            {
                return this.Array[this.ToZeroBased(index)];
            } 
          
            set
            { 
                this.Array[this.ToZeroBased(index)] = value; 
            } 
        }

        public int ToZeroBased(int index)
        {
            return index - this.MinIndex;
        }

        public bool IndexInRange(int index)
        {
            return this.MinIndex <= index && index <= this.MaxIndex;
        }

        public T[] Array { get; }
        
        public int MinIndex { get; }
        public int MaxIndex { get; }

        public IEnumerable<int> Indices => EnumerableExtensions.RangeTo(this.MinIndex, this.MaxIndex);
    }
}
