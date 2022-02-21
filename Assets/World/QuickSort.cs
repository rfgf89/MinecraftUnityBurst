using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

public static class Sort
{

    public static void QuickSort<T>(ref List<T> arr, int offset, int size , Func<T, T, bool> matchP,Func<T, T, bool> matchM) {
        if (offset < size)
        {
            int q = Partition(offset, size, ref arr, matchP, matchM);
            QuickSort(ref arr, offset, q, matchP, matchM);
            QuickSort(ref arr, q + 1, size, matchP, matchM);
        }
    }
 
    public static int Partition<T>( int p, int r ,ref List<T> arr, Func<T, T, bool> matchP, Func<T, T, bool> matchM)
    {
        T x = arr[ p ];
        int i = p - 1;
        int j = r + 1;
        while ( true ) {
            do {
                j--;
                
            }
            while ( matchP(arr[ j ], x));
            do {
                i++;
            }
            while ( matchM(arr[ i ], x));
            if ( i < j ) {
                T tmp = arr[ i ];
                arr[ i ] = arr[ j ];
                arr[ j ] = tmp;
            }
            else {
                return j;
            }
        }
    }

    public static void QuickSort<T>(ref List<T> array, Func<T, T, bool> matchP, Func<T, T, bool> matchM)
    {
        if ( array.Count < 1 ) 
            return;
        QuickSort(ref array, 0, array.Count - 1, matchP, matchM);
    }
    
 
}

public static class Function
{
    public static unsafe T[] ToArray<U,T>( NativeArray<U> arrIn,ref T[] arrOut) where U : struct  where T : struct
    {
        void* unsafeVals = NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(arrIn);
 
        
        ulong handle;
        var arrOutPtr = UnsafeUtility.PinGCArrayAndGetDataAddress(arrOut, out handle);
 
        UnsafeUtility.MemCpy(arrOutPtr, unsafeVals, arrOut.Length * UnsafeUtility.SizeOf<T>());
 
        UnsafeUtility.ReleaseGCObject(handle);
        return arrOut;
    }

    public static unsafe T[] ToArray<U,T>(ref NativeArray<U> arrIn,ref T[] arrOut) where U : struct  where T : struct
    {
        void* unsafeVals = NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(arrIn);
 
        
        ulong handle;
        var arrOutPtr = UnsafeUtility.PinGCArrayAndGetDataAddress(arrOut, out handle);
 
        UnsafeUtility.MemCpy(arrOutPtr, unsafeVals, arrOut.Length * UnsafeUtility.SizeOf<T>());
 
        UnsafeUtility.ReleaseGCObject(handle);
        return arrOut;
    }
    

    public static unsafe NativeArray<T> ToArray<U,T>(ref U[] arrIn,ref NativeArray<T> arrOut) where U : struct  where T : struct
    {
        void* unsafeVals = NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(arrOut);
 
        
        ulong handle;
        var arrOutPtr = UnsafeUtility.PinGCArrayAndGetDataAddress(arrIn, out handle);
 
        UnsafeUtility.MemCpy(unsafeVals, arrOutPtr, arrIn.Length * UnsafeUtility.SizeOf<T>());
 
        UnsafeUtility.ReleaseGCObject(handle);
        return arrOut;
    } 
    
    public static int GetBits(int a)
    {
        int count = 0;
        for (int i = 0; i < a; i++)
        {
            count = a >> i;
        }

        return count;

    }

}