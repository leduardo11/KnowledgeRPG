using KnowledgeRPG.Battle;
using Xunit;

namespace KnowledgeRPG.Tests;

public class DamageCalculatorTests
{
    private const double Fast = 1.5;
    private const double Slow = 5.0;

    [Theory]
    [InlineData(0.0, 15)]
    [InlineData(1.5, 15)]
    [InlineData(3.25, 13)]
    [InlineData(5.0, 10)]
    [InlineData(10.0, 10)]
    [InlineData(-1.0, 10)]
    public void PlayerDamageScalesWithAnswerSpeed(double answerSeconds, int expected)
    {
        int damage = DamageCalculator.ComputePlayerDamage(
            10, answerSeconds, Fast, Slow, 0.5);

        Assert.Equal(expected, damage);
    }

    [Theory]
    [InlineData(0.0, 6)]
    [InlineData(1.5, 6)]
    [InlineData(3.25, 7)]
    [InlineData(5.0, 8)]
    [InlineData(10.0, 8)]
    [InlineData(-1.0, 6)]
    public void EnemyDamageUsesInverseAnswerSpeed(double answerSeconds, int expected)
    {
        int damage = DamageCalculator.ComputeEnemyDamage(
            6, answerSeconds, Fast, Slow, 0.25);

        Assert.Equal(expected, damage);
    }

    [Fact]
    public void DisabledBonusKeepsBaseDamage()
    {
        Assert.Equal(10, DamageCalculator.ComputePlayerDamage(10, 0.0, Fast, Slow, 0));
        Assert.Equal(6, DamageCalculator.ComputeEnemyDamage(6, 10.0, Fast, Slow, 0));
    }

    [Fact]
    public void ZeroBaseDamageStaysZero()
    {
        Assert.Equal(0, DamageCalculator.ComputePlayerDamage(0, 0.0, Fast, Slow, 0.5));
        Assert.Equal(0, DamageCalculator.ComputeEnemyDamage(0, 10.0, Fast, Slow, 0.25));
    }
}
