using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkillRings
{
    public interface IBlueCookingSkillAPI
    {
        bool AddExperienceDirectly(int experience);
        int GetTotalCurrentExperience();
    }
}
