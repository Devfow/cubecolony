using System;

namespace CheapNeuroSim
{
    public sealed class CultureState
    {
        private readonly CultureBelief[] _beliefs;

        public CultureState(int capacity = 16)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            _beliefs = new CultureBelief[capacity];
            for (var i = 0; i < _beliefs.Length; i++)
            {
                _beliefs[i].TopicId = -1;
            }
        }

        public void Teach(int topicId, float valence, float confidence, float conformity = 0.5f)
        {
            valence = MathUtil.Clamp(valence, -1f, 1f);
            confidence = MathUtil.Clamp01(confidence);
            conformity = MathUtil.Clamp01(conformity);

            var index = FindOrAllocate(topicId);
            var old = _beliefs[index];
            var weight = confidence * (0.25f + conformity * 0.55f);
            _beliefs[index] = new CultureBelief
            {
                TopicId = topicId,
                Valence = MathUtil.Clamp(old.Valence * (1f - weight) + valence * weight, -1f, 1f),
                Confidence = MathUtil.Clamp01(old.Confidence + confidence * 0.18f)
            };
        }

        public void Teach(CulturalMeme meme, float influence, float conformity = 0.5f)
        {
            Teach(meme.TopicId, meme.Valence, meme.Confidence * MathUtil.Clamp01(influence) * (0.4f + meme.Virulence * 0.6f), conformity);
        }

        public float GetValence(int topicId)
        {
            var index = Find(topicId);
            return index < 0 ? 0f : _beliefs[index].Valence * _beliefs[index].Confidence;
        }

        public CultureDebugSnapshot GetDebugSnapshot()
        {
            return new CultureDebugSnapshot(CopyBeliefs());
        }

        internal CultureBelief[] CopyBeliefs()
        {
            var beliefs = new CultureBelief[_beliefs.Length];
            Array.Copy(_beliefs, beliefs, _beliefs.Length);
            return beliefs;
        }

        internal void Restore(CultureBelief[] beliefs)
        {
            for (var i = 0; i < _beliefs.Length; i++)
            {
                _beliefs[i] = new CultureBelief { TopicId = -1 };
            }

            if (beliefs == null)
            {
                return;
            }

            var count = Math.Min(beliefs.Length, _beliefs.Length);
            Array.Copy(beliefs, _beliefs, count);
        }

        private int Find(int topicId)
        {
            for (var i = 0; i < _beliefs.Length; i++)
            {
                if (_beliefs[i].TopicId == topicId)
                {
                    return i;
                }
            }

            return -1;
        }

        private int FindOrAllocate(int topicId)
        {
            var index = Find(topicId);
            if (index >= 0) return index;

            var weakest = 0;
            for (var i = 0; i < _beliefs.Length; i++)
            {
                if (_beliefs[i].TopicId < 0)
                {
                    return i;
                }

                if (_beliefs[i].Confidence < _beliefs[weakest].Confidence)
                {
                    weakest = i;
                }
            }

            return weakest;
        }
    }
}
