﻿/*
    [Reloaded] Mod Loader Common Library (libReloaded)
    The main library acting as common, shared code between the Reloaded Mod 
    Loader Launcher, Mods as well as plugins.
    Copyright (C) 2018  Sewer. Sz (Sewer56)

    [Reloaded] is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    [Reloaded] is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>
*/

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Reloaded.Process.Functions.X86Functions
{
    /// <summary>
    /// Class which stores the function information for custom functions
    /// to be called or hooked upon.
    /// See <see cref="CallingConventions" /> for information on settings
    /// for common calling conventions.
    /// </summary>
    public class ReloadedFunctionAttribute : Attribute
    {
        /// <summary>
        /// Specifies the registers in left to right parameter order to pass to the custom function to be called.
        /// </summary>
        public Register[] SourceRegisters { get; }

        /// <summary>
        /// Specifies the register to return the value from the funtion in (mov eax, source).
        /// This is typically eax.
        /// </summary>
        public Register ReturnRegister { get; }

        /// <summary>
        /// Defines the stack cleanup rule for the function.
        /// Callee: Stack pointer restored inside the game function we are executing.
        /// Caller: Stack pointer restored in our own wrapper function.
        /// </summary>
        public StackCleanup Cleanup { get; }

        /// <summary>
        /// Allocates an extra amount of uninitialized (not zero-written) stack space 
        /// for the function to use. Required by some compiler optimized functions.
        /// </summary>
        public int ReservedStackSpace { get; }

        /// <summary>
        /// Specifies the target X86 ISA register for a specific parameter.
        /// </summary>
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public enum Register
        {
            eax,
            ebx,
            ecx,
            edx,
            esi,
            edi,
            ebp,
            esp
        }
        
        /// <summary>
        /// Declares who performs the stack cleanup duty.
        /// </summary>
        public enum StackCleanup
        {
            None,
            Caller,
            Callee
        }

        /// <summary>
        /// Initializes a ReloadedFunction with its default parameters supplied in the constructor.
        /// </summary>
        /// <param name="sourceRegisters">Specifies the registers in left to right parameter order to pass to the custom function to be called.</param>
        /// <param name="returnRegister">Specifies the register to return the value from the funtion in (mov eax, source). This is typically eax.</param>
        /// <param name="stackCleanup">Defines the stack cleanup rule for the function. See <see cref="StackCleanup"/> for more details.</param>
        /// <param name="reservedStackSpace">Allocates an extra amount of uninitialized (not zero-written) stack space for the function to use when calling. Required by some compiler optimized functions.</param>
        public ReloadedFunctionAttribute(Register[] sourceRegisters, Register returnRegister, StackCleanup stackCleanup, int reservedStackSpace = 0)
        {
            SourceRegisters = sourceRegisters;
            ReturnRegister = returnRegister;
            Cleanup = stackCleanup;
            ReservedStackSpace = reservedStackSpace;
        }

        /// <summary>
        /// Initializes a ReloadedFunction with its default parameters supplied in the constructor.
        /// </summary>
        /// <param name="sourceRegister">Specifies the registers for the parameter.</param>
        /// <param name="returnRegister">Specifies the register to return the value from the funtion in (mov eax, source). This is typically eax.</param>
        /// <param name="stackCleanup">Defines the stack cleanup rule for the function. See <see cref="StackCleanup"/> for more details.</param>
        /// <param name="reservedStackSpace">Allocates an extra amount of uninitialized (not zero-written) stack space for the function to use when calling. Required by some compiler optimized functions.</param>
        public ReloadedFunctionAttribute(Register sourceRegister, Register returnRegister, StackCleanup stackCleanup, int reservedStackSpace = 0)
        {
            SourceRegisters = new[] { sourceRegister };
            ReturnRegister = returnRegister;
            Cleanup = stackCleanup;
            ReservedStackSpace = reservedStackSpace;
        }

        /// <summary>
        /// Initializes the ReloadedFunction using a preset calling convention.
        /// </summary>
        /// <param name="callingConvention">
        ///     The calling convention preset to use for instantiating the ReloadedFunction.
        ///     Please remember to mark your function delegate as [UnmanagedFunctionPointer(CallingConvention.Cdecl)],
        ///     mark only the ReloadedFunction Attribute with the true calling convention.
        /// </param>
        public ReloadedFunctionAttribute(CallingConventions callingConvention)
        {
            switch (callingConvention)
            {
                case CallingConventions.Cdecl:
                    SourceRegisters = new Register[0];
                    ReturnRegister = Register.eax;
                    Cleanup = StackCleanup.Caller;
                    break;

                case CallingConventions.Stdcall:
                    SourceRegisters = new Register[0];
                    ReturnRegister = Register.eax;
                    Cleanup = StackCleanup.Callee;
                    break;

                case CallingConventions.Fastcall:
                    SourceRegisters = new []{ Register.ecx, Register.edx };
                    ReturnRegister = Register.eax;
                    Cleanup = StackCleanup.Caller;
                    break;

                case CallingConventions.MicrosoftThiscall:
                    SourceRegisters = new[] { Register.ecx };
                    ReturnRegister = Register.eax;
                    Cleanup = StackCleanup.Callee;
                    break;

                case CallingConventions.GCCThiscall:
                    SourceRegisters = new Register[0];
                    ReturnRegister = Register.eax;
                    Cleanup = StackCleanup.Caller;
                    break;

                default:
                    Bindings.PrintError?.Invoke($"There is no preset for the specified calling convention {callingConvention.GetType().Name}");
                    break;
            }
        }

        /// <summary>
        /// Checks whether an instance of <see cref="ReloadedFunctionAttribute"/> has the same logical meaning
        /// as the current instance of <see cref="ReloadedFunctionAttribute"/>.
        /// </summary>
        /// <param name="obj">The <see cref="ReloadedFunctionAttribute"/> to compare to the current attribute.</param>
        /// <returns>True if both of the ReloadedFunctions are logically equivalent.</returns>
        public override bool Equals(Object obj)
        {
            // Check for type.
            ReloadedFunctionAttribute functionAttribute = obj as ReloadedFunctionAttribute;

            // Return false if null
            if (functionAttribute == null) return false;
            
            // Check by value.
            return functionAttribute.Cleanup == Cleanup &&
                   functionAttribute.ReturnRegister == ReturnRegister &&
                   functionAttribute.SourceRegisters.SequenceEqual(SourceRegisters);
        }

        /// <summary>
        /// Retrieves the HashCode for the current object.
        /// </summary>
        /// <returns>The hashcode for the current object.</returns>
        public override int GetHashCode()
        {
            // Stores the initial value.
            int initialHash = 13;

            // Calculate hash.
            foreach (Register register in SourceRegisters)
            { initialHash = (initialHash * 7) + (int)register; }
            
            initialHash = (initialHash * 7) + (int)ReturnRegister;
            initialHash = (initialHash * 7) + (int)Cleanup;

            // Return
            return initialHash;
        }
    }
}
