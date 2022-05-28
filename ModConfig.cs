namespace SkillRings
{
  internal class ModConfig
  {
    public int tier1SkillRingPrice { get; set; } = 5000;

    public int tier2SkillRingPrice { get; set; } = 25000;

    public int tier1SkillRingBoost { get; set; } = 1;

    public int tier2SkillRingBoost { get; set; } = 2;

    public int tier3SkillRingBoost { get; set; } = 5;

    public double tier1ExperienceRingBoost { get; set; } = 0.1;

    public double tier2ExperienceRingBoost { get; set; } = 0.2;

    public double tier3ExperienceRingBoost { get; set; } = 0.5;
  }
}
