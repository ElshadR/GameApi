using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MathematicGameApi.Infrastructure.Enums
{
    /// <summary>
    /// kullanicinin oyundaki online,ofline oynayirmi durumu
    /// </summary>
    public enum UserPosition
    {
        Online = 1,
        Offline = 2,
        AtGame = 3,
    }
}