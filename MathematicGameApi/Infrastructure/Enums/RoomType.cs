using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace MathematicGameApi.Infrastructure.Enums
{
    /// <summary>
    /// odalarin tipleri
    /// </summary>
    public enum RoomType
    {
        [Description("Kolay")]
        [EnumMember(Value = "Kolay")]
        Kolay = 1,
        [Description("Normal")]
        [EnumMember(Value = "Normal")]
        Normal = 2,
        [Description("Zor")]
        [EnumMember(Value = "Zor")]
        Zor = 3,
        [Description("Uzman")]
        [EnumMember(Value = "Uzman")]
        Uzman = 4
    }
}