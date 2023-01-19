using System;
using System.Collections.Generic;
using static Optional.NetFramework.ThrowHelper;

namespace Optional.NetFramework
{
    /// <summary>
    /// Provides a set of functions for creating optional values.
    /// </summary>
    public static class Option
    {
        /// <summary>
        /// Wraps an existing value in an Option&lt;T&gt; instance.
        /// </summary>
        /// <param name="value">The value to be wrapped.</param>
        /// <returns>An optional containing the specified value.</returns>
        public static Option<T> Some<T>(T value) => new Option<T>(value, true, null);

        /// <summary>
        /// Creates an empty Option&lt;T&gt; instance.
        /// </summary>
        /// <returns>An empty optional.</returns>
        public static Option<T> None<T>() => new Option<T>(default, false, null);

        /// <summary>
        /// Creates an empty Option<TException>; instance, 
        /// with a specified exceptional value.
        /// </summary>
        /// <param name="exception">The exceptional value.</param>
        /// <returns>An empty optional.</returns>
        public static Option<T> None<T, TException>(TException exception)
            where TException : Exception => new Option<T>(default, false, exception);
    }

    public readonly struct Option<T> : IEquatable<Option<T>>, IComparable<Option<T>>
    {
        public readonly bool HasValue;
        public readonly T Value;
        public readonly Exception Exception;

        internal Option(T value, bool hasValue, Exception exception)
        {
            Value     = value;
            HasValue  = hasValue;
            Exception = exception;
        }

        public override string ToString()
        {
            if (!HasValue) return Exception == null ? "None" : "None with Exception";
            return Value == null ? "Some(null)" : $"Some({Value})";
        }

        public Option<T> WithException<TException>(TException exception)
            where TException : Exception
        {
            return HasValue
                ? Option.Some(Value)
                : Option.None<T, TException>(exception);
        }

        /// <summary>
        /// Attaches an exceptional value to an empty optional.
        /// </summary>
        /// <param name="exceptionFactory">A factory function to create an exceptional value to attach.</param>
        /// <returns>An optional with an exceptional value.</returns>
        public Option<T> WithException<TException>(Func<TException> exceptionFactory)
            where TException : Exception
        {
            if (exceptionFactory == null) 
                throw new ArgumentNullException(nameof(exceptionFactory));

            return HasValue
                ? Option.Some(Value)
                : new Option<T>(default, false, exceptionFactory());
        }
        
        /// <summary>
        /// Forgets any attached exceptional value.
        /// </summary>
        /// <returns>An optional without an exceptional value.</returns>
        public Option<T> WithoutException() => HasValue ? Option.Some(Value) : Option.None<T>();

        #region IEquatable<Option<T>> Implementation
        //##########################################

        public bool Equals(Option<T> other)
        {
            return HasValue == other.HasValue && EqualityComparer<T>.Default.Equals(Value, other.Value) &&
                   EqualityComparer<Exception>.Default.Equals(Exception, other.Exception);
        }

        public override bool Equals(object obj)
        {
            return obj is Option<T> other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = HasValue.GetHashCode();
                hashCode = (hashCode * 397) ^ EqualityComparer<T>.Default.GetHashCode(Value);
                hashCode = (hashCode * 397) ^ EqualityComparer<Exception>.Default.GetHashCode(Exception);
                return hashCode;
            }
        }

        #endregion
        
        #region IComparable<Option<T>>  Implementation
        //############################################

        public int CompareTo(Option<T> other)
        {
            return HasValue.CompareTo(other.HasValue);
        }

        #endregion
        
        #region Compare operators
        //#######################

        /// <summary>Checks whether two optional objects are identical.</summary>
        public static bool operator ==(Option<T> left, Option<T> right) => left.Equals(right);
        /// <summary>Checks whether two optional objects are not identical.</summary>
        public static bool operator !=(Option<T> left, Option<T> right) => !left.Equals(right);

        public static bool operator <(Option<T> left, Option<T> right) => left.CompareTo(right) < 0;
        public static bool operator >(Option<T> left, Option<T> right) => left.CompareTo(right) > 0;
        public static bool operator <=(Option<T> left, Option<T> right) => left.CompareTo(right) <= 0;
        public static bool operator >=(Option<T> left, Option<T> right) => left.CompareTo(right) >= 0;

        #endregion
        
        public bool Exists(Func<T, bool> predicate)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            return HasValue && predicate(Value); 
        }
        
        public IEnumerable<T> ToEnumerable()
        {
            if (!HasValue) yield break;
            yield return Value;
        }
        
        public IEnumerator<T> GetEnumerator()
        {
            if (!HasValue) yield break;
            yield return Value;
        }
        
        /// <summary>Checks whether the optional object contains the specified value.</summary>
        public bool Contains(T value)
        {
            return HasValue && (Value == null ? value == null : Value.Equals(value));
        }
        
        #region ValueOr / Or / Else
        //#########################
        
        public T ValueOr(Option<T> alternative) => HasValue ? Value : alternative.Value;
        
        public T ValueOr(Func<T> alternativeFactory)
        {
            if (alternativeFactory == null)
                throw InvalidAlternativeFactoryReference(nameof(alternativeFactory));
            
            return HasValue ? Value : alternativeFactory();
        }
        
        /// <summary>
        /// Returns the existing value if present, and otherwise an alternative value.
        /// </summary>
        /// <param name="alternativeFactory">A factory function to map the exceptional value to an alternative value.</param>
        /// <returns>The existing or alternative value.</returns>
        public T ValueOr(Func<Exception, T> alternativeFactory)
        {
            if (alternativeFactory == null)
                throw InvalidAlternativeFactoryReference(nameof(alternativeFactory));

            return HasValue ? Value : alternativeFactory(Exception);
        }
        
        /// <summary>
        /// Uses an alternative value, if no existing value is present.
        /// </summary>
        /// <param name="alternativeFactory">A factory function to map the exceptional value to an alternative value.</param>
        /// <returns>A new optional, containing either the existing or alternative value.</returns>
        public Option<T> Or(Func<Exception, T> alternativeFactory)
        {
            if (alternativeFactory == null)
                throw InvalidAlternativeFactoryReference(nameof(alternativeFactory));

            return HasValue ? this : Option.Some(alternativeFactory(Exception));
        }
        
        public Option<T> Else(Option<T> alternativeOption) => HasValue ? this : alternativeOption;
        
        /// <summary>
        /// Uses an alternative optional, if no existing value is present.
        /// </summary>
        /// <param name="alternativeOptionFactory">A factory function to create an alternative optional.</param>
        /// <returns>The alternative optional, if no value is present, otherwise itself.</returns>
        public Option<T> Else(Func<Option<T>> alternativeOptionFactory)
        {
            if (alternativeOptionFactory == null)
                throw InvalidAlternativeFactoryReference(nameof(alternativeOptionFactory));

            return HasValue ? this : alternativeOptionFactory();
        }
        
        #endregion

        #region Match
        //###########
        
        /// <summary>
        /// Evaluates a specified function, based on whether a value is present or not.
        /// </summary>
        /// <param name="option"></param>
        /// <param name="some">The function to evaluate if the value is present.</param>
        /// <param name="none">The function to evaluate if the value is missing.</param>
        /// <returns>The result of the evaluated function.</returns>
        public TResult Match<TResult>(Func<T, TResult> some, Func<TResult> none)
        {
            if (some == null) throw new ArgumentNullException(nameof(some));
            if (none == null) throw new ArgumentNullException(nameof(none));
            return HasValue ? some(Value) : none();
        }
        
        /// <summary>
        /// Evaluates a specified action, based on whether a value is present or not.
        /// </summary>
        /// <param name="option"></param>
        /// <param name="some">The action to evaluate if the value is present.</param>
        /// <param name="none">The action to evaluate if the value is missing.</param>
        public void Match(Action<T> some, Action none)
        {
            if (some == null) throw new ArgumentNullException(nameof(some));
            if (none == null) throw new ArgumentNullException(nameof(none));

            if (HasValue)
            {
                some(Value);
                return;
            }

            none();
        }
        
        /// <summary>
        /// Evaluates a specified function, based on whether a value is present or not.
        /// </summary>
        /// <param name="some">The function to evaluate if the value is present.</param>
        /// <param name="none">The function to evaluate if the value is missing.</param>
        /// <returns>The result of the evaluated function.</returns>
        public TResult Match<TResult>(Func<T, TResult> some, Func<Exception, TResult> none)
        {
            if (some == null) throw new ArgumentNullException(nameof(some));
            if (none == null) throw new ArgumentNullException(nameof(none));
            return HasValue ? some(Value) : none(Exception);
        }
        
        /// <summary>
        /// Evaluates a specified action, based on whether a value is present or not.
        /// </summary>
        /// <param name="some">The action to evaluate if the value is present.</param>
        /// <param name="none">The action to evaluate if the value is missing.</param>
        public void Match(Action<T> some, Action<Exception> none)
        {
            if (some == null) throw new ArgumentNullException(nameof(some));
            if (none == null) throw new ArgumentNullException(nameof(none));

            if (HasValue)
            {
                some(Value);
            }
            else
            {
                none(Exception);
            }
        }
        
        /// <summary>
        /// Evaluates a specified action if a value is present.
        /// </summary>
        /// <param name="option"></param>
        /// <param name="some">The action to evaluate if the value is present.</param>
        public void MatchSome(Action<T> some)
        {
            if (some == null)
                throw new ArgumentNullException(nameof(some));

            if (HasValue)
                some(Value);
        }
        
        /// <summary>
        /// Evaluates a specified action if no value is present.
        /// </summary>
        /// <param name="option"></param>
        /// <param name="none">The action to evaluate if the value is missing.</param>
        public void MatchNone(Action none)
        {
            if (none == null)
                throw new ArgumentNullException(nameof(none));

            if (!HasValue)
                none();
        }
        
        /// <summary>
        /// Evaluates a specified action if no value is present.
        /// </summary>
        /// <param name="none">The action to evaluate if the value is missing.</param>
        public void MatchNone(Action<Exception> none)
        {
            if (none == null) 
                throw new ArgumentNullException(nameof(none));

            if (!HasValue)
                none(Exception);
        }

        #endregion
        
        #region Map
        //#########
        
        /// <summary>
        /// Transforms the inner value in an optional.
        /// If the instance is empty, an empty optional is returned.
        /// </summary>
        /// <param name="mapping">The transformation function.</param>
        /// <returns>The transformed optional.</returns>
        public Option<TResult> Map<TResult>(Func<T, TResult> mapping)
        {
            if (mapping == null) throw new ArgumentNullException(nameof(mapping));

            return Match(
                some: value => Option.Some(mapping(value)),
                none: Option.None<TResult>
            );
        }
        
        /// <summary>
        /// Transforms the exceptional value in an optional.
        /// If the instance is not empty, no transformation is carried out.
        /// </summary>
        /// <param name="mapping">The transformation function.</param>
        /// <returns>The transformed optional.</returns>
        public Option<T> MapException<TExceptionResult>(Func<Exception, TExceptionResult> mapping)
            where TExceptionResult : Exception
        {
            if (mapping == null) throw new ArgumentNullException(nameof(mapping));

            return Match(
                some: Option.Some<T>,
                none: exception => Option.None<T, TExceptionResult>(mapping(exception))
            );
        }
        
        /// <summary>
        /// Transforms the inner value in an optional
        /// into another optional. The result is flattened, 
        /// and if either is empty, an empty optional is returned.
        /// </summary>
        /// <param name="mapping">The transformation function.</param>
        /// <returns>The transformed optional.</returns>
        public Option<TResult> FlatMap<TResult>(Func<T, Option<TResult>> mapping)
        {
            if (mapping == null) throw new ArgumentNullException(nameof(mapping));

            return Match(
                some: mapping,
                none: Option.None<TResult>
            );
        }
        
        #endregion

        #region Filter
        //############

        /// <summary>
        /// Empties an optional if a specified condition
        /// is not satisfied.
        /// </summary>
        /// <param name="condition">The condition.</param>
        /// <returns>The filtered optional.</returns>
        public Option<T> Filter(bool condition) => HasValue && !condition ? Option.None<T>() : this;
        
        /// <summary>
        /// Empties an optional if a specified predicate
        /// is not satisfied.
        /// </summary>
        /// <param name="option"></param>
        /// <param name="predicate">The predicate.</param>
        /// <returns>The filtered optional.</returns>
        public Option<T> Filter(Func<T, bool> predicate)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            return HasValue && !predicate(Value) ? Option.None<T>() : this;
        }

        #endregion
    }
}
