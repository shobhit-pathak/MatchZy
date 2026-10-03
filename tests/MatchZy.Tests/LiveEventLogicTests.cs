using MatchZy;

namespace MatchZy.Tests;

public class LiveEventLogicTests
{
    [Theory]
    [InlineData("ak47", 27)]
    [InlineData("weapon_ak47", 27)]
    [InlineData("knife", 28)]
    [InlineData("knife_t", 59)]
    [InlineData("knife_karambit", 507)]
    [InlineData("usp_silencer", 61)]
    [InlineData("m4a1_silencer", 60)]
    [InlineData("hkp2000", 41)]
    [InlineData("awp", 17)]
    [InlineData("hegrenade", 4)]
    [InlineData("inferno", 0)]     // fire, as in Get5
    [InlineData("planted_c4", 0)]  // the bomb, as in Get5
    [InlineData("mp5sd", 0)]       // not in SourceMod's list
    [InlineData("world", 0)]
    [InlineData("", 0)]
    [InlineData(null, 0)]
    public void WeaponIds(string? weapon, int expected) => Assert.Equal(expected, LiveEventLogic.WeaponId(weapon));

    [Theory]
    [InlineData(true, false, false, false)]  // a kill
    [InlineData(true, true, false, true)]    // own grenade, kill command
    [InlineData(false, false, false, true)]  // fall damage, world
    [InlineData(false, false, true, false)]  // the bomb is not a suicide
    public void Suicide(bool hasAttacker, bool attackerIsVictim, bool bomb, bool expected) =>
        Assert.Equal(expected, LiveEventLogic.IsSuicide(hasAttacker, attackerIsVictim, bomb));

    [Fact]
    public void BombKill()
    {
        Assert.True(LiveEventLogic.IsBombKill("planted_c4"));
        Assert.False(LiveEventLogic.IsBombKill("c4"));
        Assert.False(LiveEventLogic.IsBombKill(null));
    }

    [Theory]
    [InlineData(3, "ct")]
    [InlineData(2, "t")]
    [InlineData(1, "spec")]
    [InlineData(0, null)]
    public void Sides(int teamNum, string? expected) => Assert.Equal(expected, LiveEventLogic.SideName(teamNum));

    [Theory]
    [InlineData(0, "a")]
    [InlineData(1, "b")]
    [InlineData(null, null)]
    [InlineData(5, null)]
    public void BombSites(int? site, string? expected) => Assert.Equal(expected, LiveEventLogic.BombSiteName(site));

    [Theory]
    [InlineData(40, 27562, 12438)]
    [InlineData(40, 0, 40000)]
    [InlineData(40, 45000, 0)]  // never negative
    public void BombTimeRemaining(int timer, int sincePlant, int expected) =>
        Assert.Equal(expected, LiveEventLogic.BombTimeRemaining(timer, sincePlant));

    [Theory]
    [InlineData(0, 100, 0)]        // freeze time has not ended
    [InlineData(100, 151.434, 51434)]
    [InlineData(100, 99, 0)]
    public void RoundTime(double startedAt, double now, int expected) => Assert.Equal(expected, LiveEventLogic.RoundTime(startedAt, now));

    [Fact]
    public void SteamIds()
    {
        Assert.Equal("76561198279375306", LiveEventLogic.PlayerSteamId(76561198279375306, false, 4));
        Assert.Equal("BOT-7", LiveEventLogic.PlayerSteamId(0, true, 7));
    }
}

public class BombStatsTests
{
    [Fact]
    public void CountsPerPlayer()
    {
        var stats = new BombStats();
        stats.AddPlant(1);
        stats.AddPlant(1);
        stats.AddDefuse(2);
        Assert.Equal(2, stats.Plants(1));
        Assert.Equal(0, stats.Defuses(1));
        Assert.Equal(1, stats.Defuses(2));
        Assert.Equal(0, stats.Plants(3));
    }

    [Fact]
    public void RoundTripThroughBackup()
    {
        var stats = new BombStats();
        stats.AddPlant(76561198279375306);
        stats.AddDefuse(76561198279375306);
        stats.AddPlant(76561198000000001);

        var restored = new BombStats();
        restored.AddPlant(5); // counts from later rounds are replaced
        Assert.True(restored.LoadJson(stats.ToJson()));
        Assert.Equal(1, restored.Plants(76561198279375306));
        Assert.Equal(1, restored.Defuses(76561198279375306));
        Assert.Equal(1, restored.Plants(76561198000000001));
        Assert.Equal(0, restored.Plants(5));
    }

    [Fact]
    public void EmptyBackupClearsCounts()
    {
        var stats = new BombStats();
        stats.AddPlant(1);
        Assert.True(stats.LoadJson(new BombStats().ToJson()));  // restored to a round before any plant
        Assert.Equal(0, stats.Plants(1));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("{\"x\":[1,0]}")]
    [InlineData("{\"1\":[1]}")]
    [InlineData("{\"1\":[-1,0]}")]
    public void InvalidBackupKeepsCounts(string? json)
    {
        var stats = new BombStats();
        stats.AddPlant(1);
        Assert.False(stats.LoadJson(json));
        Assert.Equal(1, stats.Plants(1));
    }
}
