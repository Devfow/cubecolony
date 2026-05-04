using System;

namespace CheapNeuroSim
{
    public sealed class EpisodicMemoryState
    {
        private readonly MemoryTrace[] _traces;
        private int _cursor;

        public EpisodicMemoryState(int capacity = 24)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            _traces = new MemoryTrace[capacity];
        }

        public int Capacity => _traces.Length;

        public void Record(int groupId, float valence, float threat, float social, float novelty)
        {
            _traces[_cursor] = new MemoryTrace
            {
                GroupId = groupId,
                Valence = MathUtil.Clamp(valence, -1f, 1f),
                Threat = MathUtil.Clamp01(threat),
                Social = MathUtil.Clamp01(social),
                Novelty = MathUtil.Clamp01(novelty),
                Strength = MathUtil.Clamp01(0.18f + Math.Abs(valence) * 0.35f + threat * 0.35f + novelty * 0.18f),
                Age = 0
            };
            _cursor = (_cursor + 1) % _traces.Length;
        }

        public void Tick(float dt)
        {
            for (var i = 0; i < _traces.Length; i++)
            {
                if (_traces[i].Strength <= 0f)
                {
                    continue;
                }

                _traces[i].Age++;
                _traces[i].Strength = MathUtil.Clamp01(_traces[i].Strength - 0.0025f * dt);
            }
        }

        public float RecallValence(int groupId)
        {
            var weighted = 0f;
            var total = 0f;
            for (var i = 0; i < _traces.Length; i++)
            {
                var trace = _traces[i];
                if (trace.Strength <= 0f || trace.GroupId != groupId)
                {
                    continue;
                }

                weighted += trace.Valence * trace.Strength;
                total += trace.Strength;
            }

            return total <= 0f ? 0f : weighted / total;
        }

        public MemoryDebugSnapshot GetDebugSnapshot()
        {
            var traces = new MemoryTrace[_traces.Length];
            Array.Copy(_traces, traces, _traces.Length);

            var valence = 0f;
            var threat = 0f;
            var total = 0f;
            for (var i = 0; i < traces.Length; i++)
            {
                valence += traces[i].Valence * traces[i].Strength;
                threat += traces[i].Threat * traces[i].Strength;
                total += traces[i].Strength;
            }

            return new MemoryDebugSnapshot(traces, total <= 0f ? 0f : valence / total, total <= 0f ? 0f : threat / total);
        }

        public void ConsolidateInto(CultureState culture, float restQuality)
        {
            restQuality = MathUtil.Clamp01(restQuality);
            for (var i = 0; i < _traces.Length; i++)
            {
                var trace = _traces[i];
                if (trace.Strength < 0.35f || trace.GroupId < 0)
                {
                    continue;
                }

                var topicId = 10000 + trace.GroupId;
                var confidence = trace.Strength * restQuality * (0.4f + trace.Novelty * 0.3f + trace.Threat * 0.3f);
                culture.Teach(topicId, trace.Valence - trace.Threat * 0.35f, confidence, 0.5f);
                _traces[i].Strength = MathUtil.Clamp01(_traces[i].Strength - restQuality * 0.08f);
            }
        }

        internal MemoryTrace[] CopyTraces()
        {
            var traces = new MemoryTrace[_traces.Length];
            Array.Copy(_traces, traces, _traces.Length);
            return traces;
        }

        internal void Restore(MemoryTrace[] traces)
        {
            Array.Clear(_traces, 0, _traces.Length);
            if (traces == null)
            {
                return;
            }

            var count = Math.Min(traces.Length, _traces.Length);
            Array.Copy(traces, _traces, count);
            _cursor = count % _traces.Length;
        }
    }
}
