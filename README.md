## Welcome to the .NET Compiler Platform ("Roslyn")

Original repo: [dotnet/roslyn](https://github.com/dotnet/roslyn/)

This is a fork of Roslyn that allows to use intrinsic functions.

Initially written by Mukul Sabharwal [mjsabby/roslyn/compilerintrinsics_2_8_2](https://github.com/mjsabby/roslyn/tree/compilerintrinsics_2_8_2)

I have merged his changes with current upstream (Feb 4th 2020).

## Building
First time you need to run

    ./build.sh -r

Later you can omit the `-r` (`--restore`).

### Usage
You can invoke the compiler by:

* Using `mono` with `artifacts/bin/csc/Debug/net472/csc.exe -noconfig`
* Copy over `csc.*` and `Microsoft.Code.*` from `artifacts/bin/csc/Debug/netcoreappX.X/` to `/usr/share/dotnet/sdk/<version>/Roslyn/bincore`
    (make a backup first)

To use intrinsic functions in your code you can use

    namespace System.Runtime.CompilerServices
    {
        [AttributeUsage(AttributeTargets.Method)]
        internal class CompilerIntrinsicAttribute : Attribute { }
    }

    class Program
    {
        [CompilerIntrinsic]
        static unsafe extern void* LoadFunctionPointer(DelegateType target);  // e.g. DelegateType = Func<int, int>

        [CompilerIntrinsic]
        static unsafe extern void* LoadVirtualFunctionPointer(DelegateType target);

        [CompilerIntrinsic]
        static unsafe extern ReturnType CallIndirect(Type1 p1, ..., TypeN pN, void* managedFunctionPointer);

        [CompilerIntrinsic]
        static unsafe extern ReturnType TailCallIndirect(Type1 p1, ..., TypeN pN, void* managedFunctionPointer);

        [CompilerIntrinsic]
        static unsafe extern ReturnType CallIndirectCDecl(Type1 p1, ..., TypeN pN, void* nativeFunctionPointer);

        [CompilerIntrinsic]
        static unsafe extern ReturnType CallIndirectStdCall(Type1 p1, ..., TypeN pN, void* nativeFunctionPointer);

        [CompilerIntrinsic]
        static unsafe extern ReturnType CallIndirectThisCall(Type1 p1, ..., TypeN pN, void* nativeFunctionPointer);

        [CompilerIntrinsic]
        static unsafe extern DataType* AddressOfMappedData(DataType[] mappedData); // adds entry to FieldRVA table
    }

For more take a look at the code changes, it's surprisingly easy to follow :)
