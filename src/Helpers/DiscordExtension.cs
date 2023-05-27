using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DisCatSharp.Entities;

namespace AGC_Management.Helpers
{
    internal static class DiscordExtension
    {
        public static bool IsTimedOut(this DiscordMember mem)
            => mem.CommunicationDisabledUntil is not null && mem.CommunicationDisabledUntil.Value > DateTime.UtcNow;
    }
}
