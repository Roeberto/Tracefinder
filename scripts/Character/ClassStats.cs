using System;
using System.Collections.Generic;

namespace Tracefinder.Character
{
    
    public class ClassStats
    {
        public string Name { get; set; } = "";
        public List<AbilityScore> KeyAbilityOptions { get; set; } = new();
        public int HitPointsPerLevel { get; set; }
        public List<SkillName> TrainedSkills { get; set; } = new();
        public int BonusSkillCount { get; set; }
        public ProficiencyRank PerceptionProficiency { get; set; }
        public ProficiencyRank FortitudeProficiency { get; set; }
        public ProficiencyRank ReflexProficiency { get; set; }
        public ProficiencyRank WillProficiency { get; set; }
        public ProficiencyRank ClassDcProficiency { get; set; }
        public ProficiencyRank UnarmoredProficiency { get; set; }
        public List<string> Level1Features { get; set; } = new();

    }
    
}