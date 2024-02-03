﻿namespace AGC_Management.Enums.Web;

public enum AccessLevel
{
    BotOwner = 8,
    Administrator = 7,
    HeadModerator = 6,
    Moderator = 5,
    Supporter = 4,
    Team = 3,
    User = 2,
    NichtImServer = 1,
    Blacklisted = 0
}