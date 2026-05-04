using System;

namespace CheapNeuroSim
{
    public sealed class SocialBiasState
    {
        private readonly float[] _affiliation;
        private readonly float[] _threat;
        private readonly SocialBiasSettings _settings;

        public SocialBiasState(int groupCount, int selfGroupId, SocialBiasSettings settings)
        {
            if (groupCount < 0) throw new ArgumentOutOfRangeException(nameof(groupCount));
            _affiliation = new float[groupCount];
            _threat = new float[groupCount];
            SelfGroupId = groupCount == 0 ? 0 : ClampGroupId(selfGroupId, groupCount);
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));

            for (var i = 0; i < groupCount; i++)
            {
                _affiliation[i] = i == SelfGroupId ? _settings.InGroupAffiliation : 0f;
                _threat[i] = i == SelfGroupId ? 0f : _settings.OutGroupSuspicion;
            }
        }

        public int SelfGroupId { get; }
        public int GroupCount => _affiliation.Length;

        public float GetAffiliation(int groupId)
        {
            return IsValidGroup(groupId) ? _affiliation[groupId] : 0f;
        }

        public float GetThreat(int groupId)
        {
            return IsValidGroup(groupId) ? _threat[groupId] : 0f;
        }

        public GroupAttitude Evaluate(SocialIdentity identity)
        {
            if (!IsValidGroup(identity.GroupId))
            {
                return new GroupAttitude(0f, 0f);
            }

            return new GroupAttitude(
                _affiliation[identity.GroupId] * identity.VisibleMarkerStrength,
                _threat[identity.GroupId] * identity.VisibleMarkerStrength);
        }

        public void LearnFromInteraction(SocialIdentity identity, float reward, float threat, float dt)
        {
            if (!IsValidGroup(identity.GroupId))
            {
                return;
            }

            reward = MathUtil.Clamp(reward, -1f, 1f);
            threat = MathUtil.Clamp01(threat);
            var groupId = identity.GroupId;
            var marker = identity.VisibleMarkerStrength;
            var rate = _settings.LearningRate * dt * marker;

            _affiliation[groupId] = MathUtil.Clamp(_affiliation[groupId] + reward * rate - threat * rate * 0.35f, -1f, 1f);
            _threat[groupId] = MathUtil.Clamp01(_threat[groupId] + threat * rate - MathUtil.Clamp01(reward) * rate * 0.3f);

            var generalization = _settings.Generalization * rate;
            for (var i = 0; i < _threat.Length; i++)
            {
                if (i == groupId || i == SelfGroupId)
                {
                    continue;
                }

                _threat[i] = MathUtil.Clamp01(_threat[i] + threat * generalization);
            }
        }

        public void Tick(float dt)
        {
            for (var i = 0; i < _affiliation.Length; i++)
            {
                var targetAffiliation = i == SelfGroupId ? _settings.InGroupAffiliation : 0f;
                var targetThreat = i == SelfGroupId ? 0f : _settings.OutGroupSuspicion;
                _affiliation[i] = MathUtil.Approach(_affiliation[i], targetAffiliation, _settings.ExtinctionRate * dt);
                _threat[i] = MathUtil.Approach(_threat[i], targetThreat, _settings.ExtinctionRate * dt);
            }
        }

        public GroupBiasDebugSnapshot GetDebugSnapshot()
        {
            var affiliation = new float[_affiliation.Length];
            var threat = new float[_threat.Length];
            Array.Copy(_affiliation, affiliation, _affiliation.Length);
            Array.Copy(_threat, threat, _threat.Length);
            return new GroupBiasDebugSnapshot(SelfGroupId, affiliation, threat);
        }

        private bool IsValidGroup(int groupId)
        {
            return groupId >= 0 && groupId < _affiliation.Length;
        }

        private static int ClampGroupId(int groupId, int groupCount)
        {
            if (groupId < 0) return 0;
            if (groupId >= groupCount) return groupCount - 1;
            return groupId;
        }
    }
}
