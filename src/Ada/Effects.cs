using System;

namespace Ada
{
    public class Effects : ICloneable
    {
        private const double ALPHA = 0.69314718056d; // magic number used to convert % pitch change into semitones and vice-versa.

        public int Cents { get; set; }
        public float TempoMultiplier { get; set; }
        public bool ScalingPitch { get; set; }
        public bool ScalingTempo { get; set; }

        public Effects()
        {
            Cents = 0;
            TempoMultiplier = 1.0f;
            ScalingPitch = false;
            ScalingTempo = false;
        }

        public Effects(int cents, float tempo, bool scalingPitch, bool scalingTempo)
        {
            this.Cents = cents;
            this.TempoMultiplier = tempo;
            this.ScalingPitch = scalingPitch;
            this.ScalingTempo = scalingTempo;
        }

        public float Pitch
        {
            get {
                float pitch = (float)Math.Exp(ALPHA * ((double)(Cents / 100.0d) / 12.0d));
                return pitch;
            }
        }

        public float EffectiveTempoMultiplier {

            get
            {
                if (ScalingTempo)
                    return TempoMultiplier;
                return ScalingPitch ? Pitch : 1.0f;
            }
        }

        public int EffectiveCents
        {
            get
            {
                if (ScalingPitch)
                    return Cents;
                return ScalingTempo ? PitchFloatToCents(TempoMultiplier) : 0;
            }
        }

        public static float CentsToPitchFloat(int cents)
        {
            return (float)Math.Exp(ALPHA * ((double)cents / 100.0d / 12.0d));
        }

        public static int PitchFloatToCents(float pitch)
        {
            return (int)((Math.Log((double)pitch, Math.E) / ALPHA) * 12.0d * 100.0d);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Effects))
                return false;

            Effects fxparas = obj as Effects;
            return (fxparas.Cents == this.Cents) &&
                (fxparas.ScalingPitch == this.ScalingPitch) &&
                (fxparas.ScalingTempo == this.ScalingTempo) &&
                (fxparas.TempoMultiplier == this.TempoMultiplier);
        }

        public override int GetHashCode()
        {
            return Cents.GetHashCode() + TempoMultiplier.GetHashCode() +
                ScalingTempo.GetHashCode() + ScalingPitch.GetHashCode();
        }

        #region ICloneable Members

        public object Clone()
        {
            return new Effects(this.Cents, this.TempoMultiplier, this.ScalingPitch, this.ScalingTempo);
        }

        #endregion
    }
}
