using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AGC_Management.Helper;

public class Converter
{
    public static void SeperateIdsAndReason(string ids_and_reason, out List<ulong> ids, out string reason)
    {
        ids = new List<ulong>();
        reason = "";
        string[] parts = ids_and_reason.Split(' ');
        bool isReasonStarted = false;

        foreach (string part in parts)
        {
            if (!isReasonStarted)
            {
                if (part.StartsWith("<@") && part.EndsWith(">"))
                {
                    string idString = part.Substring(2, part.Length - 3);
                    if (ulong.TryParse(idString, out ulong id))
                    {
                        ids.Add(id);
                    }
                    else
                    {
                        break;
                    }
                }
                else if (ulong.TryParse(part, out ulong id))
                {
                    ids.Add(id);
                }
                else
                {
                    isReasonStarted = true;
                    reason += part + " ";
                }
            }
            else
            {
                reason += part + " ";
            }
        }
    }
}
