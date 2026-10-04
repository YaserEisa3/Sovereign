namespace Sovereign.Core
{
    /// <summary>
    /// GDD 14. Each class and faction scores the conditions it cares about on 0-100,
    /// weights them with the ApprovalWeights asset, and drifts toward the result.
    /// The scores are calibrated at boot so the opening approvals are exactly the
    /// designer's - everything after that is the player's doing.
    /// </summary>
    public partial class ApprovalModel
    {
        readonly ApprovalConfig _c;

        public ApprovalModel(ApprovalConfig config) { _c = config; }

        public ApprovalConfig Config { get { return _c; } }

        public void Initialise(EconomyState state, PolicyState policy)
        {
            ApprovalState a = state.approval;
            a.baselineWelfare = Welfare(policy);
            a.baselineHealth = Health(policy);
            a.baselineEducation = Education(policy);
            a.baselineSocial = a.baselineWelfare + a.baselineHealth;
            a.baselineDefence = Defence(policy);
            a.baselineImmigration = policy.immigrationInflowMillions;

            float[] raw = RawTargets(state, policy);
            float[] start = { _c.startPoor, _c.startMiddle, _c.startWealthy, _c.startLeft, _c.startCentre, _c.startRight };
            for (int i = 0; i < 6; i++) a.offsets[i] = start[i] - raw[i];

            a.poor = _c.startPoor; a.middle = _c.startMiddle; a.wealthy = _c.startWealthy;
            a.left = _c.startLeft; a.centre = _c.startCentre; a.right = _c.startRight;
            a.overall = Overall(a);
        }

        public void Tick(EconomyState state, PolicyState policy)
        {
            ApprovalState a = state.approval;
            float[] raw = RawTargets(state, policy);
            float speed = _c.adjustmentSpeed;

            a.poor = Move(a.poor, raw[0] + a.offsets[0], speed);
            a.middle = Move(a.middle, raw[1] + a.offsets[1], speed);
            a.wealthy = Move(a.wealthy, raw[2] + a.offsets[2], speed);
            a.left = Move(a.left, raw[3] + a.offsets[3], speed);
            a.centre = Move(a.centre, raw[4] + a.offsets[4], speed);
            a.right = Move(a.right, raw[5] + a.offsets[5], speed);
            a.overall = Overall(a);

            state.Series("approvalOverall").Record(a.overall);
            state.Series("approvalPoor").Record(a.poor);
            state.Series("approvalMiddle").Record(a.middle);
            state.Series("approvalWealthy").Record(a.wealthy);

            UpdateUnrest(state);
            CheckRevolt(state);
        }

        float Overall(ApprovalState a)
        {
            float shares = _c.poorShare + _c.middleShare + _c.wealthyShare;
            if (shares <= 0f) return 0f;
            return (a.poor * _c.poorShare + a.middle * _c.middleShare + a.wealthy * _c.wealthyShare) / shares;
        }

        static float Move(float current, float target, float speed)
        {
            return MathUtil.Clamp(MathUtil.Approach(current, MathUtil.Clamp(target, 0f, 100f), speed), 0f, 100f);
        }

        static float Welfare(PolicyState p)
        {
            return p.Spending(SpendKeys.UnemploymentInsurance) + p.Spending(SpendKeys.FoodAssistance)
                   + p.Spending("HousingAssistance") + p.Spending("SocialSecurityDisability");
        }

        static float Health(PolicyState p)
        {
            return p.Spending(SpendKeys.Medicaid) + p.Spending(SpendKeys.Medicare) + p.Spending(SpendKeys.PublicHealth);
        }

        static float Education(PolicyState p)
        {
            return p.Spending(SpendKeys.EducationK12) + p.Spending("EducationHigher");
        }

        static float Defence(PolicyState p)
        {
            return p.Spending(SpendKeys.MilitaryPersonnel) + p.Spending("MilitaryEquipment")
                   + p.Spending(SpendKeys.MilitaryOperations) + p.Spending("HomelandSecurity");
        }
    }
}
