// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Reflection.Metadata;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.Emit;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal static class CompilerIntrinsicHandler
    {
        public static void HandleIntrinsic(
            PEModuleBuilder _moduleBeingBuiltOpt,
            MethodSymbol methodSymbol,
            VariableSlotAllocator lazyVariableSlotAllocator,
            StateMachineTypeSymbol stateMachineTypeOpt,
            ImportChain importChain,
            DiagnosticBag diagsForCurrentMethod)
        {
            var builder = new ILBuilder(_moduleBeingBuiltOpt, new LocalSlotManager(lazyVariableSlotAllocator), OptimizationLevel.Release, true)
            {
                RealizedExceptionHandlers = ImmutableArray<ExceptionHandlerRegion>.Empty
            };

            int sigRowId = 0;
            var callingConvention = CallingConvention.Default;

            var name = methodSymbol.Name;
            if (name.StartsWith("CallIndirect"))
            {
                sigRowId = HandleCallIndirectIntrinsic(_moduleBeingBuiltOpt, methodSymbol, builder, false, out callingConvention);
            }
            else if (name.StartsWith("TailCallIndirect"))
            {
                sigRowId = HandleCallIndirectIntrinsic(_moduleBeingBuiltOpt, methodSymbol, builder, true, out callingConvention);
            }

            builder.Realize();

            _moduleBeingBuiltOpt.SetMethodBody(methodSymbol.PartialDefinitionPart ?? methodSymbol, new MethodBody(
                ilBits: builder.RealizedIL,
                maxStack: builder.MaxStack,
                parent: methodSymbol.PartialDefinitionPart ?? methodSymbol,
                methodId: new DebugId(-1, _moduleBeingBuiltOpt.CurrentGenerationOrdinal),
                locals: builder.LocalSlotManager.LocalsInOrder(),
                sequencePoints: builder.RealizedSequencePoints,
                debugDocumentProvider: null,
                exceptionHandlers: builder.RealizedExceptionHandlers,
                areLocalsZeroed: true,
                hasStackalloc: false,
                localScopes: builder.GetAllScopes(),
                hasDynamicLocalVariables: builder.HasDynamicLocal,
                importScopeOpt: importChain?.Translate(_moduleBeingBuiltOpt, diagsForCurrentMethod),
                lambdaDebugInfo: ImmutableArray<LambdaDebugInfo>.Empty,
                closureDebugInfo: ImmutableArray<ClosureDebugInfo>.Empty,
                stateMachineTypeNameOpt: stateMachineTypeOpt?.Name,
                stateMachineHoistedLocalScopes: default(ImmutableArray<StateMachineHoistedLocalScope>),
                stateMachineHoistedLocalSlots: default(ImmutableArray<EncHoistedLocalInfo>),
                stateMachineAwaiterSlots: default(ImmutableArray<ITypeReference>),
                stateMachineMoveNextDebugInfoOpt: null,
                dynamicAnalysisDataOpt: null,
                standAloneSignatureRowId: sigRowId,
                indirectCallCallingConvention: callingConvention));

            builder.FreeBasicBlocks();
        }

        public static int HandleCallIndirectIntrinsic(PEModuleBuilder _moduleBeingBuiltOpt, MethodSymbol methodSymbol, ILBuilder builder, bool isTailPrefixed, out Cci.CallingConvention callingConvention)
        {
            for (int i = 0; i < methodSymbol.ParameterCount; ++i)
            {
                builder.EmitLoad(i);
            }

            int stackAdjustment = methodSymbol.ReturnsVoid ? 0 : 1;
            stackAdjustment -= methodSymbol.ParameterCount;

            if (isTailPrefixed)
            {
                builder.EmitOpCode(ILOpCode.Tail);
            }

            int sigRowId = _moduleBeingBuiltOpt.GetAndIncrementCurrentStandAloneSigRowId;

            builder.EmitOpCode(ILOpCode.Calli, stackAdjustment);
            builder.EmitRawToken(0x11000000 + (uint)sigRowId);
            builder.EmitRet(methodSymbol.ReturnsVoid);

            if (methodSymbol.Name.EndsWith("CDecl"))
            {
                callingConvention = CallingConvention.C;
            }
            else if (methodSymbol.Name.EndsWith("StdCall"))
            {
                callingConvention = CallingConvention.Standard;
            }
            else if (methodSymbol.Name.EndsWith("FastCall"))
            {
                callingConvention = CallingConvention.FastCall;
            }
            else if (methodSymbol.Name.EndsWith("HasThis"))
            {
                callingConvention = CallingConvention.HasThis;
            }
            else if (methodSymbol.Name.EndsWith("ThisCall"))
            {
                callingConvention = CallingConvention.ThisCall;
            }
            else if (methodSymbol.Name.EndsWith("Generic"))
            {
                callingConvention = CallingConvention.Generic;
            }
            else
            {
                callingConvention = CallingConvention.Default;
            }

            return sigRowId;
        }
    }
}
