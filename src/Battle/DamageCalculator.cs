using System;

namespace KnowledgeRPG.Battle
{
    /// <summary>
    /// Answer-speed damage tuning carried over from the Godot project.
    /// Fast answers increase player damage and reduce the enemy counterattack.
    /// </summary>
    public static class DamageCalculator
    {
        public static int ComputePlayerDamage(
            int baseDamage,
            double answerSeconds,
            double fastSeconds,
            double slowSeconds,
            double maxBonus)
        {
            if (baseDamage <= 0)
                return 0;

            if (answerSeconds < 0)
                return baseDamage;

            double speed = SpeedFactor(answerSeconds, fastSeconds, slowSeconds);
            return AddBonus(baseDamage, maxBonus, speed);
        }

        public static int ComputeEnemyDamage(
            int baseDamage,
            double answerSeconds,
            double fastSeconds,
            double slowSeconds,
            double maxBonus)
        {
            if (baseDamage <= 0)
                return 0;

            if (answerSeconds < 0)
                return baseDamage;

            double slow = 1.0 - SpeedFactor(answerSeconds, fastSeconds, slowSeconds);
            return AddBonus(baseDamage, maxBonus, slow);
        }

        private static double SpeedFactor(double answerSeconds, double fastSeconds, double slowSeconds)
        {
            fastSeconds = Math.Max(0, fastSeconds);
            slowSeconds = Math.Max(fastSeconds + 0.001, slowSeconds);

            double window = slowSeconds - fastSeconds;
            return Math.Clamp(1.0 - (answerSeconds - fastSeconds) / window, 0.0, 1.0);
        }

        private static int AddBonus(int baseDamage, double maxBonus, double factor)
        {
            maxBonus = Math.Max(0, maxBonus);

            int bonus = (int)Math.Round(baseDamage * maxBonus * factor, MidpointRounding.AwayFromZero);
            return baseDamage + Math.Max(0, bonus);
        }
    }
}
