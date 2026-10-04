using System.Collections.Generic;

namespace Sovereign.Core
{
    /// <summary>
    /// GDD 4 and 11-13. Everything the player controls. Changes are QUEUED and land
    /// at the next quarter boundary - you cannot tweak your way out of a bad quarter
    /// while it is happening, which is the point.
    /// </summary>
    public partial class PolicyState
    {
        // --- Fiscal -----------------------------------------------------------
        public readonly Dictionary<string, float> taxRates = new Dictionary<string, float>();
        public readonly Dictionary<string, float> spendingBillions = new Dictionary<string, float>();

        // --- Monetary ---------------------------------------------------------
        public float centralBankRate = 4f;
        public float qeAmountPerQuarter;        // $B
        public float qtAmountPerQuarter;        // $B
        public float reserveRequirement = 8f;   // percent
        public ForwardGuidance guidance = ForwardGuidance.Neutral;

        // --- Bonds ------------------------------------------------------------
        /// <summary>Cash committed to buying your own debt back at the next quarter.</summary>
        public float buybackBillions;

        public float foreignIssuanceShare = 0.28f;                                  // 0 - 0.8
        public BondMaturityPreference maturityPreference = BondMaturityPreference.Balanced;
        public bool issueInForeignCurrency;

        // --- Trade and currency -----------------------------------------------
        public float currencyIntervention;      // $B per quarter, + defends, - weakens
        public float exportSubsidyBillions;

        // --- Regulation -------------------------------------------------------
        public float bankingCapitalRequirement = 10f;   // percent
        public float environmentalRegulation = 40f;     // 0-100
        public float labourRegulation = 40f;            // 0-100

        // --- Population (GDD 7.6, used from Phase 2) --------------------------
        public float immigrationInflowMillions = 1.0f;
        public float skilledImmigrationShare = 0.2f;

        // --- The queue --------------------------------------------------------
        readonly Dictionary<string, float> _pendingTaxes = new Dictionary<string, float>();
        readonly Dictionary<string, float> _pendingSpending = new Dictionary<string, float>();
        readonly Dictionary<string, float> _pendingScalars = new Dictionary<string, float>();

        public int PendingChangeCount
        {
            get { return _pendingTaxes.Count + _pendingSpending.Count + _pendingScalars.Count + PendingNationCount; }
        }

        public float Tax(string key)
        {
            float value;
            return taxRates.TryGetValue(key, out value) ? value : 0f;
        }

        public float Spending(string key)
        {
            float value;
            return spendingBillions.TryGetValue(key, out value) ? value : 0f;
        }

        public float TotalSpendingBillions
        {
            get
            {
                float total = 0f;
                foreach (KeyValuePair<string, float> line in spendingBillions) total += line.Value;
                return total;
            }
        }

        public float InfrastructureSpendingBillions
        {
            get
            {
                float total = 0f;
                for (int i = 0; i < SpendKeys.Infrastructure.Length; i++)
                    total += Spending(SpendKeys.Infrastructure[i]);
                return total;
            }
        }

        /// <summary>What a control will read after the next quarter boundary - what the
        /// UI shows as the queued value.</summary>
        public float PendingTax(string key)
        {
            float value;
            return _pendingTaxes.TryGetValue(key, out value) ? value : Tax(key);
        }

        public float PendingSpending(string key)
        {
            float value;
            return _pendingSpending.TryGetValue(key, out value) ? value : Spending(key);
        }

        public float PendingScalar(string key, float current)
        {
            float value;
            return _pendingScalars.TryGetValue(key, out value) ? value : current;
        }

        public bool HasPendingTax(string key) { return _pendingTaxes.ContainsKey(key); }
        public bool HasPendingSpending(string key) { return _pendingSpending.ContainsKey(key); }
        public bool HasPendingScalar(string key) { return _pendingScalars.ContainsKey(key); }

        /// <summary>Queuing a value equal to the live one withdraws the change rather than
        /// leaving a no-op sitting in the queue looking like a decision.</summary>
        public void QueueTax(string key, float value)
        {
            if (value == Tax(key)) _pendingTaxes.Remove(key); else _pendingTaxes[key] = value;
        }
        public void QueueSpending(string key, float value)
        {
            if (value == Spending(key)) _pendingSpending.Remove(key); else _pendingSpending[key] = value;
        }

        public void QueueScalar(string key, float value, float current)
        {
            if (value == current) _pendingScalars.Remove(key); else _pendingScalars[key] = value;
        }

        public void ClearQueue()
        {
            _pendingTaxes.Clear();
            _pendingSpending.Clear();
            _pendingScalars.Clear();
            _pendingNations.Clear();
        }

        /// <summary>Applied by the simulator at the quarter boundary, never mid-quarter.</summary>
        public void CommitQueued()
        {
            foreach (KeyValuePair<string, float> entry in _pendingTaxes) taxRates[entry.Key] = entry.Value;
            foreach (KeyValuePair<string, float> entry in _pendingSpending) spendingBillions[entry.Key] = entry.Value;

            foreach (KeyValuePair<string, float> entry in _pendingScalars)
            {
                switch (entry.Key)
                {
                    case "centralBankRate": centralBankRate = entry.Value; break;
                    case "qeAmount": qeAmountPerQuarter = entry.Value; break;
                    case "qtAmount": qtAmountPerQuarter = entry.Value; break;
                    case "reserveRequirement": reserveRequirement = entry.Value; break;
                    case "foreignIssuanceShare": foreignIssuanceShare = entry.Value; break;
                    case "currencyIntervention": currencyIntervention = entry.Value; break;
                    case "bankingCapitalRequirement": bankingCapitalRequirement = entry.Value; break;
                    case "environmentalRegulation": environmentalRegulation = entry.Value; break;
                    case "labourRegulation": labourRegulation = entry.Value; break;
                    case "immigrationInflow": immigrationInflowMillions = entry.Value; break;
                    case "skilledImmigrationShare": skilledImmigrationShare = entry.Value; break;
                    case "exportSubsidy": exportSubsidyBillions = entry.Value; break;
                }
            }

            CommitNations();
            ClearQueue();
        }
    }
}
