using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using static Optional.NetFramework.ThrowHelper;

namespace Optional.NetFramework
{
    public static class Result
    {
        /// <summary>
        /// Wraps an existing value in an Result<T> instance.
        /// </summary>
        /// <typeparam name="T">The type of the value to be wrapped.</typeparam>
        /// <param name="value">The value to be wrapped.</param>
        /// <returns>An optional containing the specified valued.</returns>
        public static Result<T> Ok<T>(T value) => new Result<T>(value, true, null);

        public static Result<T, TException> Ok<T, TException>(T value) => new Result<T, TException>(value, true, default);
        
        public static Result<T> Err<T>(Exception exception = null) => new Result<T>(default, false, exception);

        public static Result<T, TException> Err<T, TException>(TException exception) => new Result<T, TException>(default, false, exception);
    }

    public readonly struct Result<T> : IResult<T>, IEquatable<Result<T>>, IComparable<Result<T>>
    {
        public readonly bool HasValue;
        public readonly T Value;
        public readonly Exception Exception;

        public bool IsOk => HasValue;
        public bool IsErr => !HasValue;
        public bool HasException => Exception != null;

        public IDictionary<object, object> Data { get; }

        internal Result(T value, bool hasValue, Exception exception)
        {
            Value     = value;
            HasValue  = hasValue;
            Exception = exception;
            Data      = new Dictionary<object, object>();
        }

        public override string ToString()
        {
            if (!HasValue) return Exception == null ? "Err" : "Err with Exception";
            return Value == null ? "Some(null)" : $"Some({Value})";
        }

        public Result<T> WithException(Exception exception)
        {
            return HasValue
                ? Result.Ok(Value).AdoptData(this)
                : Result.Err<T>(exception).AdoptData(this);
        }

        public Result<T, TException> WithException<TException>(TException exception)
        {
            return HasValue
                ? Result.Ok<T, TException>(Value).AdoptData(this)
                : Result.Err<T, TException>(exception).AdoptData(this);
        }

        public Result<T, TException> WithException<TException>(Func<TException> exceptionFactory)
        {
            if (exceptionFactory == null)
                throw new ArgumentNullException(nameof(exceptionFactory));

            return HasValue
                ? Result.Ok<T, TException>(Value)
                : new Result<T, TException>(default, false, exceptionFactory());
        }

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

        #region IEquatable<Result<T>> Implementation
        //##########################################

        public bool Equals(Result<T> other)
        {
            return HasValue == other.HasValue && EqualityComparer<T>.Default.Equals(Value, other.Value) && Equals(Exception, other.Exception);
        }

        public override bool Equals(object obj)
        {
            return obj is Result<T> other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = HasValue.GetHashCode();
                hashCode = (hashCode * 397) ^ EqualityComparer<T>.Default.GetHashCode(Value);
                hashCode = (hashCode * 397) ^ (Exception != null ? Exception.GetHashCode() : 0);
                return hashCode;
            }
        }

        #endregion

        #region IComparable<Result<T>> Implementation
        //###########################################

        public int CompareTo(Result<T> other)
        {
            return HasValue.CompareTo(other.HasValue);
        }

        #endregion

        #region Compare operators
        //#######################

        /// <summary>Checks whether two optional objects are identical.</summary>
        public static bool operator ==(Result<T> left, Result<T> right) => left.Equals(right);
        /// <summary>Checks whether two optional objects are not identical.</summary>
        public static bool operator !=(Result<T> left, Result<T> right) => !left.Equals(right);

        public static bool operator <(Result<T> left, Result<T> right) => left.CompareTo(right) < 0;
        public static bool operator >(Result<T> left, Result<T> right) => left.CompareTo(right) > 0;
        public static bool operator <=(Result<T> left, Result<T> right) => left.CompareTo(right) <= 0;
        public static bool operator >=(Result<T> left, Result<T> right) => left.CompareTo(right) >= 0;

        #endregion

        #region ValueOr / Or / Else
        //#########################

        public T ValueOr(Result<T> alternative) => HasValue ? Value : alternative.Value;

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
        public Result<T> Or(Func<Exception, T> alternativeFactory)
        {
            if (alternativeFactory == null)
                throw InvalidAlternativeFactoryReference(nameof(alternativeFactory));

            return HasValue ? this : Result.Ok(alternativeFactory(Exception));
        }

        public Result<T> Else(Result<T> alternativeResult) => HasValue ? this : alternativeResult;

        /// <summary>
        /// Uses an alternative optional, if no existing value is present.
        /// </summary>
        /// <param name="alternativeOptionFactory">A factory function to create an alternative optional.</param>
        /// <returns>The alternative optional, if no value is present, otherwise itself.</returns>
        public Result<T> Else(Func<Result<T>> alternativeResultFactory)
        {
            if (alternativeResultFactory == null)
                throw InvalidAlternativeFactoryReference(nameof(alternativeResultFactory));

            return HasValue ? this : alternativeResultFactory();
        }

        #endregion

        #region Match
        //###########

        /// <summary>
        /// Evaluates a specified function, based on whether a value is present or not.
        /// </summary>
        /// <param name="option"></param>
        /// <param name="ok">The function to evaluate if the value is present.</param>
        /// <param name="err">The function to evaluate if the value is missing.</param>
        /// <returns>The result of the evaluated function.</returns>
        public TResult Match<TResult>(Func<T, TResult> ok, Func<Exception, TResult> err)
        {
            if (ok == null) throw new ArgumentNullException(nameof(ok));
            if (err == null) throw new ArgumentNullException(nameof(err));
            return HasValue ? ok(Value) : err(Exception);
        }

        /// <summary>
        /// Evaluates a specified action, based on whether a value is present or not.
        /// </summary>
        /// <param name="option"></param>
        /// <param name="ok">The action to evaluate if the value is present.</param>
        /// <param name="err">The action to evaluate if the value is missing.</param>
        public void Match(Action<T> ok, Action<Exception> err)
        {
            if (ok == null) throw new ArgumentNullException(nameof(ok));
            if (err == null) throw new ArgumentNullException(nameof(err));

            if (HasValue)
            {
                ok(Value);
                return;
            }

            err(Exception);
        }

        #endregion
    }

    public readonly struct Result<T, TException> : IResult<T>, IEquatable<Result<T,  TException>>, IComparable<Result<T, TException>>
    {
        public readonly bool HasValue;
        public readonly T Value;
        public readonly TException Exception;

        public bool IsOk => HasValue;
        public bool IsErr => !HasValue;
        public bool HasException => Exception != null;

        public IDictionary<object, object> Data { get; }

        internal Result(T value, bool hasValue, TException exception)
        {
            Value     = value;
            HasValue  = hasValue;
            Exception = exception;
            Data      = new Dictionary<object, object>();
        }

        public override string ToString()
        {
            if (!HasValue) return Exception == null ? "Err" : "Err with Exception";
            return Value == null ? "Some(null)" : $"Some({Value})";
        }

        public Result<T> WithoutException() => 
            HasValue ? Result.Ok<T>(Value) : Result.Err<T>(Exception as Exception);

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

        #region IEquatable<Result<T,  TException>> Implementation
        //######################################################

        public bool Equals(Result<T, TException> other)
        {
            return HasValue == other.HasValue && EqualityComparer<T>.Default.Equals(Value, other.Value) &&
                   EqualityComparer<TException>.Default.Equals(Exception, other.Exception);
        }

        public override bool Equals(object obj)
        {
            return obj is Result<T, TException> other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = HasValue.GetHashCode();
                hashCode = (hashCode * 397) ^ EqualityComparer<T>.Default.GetHashCode(Value);
                hashCode = (hashCode * 397) ^ EqualityComparer<TException>.Default.GetHashCode(Exception);
                return hashCode;
            }
        }

        #endregion

        #region IComparable<Result<T, TException>> Implementation
        //#######################################################

        public int CompareTo(Result<T, TException> other)
        {
            return HasValue.CompareTo(other.HasValue);
        }

        #endregion

        #region Compare operators
        //#######################

        /// <summary>Checks whether two optional objects are identical.</summary>
        public static bool operator ==(Result<T, TException> left, Result<T, TException> right) => left.Equals(right);
        /// <summary>Checks whether two optional objects are not identical.</summary>
        public static bool operator !=(Result<T, TException> left, Result<T, TException> right) => !left.Equals(right);

        public static bool operator <(Result<T, TException> left, Result<T, TException> right) => left.CompareTo(right) < 0;
        public static bool operator >(Result<T, TException> left, Result<T, TException> right) => left.CompareTo(right) > 0;
        public static bool operator <=(Result<T, TException> left, Result<T, TException> right) => left.CompareTo(right) <= 0;
        public static bool operator >=(Result<T, TException> left, Result<T, TException> right) => left.CompareTo(right) >= 0;

        #endregion

        #region ValueOr / Or / Else
        //#########################

        public T ValueOr(Result<T> alternative) => HasValue ? Value : alternative.Value;

        public T ValueOr(Result<T, TException> alternative) => HasValue ? Value : alternative.Value;

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
        public T ValueOr(Func<TException, T> alternativeFactory)
        {
            if (alternativeFactory == null)
                throw InvalidAlternativeFactoryReference(nameof(alternativeFactory));

            return HasValue ? Value : alternativeFactory(Exception);
        }
        
        public Result<T, TException> Else(Result<T, TException> alternativeResult) => HasValue ? this : alternativeResult;

        /// <summary>
        /// Uses an alternative optional, if no existing value is present.
        /// </summary>
        /// <param name="alternativeOptionFactory">A factory function to create an alternative optional.</param>
        /// <returns>The alternative optional, if no value is present, otherwise itself.</returns>
        public Result<T, TException> Else(Func<TException, Result<T, TException>> alternativeResultFactory)
        {
            if (alternativeResultFactory == null)
                throw InvalidAlternativeFactoryReference(nameof(alternativeResultFactory));

            return HasValue ? this : alternativeResultFactory(Exception);
        }

        #endregion

        #region Match
        //###########

        /// <summary>
        /// Evaluates a specified function, based on whether a value is present or not.
        /// </summary>
        /// <param name="option"></param>
        /// <param name="ok">The function to evaluate if the value is present.</param>
        /// <param name="err">The function to evaluate if the value is missing.</param>
        /// <returns>The result of the evaluated function.</returns>
        public TResult Match<TResult>(Func<T, TResult> ok, Func<TException, TResult> err)
        {
            if (ok == null) throw new ArgumentNullException(nameof(ok));
            if (err == null) throw new ArgumentNullException(nameof(err));
            return HasValue ? ok(Value) : err(Exception);
        }

        /// <summary>
        /// Evaluates a specified action, based on whether a value is present or not.
        /// </summary>
        /// <param name="option"></param>
        /// <param name="ok">The action to evaluate if the value is present.</param>
        /// <param name="err">The action to evaluate if the value is missing.</param>
        public void Match(Action<T, TException> ok, Action<TException> err)
        {
            if (ok == null) throw new ArgumentNullException(nameof(ok));
            if (err == null) throw new ArgumentNullException(nameof(err));

            if (HasValue)
            {
                ok(Value,Exception);
                return;
            }

            err(Exception);
        }

        #endregion
    }

    public interface IResult<T>
    {
        bool IsOk { get; }
        bool IsErr { get; }
        bool HasException { get; }

        IDictionary<object, object> Data { get; }
    }

    internal static class ResultDataExtensions
    {
        public static Result<T> AdoptData<T>(this Result<T> target, IResult<T> source)
        {
            foreach (var valuePair in source.Data)
            {
                target.Data.Add(valuePair);
            }
            return target;
        }

        public static Result<T, TException> AdoptData<T, TException>(this Result<T, TException> target, IResult<T> source)
        {
            foreach (var valuePair in source.Data)
            {
                target.Data.Add(valuePair);
            }
            return target;
        }
    }
}
