﻿#region Using Directives

using System;
using System.Collections;
using System.Collections.Generic;

#endregion

#region Other Licenses
// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
#endregion

namespace Odyssey.Collections
{
    /// <summary>
    ///     Exposes an array as readonly with readonly elements with support for improved performance for equality.
    /// </summary>
    public class ReadOnlyArray<T> : IEnumerable<T>, IEquatable<ReadOnlyArray<T>>
    {
        private readonly int hashCode;
        internal T[] Elements;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ReadOnlyArray{T}" /> class.
        /// </summary>
        /// <param name="elements">The elements.</param>
        public ReadOnlyArray(T[] elements)
        {
            if (elements == null)
                throw new ArgumentNullException("elements");

            Elements = elements;

            // precompute the hashcode
            hashCode = elements.Length;
            for (int i = 0; i < elements.Length; i++)
            {
                hashCode = (hashCode*397) ^ elements[i].GetHashCode();
            }
        }

        /// <summary>
        ///     Gets number of elements.
        /// </summary>
        /// <value>The number of elements.</value>
        public int Length
        {
            get { return Elements.Length; }
        }

        /// <summary>Gets a specific element in the collection by using an index value.</summary>
        /// <param name="index">Index of the value to get.</param>
        public T this[int index]
        {
            get { return Elements[index]; }
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < Elements.Length; i++)
                yield return Elements[i];
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool Equals(ReadOnlyArray<T> other)
        {
            if (hashCode != other.hashCode)
                return false;

            if (ReferenceEquals(Elements, other.Elements))
                return true;

            if (Elements.Length != other.Elements.Length)
                return false;

            for (int i = 0; i < Elements.Length; i++)
            {
                if (!Equals(Elements[i], other.Elements[i]))
                    return false;
            }

            return true;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((ReadOnlyArray<T>) obj);
        }

        public override int GetHashCode()
        {
            return hashCode;
        }

        /// <summary>
        ///     Implements the operator ==.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator ==(ReadOnlyArray<T> left, ReadOnlyArray<T> right)
        {
            return Equals(left, right);
        }

        /// <summary>
        ///     Implements the operator !=.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator !=(ReadOnlyArray<T> left, ReadOnlyArray<T> right)
        {
            return !Equals(left, right);
        }
    }
}