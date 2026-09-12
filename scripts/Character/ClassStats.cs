using System;
using System.Collections.Generic;

namespace Tracefinder.Character
{
    
    public class ClassStats
    {
        public string Name { get; set; } = ""; // nazwa klasy postaci
        public List<AbilityScore> KeyAbilityOptions { get; set; } = new(); // 
        public int HitPointsPerLevel { get; set; } // ile hp na poziom ma postać
        public List<SkillName> TrainedSkills { get; set; } = new(); // Jakie skille typu medicine albo athelics jest wytrenowana
        public int BonusSkillCount { get; set; } // ile dodatkowych skillow moze postac sobie dobrac
        public ProficiencyRank PerceptionProficiency { get; set; }
        public ProficiencyRank FortitudeProficiency { get; set; }
        public ProficiencyRank ReflexProficiency { get; set; }
        public ProficiencyRank WillProficiency { get; set; }
        public ProficiencyRank ClassDcProficiency { get; set; }
        public ProficiencyRank UnarmoredProficiency { get; set; }
        public List<string> Level1Features { get; set; } = new();

    }






}

