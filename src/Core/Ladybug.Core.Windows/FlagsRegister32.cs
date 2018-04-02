using System.Collections.Generic;

namespace Ladybug.Core.Windows
{
    public class FlagsRegister32 : Register<uint>, ICompoundedRegister
    {
        public FlagsRegister32(uint value) 
            : base("eflags", value)
        {
            Parts = new IChildRegister[]
            {
                new FlagRegister(0, "cf", this),
                new FlagRegister(2, "pf", this),
                new FlagRegister(4, "af", this),
                new FlagRegister(6, "zf", this),
                new FlagRegister(7, "sf", this),
                new FlagRegister(8, "tf", this),
                new FlagRegister(9, "if", this),
                new FlagRegister(10, "df", this),
                new FlagRegister(11, "of", this),
                // TODO: IOPL
                new FlagRegister(14, "nt", this),
                new FlagRegister(16, "rf", this),
                new FlagRegister(17, "vm", this),
                new FlagRegister(18, "ac", this),
                new FlagRegister(19, "vif", this),
                new FlagRegister(20, "vip", this),
                new FlagRegister(21, "id", this),
            };
        }

        public IList<IChildRegister> Parts
        {
            get;
        }
    }
}