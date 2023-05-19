using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DisCatSharp.CommandsNext;
using DisCatSharp.CommandsNext.Attributes;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using DisCatSharp.Exceptions;
using IniParser;
using IniParser.Model;
using DisCatSharp.Interactivity;
using DisCatSharp.Interactivity.Extensions;


namespace AGC_Management.Helper
{
    public class RequireStaffRole : CheckBaseAttribute
    {

        private ulong RoleId = ulong.Parse(GlobalProperties.ConfigIni["ServerConfig"]["StaffRoleId"]);
        public override async Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
        {
            
            // Check if user has staff role
            if (ctx.Member.Roles.Any(r => r.Id == RoleId))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}