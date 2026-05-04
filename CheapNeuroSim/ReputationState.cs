using System;

namespace CheapNeuroSim
{
    public sealed class ReputationState
    {
        private readonly ReputationEntry[] _entries;

        public ReputationState(int capacity = 32)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            _entries = new ReputationEntry[capacity];
            for (var i = 0; i < _entries.Length; i++)
            {
                _entries[i].IndividualId = -1;
            }
        }

        public ReputationEntry Learn(IndividualIdentity other, float helpfulness, float harm, float affection, float dt)
        {
            var index = FindOrAllocate(other.Id);
            var entry = _entries[index];
            entry.IndividualId = other.Id;
            entry.Trust = MathUtil.Clamp(entry.Trust + helpfulness * 0.12f * dt - harm * 0.18f * dt, -1f, 1f);
            entry.Fear = MathUtil.Clamp01(entry.Fear + harm * 0.16f * dt - helpfulness * 0.05f * dt);
            entry.Affection = MathUtil.Clamp(entry.Affection + affection * 0.10f * dt + helpfulness * 0.04f * dt - harm * 0.08f * dt, -1f, 1f);
            entry.Encounters++;
            _entries[index] = entry;
            return entry;
        }

        public ReputationEntry Get(int individualId)
        {
            var index = Find(individualId);
            return index < 0 ? new ReputationEntry { IndividualId = individualId } : _entries[index];
        }

        public ReputationDebugSnapshot GetDebugSnapshot()
        {
            var entries = new ReputationEntry[_entries.Length];
            Array.Copy(_entries, entries, _entries.Length);
            return new ReputationDebugSnapshot(entries);
        }

        internal ReputationEntry[] CopyEntries()
        {
            var entries = new ReputationEntry[_entries.Length];
            Array.Copy(_entries, entries, _entries.Length);
            return entries;
        }

        internal void Restore(ReputationEntry[] entries)
        {
            for (var i = 0; i < _entries.Length; i++)
            {
                _entries[i] = new ReputationEntry { IndividualId = -1 };
            }

            if (entries == null)
            {
                return;
            }

            var count = Math.Min(entries.Length, _entries.Length);
            Array.Copy(entries, _entries, count);
        }

        private int Find(int individualId)
        {
            for (var i = 0; i < _entries.Length; i++)
            {
                if (_entries[i].IndividualId == individualId)
                {
                    return i;
                }
            }

            return -1;
        }

        private int FindOrAllocate(int individualId)
        {
            var existing = Find(individualId);
            if (existing >= 0) return existing;

            var weakest = 0;
            for (var i = 0; i < _entries.Length; i++)
            {
                if (_entries[i].IndividualId < 0)
                {
                    return i;
                }

                if (_entries[i].Encounters < _entries[weakest].Encounters)
                {
                    weakest = i;
                }
            }

            return weakest;
        }
    }
}
