using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Ladybug.Core
{
    /// <summary>
    /// Represents a single register and the corresponding value inside a thread.
    /// </summary>
    public interface IRegister
    {
        /// <summary>
        /// Gets the name of the register.
        /// </summary>
        string Name
        {
            get;
        }

        /// <summary>
        /// Gets the size in bits of the register.
        /// </summary>
        int Size
        {
            get;
        }

        /// <summary>
        /// Gets or sets the current value of the register.
        /// </summary>
        object Value
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Represents a register that is built up from other smaller registers.
    /// </summary>
    public interface ICompoundedRegister : IRegister
    {
        /// <summary>
        /// Gets a collection of all the child registers that build up the compounded register.  
        /// </summary>
        IList<IChildRegister> Parts
        {
            get;
        }
    }

    /// <summary>
    /// Represents a register that is part of a bigger register.
    /// </summary>
    public interface IChildRegister : IRegister
    {
        ICompoundedRegister ParentRegister
        {
            get;
        }
    }

    /// <summary>
    /// Represents a simple one value register.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Register<T> : IRegister
        where T: struct 
    {
        public Register(string name, T value)
        {
            Name = name;
            Size = Marshal.SizeOf(typeof(T)) * 8;
            Value = value;
        }

        /// <inheritdoc />
        public string Name
        {
            get;
        }

        /// <inheritdoc />
        public int Size
        {
            get;
        }
        
        /// <summary>
        /// Gets or sets the current value of the register.
        /// </summary>
        public T Value
        {
            get;
            set;
        }

        object IRegister.Value
        {
            get { return Value; }
            set { Value = (T) value; }
        }
    }
}