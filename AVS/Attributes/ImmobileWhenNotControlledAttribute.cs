using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AVS.Attributes
{
    /// <summary>
    /// Declares that a vehicle cannot move when not controlled by the player.
    /// Affects the bed patcher.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ImmobileWhenNotControlledAttribute : Attribute
    {
    }
}
