﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using System.Collections.Immutable;
using System.ComponentModel;
using LanguageExt;
using static LanguageExt.Prelude;

namespace LanguageExt
{
    /// <summary>
    /// OptionUnsafe<T> can be in two states:
    ///     1. Some(x) -- which means there is a value stored inside
    ///     2. None    -- which means there's no value stored inside
    /// To extract the value you must use the 'matchUnsafe' function.
    /// This differs from Option<T> in that it allows Some(null) which
    /// is expressly forbidden for Option<T>.  That is what makes this
    /// type 'unsafe'.  
    /// </summary>
    [TypeConverter(typeof(OptionalTypeConverter))]
    public struct OptionUnsafe<T> : IOptionalValue, IEnumerable<T>
    {
        readonly T value;

        private OptionUnsafe(T value, bool isSome)
        {
            this.IsSome = isSome;
            this.value = value;
        }

        private OptionUnsafe(T value)
        {
            this.IsSome = true;
            this.value = value;
        }

        public static OptionUnsafe<T> Some(T value) =>
            new OptionUnsafe<T>(value, true);

        public static readonly OptionUnsafe<T> None = new OptionUnsafe<T>();

        public bool IsSome { get; }

        public bool IsNone =>
            !IsSome;

        internal T Value =>
            IsSome
                ? value
                : raise<T>(new OptionIsNoneException());

        public static implicit operator OptionUnsafe<T>(T value) =>
            Some(value);

        public static implicit operator OptionUnsafe<T>(OptionNone none) =>
            None;

        public R MatchUnsafe<R>(Func<T, R> Some, Func<R> None) =>
            IsSome
                ? Some(Value)
                : None();

        public object MatchUntyped(Func<object, object> Some, Func<object> None) =>
            IsSome
                ? Some(Value)
                : None();

        public Unit MatchUnsafe(Action<T> Some, Action None)
        {
            if (IsSome)
            {
                Some(Value);
            }
            else
            {
                None();
            }
            return Unit.Default;
        }

        /// <summary>
        /// Invokes the someHandler if OptionUnsafe is in the Some state, otherwise nothing
        /// happens.
        /// </summary>
        public Unit IfSomeUnsafe(Action<T> someHandler)
        {
            if (IsSome)
            {
                someHandler(value);
            }
            return unit;
        }

        public T IfNoneUnsafe(Func<T> None) =>
            MatchUnsafe(identity, None);

        public T IfNoneUnsafe(T noneValue) =>
            MatchUnsafe(identity, () => noneValue);

        [Obsolete("'FailureUnsafe' has been deprecated.  Please use 'IfNoneUnsafe' instead")]
        public T FailureUnsafe(Func<T> None) =>
            MatchUnsafe(identity, None);

        [Obsolete("'FailureUnsafe' has been deprecated.  Please use 'IfNoneUnsafe' instead")]
        public T FailureUnsafe(T noneValue) =>
            MatchUnsafe(identity, () => noneValue);

        public SomeUnsafeUnitContext<T> SomeUnsafe<R>(Action<T> someHandler) =>
            new SomeUnsafeUnitContext<T>(this, someHandler);

        public SomeUnsafeContext<T, R> SomeUnsafe<R>(Func<T, R> someHandler) =>
            new SomeUnsafeContext<T, R>(this, someHandler);

        public override string ToString() =>
            IsSome
                ? Value == null
                    ? "Some(null)"
                    : String.Format("Some({0})", Value)
                : "None";

        public override int GetHashCode() =>
            IsSome && Value != null
                ? Value.GetHashCode()
                : 0;

        public override bool Equals(object obj) =>
            obj is OptionUnsafe<T>
                ? map(this, (OptionUnsafe<T>)obj, (lhs, rhs) =>
                    lhs.IsNone && rhs.IsNone
                        ? true
                        : lhs.IsNone || rhs.IsNone
                            ? false
                            : lhs.Value == null
                                ? rhs.Value == null
                                : lhs.Value.Equals(rhs.Value))
                : IsSome
                    ? Value.Equals(obj)
                    : false;

        public int Count =>
            IsSome ? 1 : 0;

        public bool ForAllUnsafe(Func<T,bool> pred) =>
            IsSome
                ? pred(Value)
                : true;

        public S FoldUnsafe<S>(S state, Func<S, T, S> folder) =>
            IsSome
                ? folder(state, Value)
                : state;

        public bool ExistsUnsafe(Func<T,bool> pred) =>
            IsSome
                ? pred(Value)
                : false;

        public OptionUnsafe<R> MapUnsafe<R>(Func<T,R> mapper) =>
            IsSome
                ? OptionUnsafe<R>.Some(mapper(Value))
                : OptionUnsafe<R>.None;

        public bool FilterUnsafe(Func<T, bool> pred) =>
            ExistsUnsafe(pred);

        public OptionUnsafe<R> BindUnsafe<R>(Func<T, OptionUnsafe<R>> binder) =>
            IsSome
                ? binder(Value)
                : OptionUnsafe<R>.None;

        public IEnumerable<T> AsEnumerable()
        {
            if (IsSome)
            {
                yield return Value;
            }
        }

        public EitherUnsafe<L, T> ToEitherUnsafe<L>(L defaultLeftValue) =>
            IsSome
                ? RightUnsafe<L, T>(Value)
                : LeftUnsafe<L, T>(defaultLeftValue);

        public EitherUnsafe<L, T> ToEitherUnsafe<L>(Func<L> Left) =>
            IsSome
                ? RightUnsafe<L, T>(Value)
                : LeftUnsafe<L, T>(Left());

        public IEnumerator<T> GetEnumerator() =>
            AsEnumerable().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() =>
            AsEnumerable().GetEnumerator();

        public IImmutableList<T> ToList() =>
            Prelude.toList(AsEnumerable());

        public ImmutableArray<T> ToArray() =>
            Prelude.toArray(AsEnumerable());

        public static bool operator ==(OptionUnsafe<T> lhs, OptionUnsafe<T> rhs) =>
            lhs.Equals(rhs);

        public static bool operator !=(OptionUnsafe<T> lhs, OptionUnsafe<T> rhs) =>
            !lhs.Equals(rhs);

        public static OptionUnsafe<T> operator |(OptionUnsafe<T> lhs, OptionUnsafe<T> rhs) =>
            lhs.IsSome
                ? lhs
                : rhs;

        public static bool operator true(OptionUnsafe<T> value) =>
            value.IsSome;

        public static bool operator false(OptionUnsafe<T> value) =>
            value.IsNone;

        public Type GetUnderlyingType() =>
            typeof(T);
    }

    public struct SomeUnsafeContext<T, R>
    {
        readonly OptionUnsafe<T> option;
        readonly Func<T, R> someHandler;

        internal SomeUnsafeContext(OptionUnsafe<T> option, Func<T, R> someHandler)
        {
            this.option = option;
            this.someHandler = someHandler;
        }

        public R None(Func<R> noneHandler) =>
            matchUnsafe(option, someHandler, noneHandler);

        public R None(R noneValue) =>
            matchUnsafe(option, someHandler, () => noneValue);
    }

    public struct SomeUnsafeUnitContext<T>
    {
        readonly OptionUnsafe<T> option;
        readonly Action<T> someHandler;

        internal SomeUnsafeUnitContext(OptionUnsafe<T> option, Action<T> someHandler)
        {
            this.option = option;
            this.someHandler = someHandler;
        }

        public Unit None(Action noneHandler) =>
            matchUnsafe(option, someHandler, noneHandler);
    }

    internal static class OptionUnsafe
    {
        public static OptionUnsafe<T> Cast<T>(T value) =>
            value == null
                ? OptionUnsafe<T>.None
                : OptionUnsafe<T>.Some(value);


        public static OptionUnsafe<T> Cast<T>(Nullable<T> value) where T : struct =>
            value == null
                ? OptionUnsafe<T>.None
                : OptionUnsafe<T>.Some(value.Value);
    }
}

public static class __OptionUnsafeExt
{
    public static OptionUnsafe<U> Select<T, U>(this OptionUnsafe<T> self, Func<T, U> map) => 
        self.MapUnsafe(map);

    public static OptionUnsafe<V> SelectMany<T, U, V>(this OptionUnsafe<T> self,
        Func<T, OptionUnsafe<U>> bind,
        Func<T, U, V> project
        ) =>
        matchUnsafe(self,
            Some: t =>
                matchUnsafe(bind(t),
                    Some: u => OptionUnsafe.Cast<V>(project(t, u)),
                    None: () => OptionUnsafe<V>.None
                ),
            None: () => OptionUnsafe<V>.None
            );

    public static OptionUnsafe<T> Where<T>(this OptionUnsafe<T> self, Func<T, bool> pred) =>
        self.FilterUnsafe(pred)
            ? self
            : None;
}
