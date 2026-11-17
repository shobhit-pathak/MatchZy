using System.Numerics;
using MatchZy;

namespace MatchZy.Tests;

public class SpawnPickLogicTests
{
    // Standing eye height above the feet.
    private static readonly Vector3 Eye = new(0, 0, 64);

    [Fact]
    public void ForwardMatchesSourceAngles()
    {
        AssertClose(new Vector3(1, 0, 0), SpawnPickLogic.Forward(0, 0));
        AssertClose(new Vector3(0, 1, 0), SpawnPickLogic.Forward(0, 90));
        AssertClose(new Vector3(0, 0, -1), SpawnPickLogic.Forward(90, 0)); // positive pitch looks down
    }

    [Fact]
    public void PicksSpawnInFront()
    {
        var spawns = new List<Vector3> { new(0, 250, 0), new(200, 0, 0) };
        Assert.Equal(1, SpawnPickLogic.FindAimedSpawn(Eye, SpawnPickLogic.Forward(0, 0), spawns));
        Assert.Equal(0, SpawnPickLogic.FindAimedSpawn(Eye, SpawnPickLogic.Forward(0, 90), spawns));
    }

    [Fact]
    public void PicksSpawnWhenLookingDownAtItsFeet()
    {
        var spawns = new List<Vector3> { new(200, 0, 0) };
        // Looking at the floor where the spawn stands, below the beam's top.
        float pitch = MathF.Atan2(64, 200) * 180f / MathF.PI;
        Assert.Equal(0, SpawnPickLogic.FindAimedSpawn(Eye, SpawnPickLogic.Forward(pitch, 0), spawns));
    }

    [Fact]
    public void IgnoresSpawnBesideTheView()
    {
        var spawns = new List<Vector3> { new(250, 60, 0) };
        Assert.Equal(-1, SpawnPickLogic.FindAimedSpawn(Eye, SpawnPickLogic.Forward(0, 0), spawns));
    }

    [Fact]
    public void IgnoresSpawnBehind()
    {
        var spawns = new List<Vector3> { new(-200, 0, 0) };
        Assert.Equal(-1, SpawnPickLogic.FindAimedSpawn(Eye, SpawnPickLogic.Forward(0, 0), spawns));
    }

    [Fact]
    public void IgnoresSpawnWhenLookingOverTheBeam()
    {
        var spawns = new List<Vector3> { new(300, 0, 0) };
        // Beam ends at z = 100; looking up passes well above it.
        Assert.Equal(-1, SpawnPickLogic.FindAimedSpawn(Eye, SpawnPickLogic.Forward(-30, 0), spawns));
    }

    [Fact]
    public void PicksNearestOfSpawnsInLine()
    {
        var spawns = new List<Vector3> { new(250, 0, 0), new(120, 0, 0) };
        Assert.Equal(1, SpawnPickLogic.FindAimedSpawn(Eye, SpawnPickLogic.Forward(0, 0), spawns));
    }

    [Fact]
    public void IgnoresSpawnBeyondMaxDistance()
    {
        var spawns = new List<Vector3> { new(SpawnPickLogic.MaxDistance + 100, 0, 0) };
        Assert.Equal(-1, SpawnPickLogic.FindAimedSpawn(Eye, SpawnPickLogic.Forward(0, 0), spawns));
    }

    [Fact]
    public void IgnoresSpawnThePlayerStandsOn()
    {
        var spawns = new List<Vector3> { new(0, 0, 0), new(200, 0, 0) };
        Assert.Equal(1, SpawnPickLogic.FindAimedSpawn(Eye, SpawnPickLogic.Forward(0, 0), spawns));
        // Looking at the sky from the spawn you stand on picks nothing.
        Assert.Equal(-1, SpawnPickLogic.FindAimedSpawn(Eye, SpawnPickLogic.Forward(-80, 0), spawns));
    }

    [Fact]
    public void NoSpawns()
    {
        Assert.Equal(-1, SpawnPickLogic.FindAimedSpawn(Eye, SpawnPickLogic.Forward(0, 0), new List<Vector3>()));
    }

    private static void AssertClose(Vector3 expected, Vector3 actual)
    {
        Assert.True(Vector3.Distance(expected, actual) < 1e-4f, $"expected {expected}, got {actual}");
    }
}
