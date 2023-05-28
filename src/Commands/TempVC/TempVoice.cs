using AGC_Management.Helpers.TempVoice;

namespace AGC_Management.Commands.TempVC;

public class TempVoice : TempVoiceHelper
{
    private static List<ulong> LevelRoleIDs = new List<ulong>()
    {
        750402390691152005, 798562254408777739, 750450170189185024, 798555933089071154,
        750450342474416249, 750450621492101280, 798555135071617024, 751134108893184072,
        776055585912389673, 750458479793274950, 798554730988306483, 757683142894157904,
        810231454985486377, 810232899713630228, 810232892386705418
    };

    private static List<string> lookup = new List<string>()
    {
        "5+", "10+", "15+", "20+", "25+", "30+", "35+", "40+", "45+", "50+", "60+", "70+", "80+", "90+", "100+"
    };


}