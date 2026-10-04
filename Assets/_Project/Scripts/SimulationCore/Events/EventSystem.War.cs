namespace Sovereign.Core
{
    /// <summary>
    /// GDD 17.3. A war against you is decided by the spending gap, not by a dice
    /// roll: outspend the enemy and it is short and cheap; underspend and the bill
    /// arrives as casualties, wrecked equipment, damaged infrastructure and, years
    /// later, veterans. Plain numbers, no gore.
    /// </summary>
    public partial class EventSystem
    {
        void StartWar(EconomyState state, EventInstance e)
        {
            WarState war = state.events.war;
            war.active = true;
            war.eventId = e.id;
            war.weeks = 0;
            war.kia = 0f;
            war.equipmentStock = 100f;
            war.cumulativeCostBillions = 0f;
            war.gapSum = 0f;
            war.enemyStrength = state.events.baselineDefence * _c.war.enemyStrengthMultiple * e.severity;

            // GDD 14: the rally round the flag.
            PushApproval(state, _c.war.rallyApprovalBonus);
            state.events.Post(state.week, AlertLevel.Danger, AlertChannel.Advisor,
                "We are at war. Defence spending of at least " + _c.war.wartimeMinimumDefenceBillions.ToString("0")
                + "B is now mandatory; every dollar above what they spend saves lives.");
        }

        void TickWar(EconomyState state, PolicyState policy, EventInstance e)
        {
            WarState war = state.events.war;
            if (!war.active) return;
            war.weeks++;

            // GDD 17.3: a war forces a minimum defence spend.
            float defence = Defence(policy);
            if (defence < _c.war.wartimeMinimumDefenceBillions)
            {
                float shortfall = _c.war.wartimeMinimumDefenceBillions - defence;
                policy.spendingBillions[SpendKeys.MilitaryOperations] = policy.Spending(SpendKeys.MilitaryOperations) + shortfall;
                defence += shortfall;
            }

            // Worn-out equipment fights worse, so under-spending compounds.
            float effective = defence * war.equipmentStock * 0.01f;
            float gap = (war.enemyStrength - effective) / MathUtil.Max(1f, MathUtil.Max(war.enemyStrength, effective));
            war.gapSum += gap;

            float kia = _c.war.casualtyCurve.Evaluate(gap) * e.severity;
            war.kia += kia;
            state.population.workingAge = MathUtil.Max(0f, state.population.workingAge - kia * 1e-6f);

            float pressure = 1f + MathUtil.Max(0f, gap);
            war.equipmentStock = MathUtil.Clamp(war.equipmentStock - _c.war.equipmentDegradationPercentPerWeek * pressure, 5f, 100f);
            state.infrastructureHealth = MathUtil.Clamp(
                state.infrastructureHealth - _c.war.infrastructureDamagePerWeek * pressure * (1f - 0.5f * e.mitigation), 0f, 100f);

            // Wartime rotation turns serving personnel into veterans twice as fast.
            state.population.veterans += state.population.militaryHeadcount * _c.war.veteranConversionRate * 2f * Weekly;
            war.cumulativeCostBillions += defence * Weekly;

            // GDD 14: the drain, accelerating after eighteen months.
            PushApproval(state, war.weeks > _c.war.lateWarThresholdWeeks ? -_c.war.lateWarApprovalDrain : -_c.war.weeklyApprovalDrain);

            war.lastReport = "Week " + war.weeks + ": " + war.kia.ToString("#,0") + " KIA - equipment "
                             + war.equipmentStock.ToString("0") + "% - infrastructure " + state.infrastructureHealth.ToString("0") + "%";
            if (war.weeks % MathUtil.Max(1, _t.warReportEveryWeeks) == 0)
                state.events.Post(state.week, gap > 0f ? AlertLevel.Danger : AlertLevel.Warning, AlertChannel.Ticker, war.lastReport);
        }

        /// <summary>Victory or defeat is the average spending gap across the war.
        /// Lose badly enough and it is occupation - the end of the run.</summary>
        string EndWar(EconomyState state)
        {
            WarState war = state.events.war;
            war.active = false;
            float averageGap = war.weeks <= 0 ? 0f : war.gapSum / war.weeks;

            if (averageGap < 0f)
            {
                PushApproval(state, _c.war.victoryApprovalBonus);
                return "victory after " + war.weeks + " weeks, " + war.kia.ToString("#,0") + " killed";
            }

            PushApproval(state, _c.war.defeatApprovalPenalty);
            if (averageGap > _t.occupationGapThreshold && !state.IsGameOver)
            {
                state.approval.revolt = true;
                state.approval.revoltReason = "Economic occupation. Outspent for " + war.weeks + " weeks, with "
                    + war.kia.ToString("#,0") + " killed, the country could no longer defend itself.";
                return "defeat and occupation";
            }
            return "stalemate after " + war.weeks + " weeks, " + war.kia.ToString("#,0") + " killed";
        }
    }
}
