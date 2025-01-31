﻿using lowlevel_benchmark.Helpers;
using Microsoft.Toolkit.HighPerformance.Helpers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace lowlevel_benchmark;

/// <summary>
/// various extension methods munged together for use in benchmarking
/// </summary>
public static class zz_Extensions
{
	//public delegate void Action_Ref<T1, T2>(ref T1 arg1, ref T2 arg2);
	public unsafe delegate void Action_PData<TData>(TData* pData, int index) where TData : unmanaged;

	public static unsafe void _ParallelForEach<TData>(this Span<TData> inputSpan, Action_PData<TData> parallelAction) where TData : unmanaged
	{
		fixed (TData* pSpan = inputSpan)
		{
			var actionStruct = new _ParallelForEach_ActionHelper_Span<TData>(pSpan, parallelAction);
			ParallelHelper.For(0, inputSpan.Length, in actionStruct);
		}
	}

	private unsafe readonly struct _ParallelForEach_ActionHelper_Span<TData> : IAction where TData : unmanaged
	{
		public readonly TData* pSpan;
		public readonly Action_PData<TData> parallelAction;

		public _ParallelForEach_ActionHelper_Span(TData* pSpan, Action_PData<TData> parallelAction)
		{
			this.pSpan = pSpan;
			this.parallelAction = parallelAction;
		}

		public void Invoke(int index)
		{
			//Using delegate pointer invoke, Because action is a readonly field,
			//but Invoke is an interface method where the compiler can't see it's actually readonly in all implementing types,
			//so it emits a defensive copies. This skips that 
			Unsafe.AsRef(parallelAction).Invoke(pSpan, index);
		}
	}

	public static void _ParallelForEach<TData>(this TData[] inputArray, int start, int endExclusive, Action<TData[], int> parallelAction) where TData : unmanaged
	{
		var actionStruct = new _ParallelForEach_ActionHelper_Array<TData>(inputArray, parallelAction);
		ParallelHelper.For(start, endExclusive, in actionStruct);
	}


	private unsafe readonly struct _ParallelForEach_ActionHelper_Array<TData> : IAction where TData : unmanaged
	{
		public readonly TData[] array;
		public readonly Action<TData[], int> parallelAction;

		public _ParallelForEach_ActionHelper_Array(TData[] array, Action<TData[], int> parallelAction)
		{
			this.array = array;
			this.parallelAction = parallelAction;
		}

		public void Invoke(int index)
		{
			//Using delegate pointer invoke, Because action is a readonly field,
			//but Invoke is an interface method where the compiler can't see it's actually readonly in all implementing types,
			//so it emits a defensive copies. This skips that 
			Unsafe.AsRef(parallelAction).Invoke(array, index);
		}
	}

	public static bool TryRemove<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, out TValue value) where TKey : notnull
	{
		var toReturn = dict.TryGetValue(key, out value);
		if (toReturn == true)
		{
			dict.Remove(key);
		}
		return toReturn;
	}

	public static void _Randomize<T>(this T[] target)
	{
		//lock (_rand)
		{
			for (var index = 0; index < target.Length; index++)
			{
				var swapIndex = __.Rand.Next(0, target.Length);
				var value = target[index];
				target[index] = target[swapIndex];
				target[swapIndex] = value;
			}
		}
	}

	/// <summary>
	/// warning: do not modify list while enumerating span
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Span<T> _AsSpan_Unsafe_Inline<T>(this List<T> list)
	{
		return CollectionsMarshal.AsSpan(list);
	}

	/// <summary>
	/// warning: do not modify list while enumerating span
	/// </summary>
	public static Span<T> _AsSpan_Unsafe<T>(this List<T> list)
	{
		return CollectionsMarshal.AsSpan(list);
	}

}
