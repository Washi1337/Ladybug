using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Ladybug.Core
{
    public interface IRegister
    {
        string Name
        {
            get;
        }

        int Size
        {
            get;
        }

        object Value
        {
            get;
            set;
        }
    }

    public interface ICompoundedRegister : IRegister
    {
        IList<IChildRegister> Parts
        {
            get;
        }
    }

    public interface IChildRegister : IRegister
    {
        ICompoundedRegister ParentRegister
        {
            get;
        }
    }

    public class Register<T> : IRegister
        where T: struct 
    {
        public Register(string name, T value)
        {
            Name = name;
            Size = Marshal.SizeOf(typeof(T)) * 8;
            Value = value;
        }
        
        public string Name
        {
            get;
        }

        public int Size
        {
            get;
        }

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