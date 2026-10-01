namespace MatchZy
{
    // Which new bot a practice .bot request keeps. This file must not depend on CounterStrikeSharp so that it can be unit tested (see tests/).
    public static class BotClaimLogic
    {
        public const int NoTeam = 0;

        public readonly record struct NewBot(int UserId, int Team, bool HasPawn);

        // Returns the user id of the bot to keep, or null to wait for another attempt (or, on the last attempt, to give up).
        // A bot on the requested team is kept first. A bot without a team yet is the one bot_add is still placing, but it is
        // only taken when it is the only one (or on the last attempt): with two, the other may be an extra joining the wrong team.
        public static int? SelectBotToClaim(IReadOnlyList<NewBot> newBots, int requestedTeam, bool lastAttempt)
        {
            var claimable = newBots.Where(bot => bot.HasPawn).ToList();
            foreach (var bot in claimable)
            {
                if (bot.Team == requestedTeam) return bot.UserId;
            }
            var teamless = claimable.Where(bot => bot.Team == NoTeam).ToList();
            if (teamless.Count == 1 || (lastAttempt && teamless.Count > 0)) return teamless[0].UserId;
            return null;
        }
    }
}
