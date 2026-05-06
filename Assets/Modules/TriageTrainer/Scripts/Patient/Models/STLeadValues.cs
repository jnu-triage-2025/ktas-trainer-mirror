using System;

namespace TriageTrainer.Entity.Patient
{
  [Serializable]
  public struct STLeadValues
  {
    public float i;
    public float ii;
    public float iii;
    public float avr;
    public float avl;
    public float avf;
    public float v1;
    public float v2;
    public float v3;
    public float v4;
    public float v5;
    public float v6;

    public static STLeadValues Default => new STLeadValues
    {
      i = 0.0f,
      ii = 0.0f,
      iii = 0.0f,
      avr = 0.0f,
      avl = 0.0f,
      avf = 0.0f,
      v1 = 0.0f,
      v2 = 0.0f,
      v3 = 0.0f,
      v4 = 0.0f,
      v5 = 0.0f,
      v6 = 0.0f
    };
  }
}
